using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CADAddin.Common;                    // ← THÊM để gọi Utils

// Alias để tránh xung đột
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;



namespace CADAddin.Layer
{
    public class ChangeLayerCommands
    {
        [CommandMethod("ChangeLayer")]
        public void ChangeLayer()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<string> layerList = GetAllLayers(db);
            if (layerList.Count == 0)
            {
                ed.WriteMessage("\nKhông có layer nào trong bản vẽ.");
                return;
            }

            using (ChangeLayerForm form = new ChangeLayerForm(layerList))
            {
                if (AcadApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                string sourceLayer = form.SourceLayer;
                string targetLayer = form.TargetLayer;
                bool isAllMode = form.IsAllMode;

                if (sourceLayer == targetLayer)
                {
                    MessageBox.Show("Layer nguồn và đích giống nhau, không cần chuyển.");
                    return;
                }

                if (isAllMode)
                    ChangeAllEntities(db, ed, sourceLayer, targetLayer);
                else
                    ChangeSelectedEntities(db, ed, sourceLayer, targetLayer);
            }
        }

        private List<string> GetAllLayers(Database db)
        {
            List<string> list = new List<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId id in lt)
                {
                    LayerTableRecord ltr = tr.GetObject(id, OpenMode.ForRead) as LayerTableRecord;
                    list.Add(ltr.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x).ToList();
        }

        private void ChangeAllEntities(Database db, Editor ed, string source, string target)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TypedValue[] filter = { new TypedValue((int)DxfCode.LayerName, source) };
                SelectionFilter sf = new SelectionFilter(filter);
                PromptSelectionResult psr = ed.SelectAll(sf);

                if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
                {
                    MessageBox.Show($"Không có entity nào trên layer {source}.");
                    return;
                }

                int cnt = 0;
                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Layer = target;
                        cnt++;
                    }
                }
                tr.Commit();
                MessageBox.Show($"Đã chuyển {cnt} entity từ layer {source} sang {target}.");
            }
        }

        private void ChangeSelectedEntities(Database db, Editor ed, string source, string target)
        {
            ed.WriteMessage($"\nChọn vùng entity trên layer {source} (Enter để kết thúc chọn): ");
            TypedValue[] filter = { new TypedValue((int)DxfCode.LayerName, source) };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.GetSelection(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show("Không có entity nào được chọn trên layer nguồn.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                int cnt = 0;
                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Layer = target;
                        cnt++;
                    }
                }
                tr.Commit();
                MessageBox.Show($"Đã chuyển {cnt} entity từ layer {source} sang {target} trong vùng chọn.");
            }
        }
    }

    public class ChangeLayerForm : Form
    {
        private ComboBox cboSource;
        private ComboBox cboTarget;
        private RadioButton rbAll;
        private RadioButton rbSelect;
        private Button btnPick;
        private Button btnOK;
        private Button btnCancel;

        public string SourceLayer => cboSource.SelectedItem?.ToString();
        public string TargetLayer => cboTarget.SelectedItem?.ToString();
        public bool IsAllMode => rbAll.Checked;

        public ChangeLayerForm(List<string> layers)
        {
            this.Text = "Chuyển Layer";
            this.Width = 420;
            this.Height = 280;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label { Text = "Chọn layer nguồn và đích:", Left = 20, Top = 15, Width = 360 };

            rbAll = new RadioButton { Text = "Chuyển tất cả trên layer nguồn", Left = 20, Top = 45, Width = 360, Checked = true };
            rbSelect = new RadioButton { Text = "Chuyển entity được chọn trong vùng", Left = 20, Top = 70, Width = 360 };

            Label lblSource = new Label { Text = "Layer nguồn:", Left = 20, Top = 110, Width = 100 };
            cboSource = new ComboBox { Left = 120, Top = 107, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboSource.Items.AddRange(layers.ToArray());
            if (layers.Count > 0) cboSource.SelectedIndex = 0;

            btnPick = new Button { Text = "Chọn Entity", Left = 310, Top = 105, Width = 80 };
            btnPick.Click += BtnPick_Click;

            Label lblTarget = new Label { Text = "Layer đích:", Left = 20, Top = 150, Width = 100 };
            cboTarget = new ComboBox { Left = 120, Top = 147, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboTarget.Items.AddRange(layers.ToArray());
            if (layers.Count > 0) cboTarget.SelectedIndex = 0;

            btnOK = new Button { Text = "OK", Left = 200, Top = 200, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Left = 290, Top = 200, Width = 80, DialogResult = DialogResult.Cancel };

            this.Controls.AddRange(new Control[] { lblTitle, rbAll, rbSelect, lblSource, cboSource, btnPick, lblTarget, cboTarget, btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void BtnPick_Click(object sender, EventArgs e)
        {
            this.Hide();
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptEntityResult per = ed.GetEntity("\nChọn 1 đối tượng để lấy layer nguồn: ");
            if (per.Status == PromptStatus.OK)
            {
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent != null)
                    {
                        string lay = ent.Layer;
                        int idx = cboSource.Items.IndexOf(lay);
                        if (idx >= 0) cboSource.SelectedIndex = idx;
                        MessageBox.Show($"Đã chọn layer nguồn: {lay}");
                    }
                    tr.Commit();
                }
            }
            this.Show();
        }
    }
}