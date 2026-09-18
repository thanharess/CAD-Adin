using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;


namespace YourNamespace
{
    public class LayerDeleteCommands
    {
        // Biến nhớ layer đã pick (tương đương *picked-layer*)
        private static string _pickedLayer = null;

        [CommandMethod("LayerDelete")]
        public void LayerDelete()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<string> layerList = GetAllLayers(db);

            using (LayerDeleteForm form = new LayerDeleteForm(layerList, _pickedLayer))
            {
                DialogResult dr = AcadApp.ShowModalDialog(form);

                // Nút "Chọn Entity" → result đặc biệt
                if (form.PickRequested)
                {
                    ed.WriteMessage("\nChọn 1 đối tượng để lấy layer: ");
                    PromptEntityResult per = ed.GetEntity("");
                    if (per.Status == PromptStatus.OK)
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent != null)
                            {
                                _pickedLayer = ent.Layer;
                                MessageBox.Show($"Đã chọn layer: {_pickedLayer}");
                            }
                            tr.Commit();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Không chọn được đối tượng.");
                    }

                    // Gọi lại lệnh (mở lại form với layer đã chọn)
                    LayerDelete();
                    return;
                }

                if (dr != DialogResult.OK)
                {
                    ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                string choice = form.SelectedOption;
                string layname = form.SelectedLayer;

                if (string.IsNullOrEmpty(layname))
                {
                    MessageBox.Show("Không có layer nào được chọn.");
                    return;
                }

                switch (choice)
                {
                    case "dim_by_layer_select":
                        DeleteEntitiesByLayerSelect(ed, layname);
                        break;

                    case "dim_by_layer":
                        DeleteEntitiesByLayer(ed, layname);
                        break;

                    case "all_dim":
                        PurgeAllUnusedLayers(ed);
                        break;

                    default:
                        MessageBox.Show("Lựa chọn không hợp lệ.");
                        break;
                }
            }
        }

        // ========== Chức năng ==========

        private void DeleteEntitiesByLayerSelect(Editor ed, string layname)
        {
            ed.WriteMessage($"\nChọn vùng các entity trên layer {layname}: ");
            TypedValue[] filter = { new TypedValue((int)DxfCode.LayerName, layname) };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.GetSelection(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show("Không có entity nào được chọn.");
                return;
            }

            int cnt = EraseEntities(ed.Document.Database, psr.Value.GetObjectIds());
            MessageBox.Show($"Đã xóa {cnt} entity trên layer {layname} trong vùng chọn.");
        }

        private void DeleteEntitiesByLayer(Editor ed, string layname)
        {
            TypedValue[] filter = { new TypedValue((int)DxfCode.LayerName, layname) };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.SelectAll(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show($"Không có entity nào trên layer {layname}.");
                return;
            }

            int cnt = EraseEntities(ed.Document.Database, psr.Value.GetObjectIds());
            MessageBox.Show($"Đã xóa {cnt} entity trên layer {layname}.");
        }

        private void PurgeAllUnusedLayers(Editor ed)
        {
            // Tương đương (command "_.PURGE" "_LA" "*" "N")
            ed.Command("_.PURGE", "_LA", "*", "N");
            MessageBox.Show("Đã purge tất cả layer không dùng.");
        }

        private int EraseEntities(Database db, ObjectId[] ids)
        {
            int cnt = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Erase();
                        cnt++;
                    }
                }
                tr.Commit();
            }
            return cnt;
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
                    if (ltr.Name != "Defpoints")          // giống LISP
                        list.Add(ltr.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x).ToList();
        }
    }

    // ========== Windows Form ==========
    public class LayerDeleteForm : Form
    {
        private RadioButton rbSelect;
        private RadioButton rbAllOnLayer;
        private RadioButton rbPurge;
        private ComboBox cboLayer;
        private Button btnPick;
        private Button btnOK;
        private Button btnCancel;

        public bool PickRequested { get; private set; } = false;

        public string SelectedOption
        {
            get
            {
                if (rbSelect.Checked) return "dim_by_layer_select";
                if (rbAllOnLayer.Checked) return "dim_by_layer";
                if (rbPurge.Checked) return "all_dim";
                return "dim_by_layer_select";
            }
        }

        public string SelectedLayer => cboLayer.SelectedItem?.ToString() ?? "";

        public LayerDeleteForm(List<string> layers, string preSelectedLayer)
        {
            this.Text = "Xóa Layer";
            this.Width = 480;
            this.Height = 300;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Chọn chức năng:",
                Left = 20,
                Top = 15,
                Width = 420,
                Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
            };

            rbSelect = new RadioButton
            {
                Text = "1. Xóa Line theo layer trong vùng chọn",
                Left = 20,
                Top = 45,
                Width = 420,
                Checked = true
            };
            rbAllOnLayer = new RadioButton
            {
                Text = "2. Xóa tất cả Line trên layer",
                Left = 20,
                Top = 70,
                Width = 420
            };
            rbPurge = new RadioButton
            {
                Text = "3. Phá khối tất cả layer không dùng",
                Left = 20,
                Top = 95,
                Width = 420
            };

            Label lblLayer = new Label { Text = "Chọn Layer:", Left = 20, Top = 140, Width = 90 };

            cboLayer = new ComboBox
            {
                Left = 110,
                Top = 137,
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboLayer.Items.AddRange(layers.ToArray());

            // Set layer đã pick trước đó
            if (!string.IsNullOrEmpty(preSelectedLayer))
            {
                int idx = cboLayer.Items.IndexOf(preSelectedLayer);
                cboLayer.SelectedIndex = (idx >= 0) ? idx : 0;
            }
            else if (cboLayer.Items.Count > 0)
            {
                cboLayer.SelectedIndex = 0;
            }

            btnPick = new Button { Text = "Chọn Entity", Left = 340, Top = 135, Width = 100 };
            btnPick.Click += (s, e) =>
            {
                PickRequested = true;
                this.DialogResult = DialogResult.Cancel;   // đóng form để xử lý pick
                this.Close();
            };

            btnOK = new Button { Text = "OK", Left = 260, Top = 200, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Left = 350, Top = 200, Width = 80, DialogResult = DialogResult.Cancel };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, rbSelect, rbAllOnLayer, rbPurge,
                lblLayer, cboLayer, btnPick, btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}