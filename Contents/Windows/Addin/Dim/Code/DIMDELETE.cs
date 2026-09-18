using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

// Alias tránh xung đột Application
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;


namespace CADAddin.Dim
{
    public class DimDeleteCommands
    {
        [CommandMethod("DimDelete")]
        public void DimDelete()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<string> layerList = GetAllLayers(db);

            using (DimDeleteForm form = new DimDeleteForm(layerList))
            {
                if (AcadApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                string choice = form.SelectedOption;
                string layname = form.SelectedLayer;

                switch (choice)
                {
                    case "selected_dim":
                        DeleteSelectedDim(ed);
                        break;

                    case "dim_by_layer":
                        if (string.IsNullOrEmpty(layname) || layname == "Không có layer")
                            MessageBox.Show("Layer không hợp lệ.");
                        else
                            DeleteDimByLayer(ed, layname);
                        break;

                    case "dim_mleader":
                        DeleteMLeader(ed);
                        break;

                    case "dim_by_layer_select":
                        if (string.IsNullOrEmpty(layname) || layname == "Không có layer")
                            MessageBox.Show("Layer không hợp lệ.");
                        else
                            DeleteDimByLayerSelect(ed, layname);
                        break;

                    case "all_dim":
                        DeleteAllDim(ed);
                        break;

                    default:
                        MessageBox.Show("Lựa chọn không hợp lệ. Mặc định xóa DIMENSION được chọn.");
                        DeleteSelectedDim(ed);
                        break;
                }
            }
        }

        // ========== Các hàm chức năng ==========

        private void DeleteSelectedDim(Editor ed)
        {
            ed.WriteMessage("\nChọn các DIMENSION cần xóa (window/crossing hoặc pick, Enter để kết thúc): ");
            TypedValue[] filter = { new TypedValue((int)DxfCode.Start, "DIMENSION") };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.GetSelection(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show("Không có DIMENSION nào được chọn.");
                return;
            }

            using (Transaction tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                int cnt = 0;
                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Erase();
                        cnt++;
                    }
                }
                tr.Commit();
                MessageBox.Show($"Đã xóa {cnt} DIMENSION được chọn.");
            }
        }

        private void DeleteDimByLayer(Editor ed, string layname)
        {
            TypedValue[] filter = {
                new TypedValue((int)DxfCode.Start, "DIMENSION"),
                new TypedValue((int)DxfCode.LayerName, layname)
            };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.SelectAll(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show($"Không có DIMENSION nào trên layer {layname}.");
                return;
            }

            using (Transaction tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                int cnt = 0;
                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Erase();
                        cnt++;
                    }
                }
                tr.Commit();
                MessageBox.Show($"Đã xóa {cnt} DIMENSION trên layer {layname}.");
            }
        }

        private void DeleteMLeader(Editor ed)
        {
            TypedValue[] filter = { new TypedValue((int)DxfCode.Start, "MULTILEADER") };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.SelectAll(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show("Không có LEADER NOTE (MLEADER) nào trong bản vẽ.");
                return;
            }

            using (Transaction tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                int cnt = 0;
                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Erase();
                        cnt++;
                    }
                }
                tr.Commit();
                MessageBox.Show($"Đã xóa {cnt} LEADER NOTE (MLEADER).");
            }
        }

        private void DeleteDimByLayerSelect(Editor ed, string layname)
        {
            ed.WriteMessage($"\nChọn vùng DIMENSION trên layer {layname} (window/crossing hoặc pick, Enter để kết thúc): ");
            TypedValue[] filter = {
                new TypedValue((int)DxfCode.Start, "DIMENSION"),
                new TypedValue((int)DxfCode.LayerName, layname)
            };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.GetSelection(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show($"Không có DIMENSION nào được chọn trên layer {layname}.");
                return;
            }

            using (Transaction tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                int cnt = 0;
                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Erase();
                        cnt++;
                    }
                }
                tr.Commit();
                MessageBox.Show($"Đã xóa {cnt} DIMENSION trên layer {layname} trong vùng chọn.");
            }
        }

        private void DeleteAllDim(Editor ed)
        {
            TypedValue[] filter = { new TypedValue((int)DxfCode.Start, "DIMENSION") };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.SelectAll(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show("Không có DIMENSION nào trong bản vẽ.");
                return;
            }

            using (Transaction tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                int cnt = 0;
                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Erase();
                        cnt++;
                    }
                }
                tr.Commit();
                MessageBox.Show($"Đã xóa {cnt} DIMENSION.");
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
            return list.Count > 0 ? list.OrderBy(x => x).ToList() : new List<string> { "Không có layer" };
        }
    }

    // ========== Windows Form thay cho DCL ==========
    public class DimDeleteForm : Form
    {
        private RadioButton rbSelected;
        private RadioButton rbByLayer;
        private RadioButton rbMLeader;
        private RadioButton rbByLayerSelect;
        private RadioButton rbAll;
        private ComboBox cboLayer;
        private Button btnOK;
        private Button btnCancel;

        public string SelectedOption
        {
            get
            {
                if (rbSelected.Checked) return "selected_dim";
                if (rbByLayer.Checked) return "dim_by_layer";
                if (rbMLeader.Checked) return "dim_mleader";
                if (rbByLayerSelect.Checked) return "dim_by_layer_select";
                if (rbAll.Checked) return "all_dim";
                return "selected_dim";
            }
        }

        public string SelectedLayer => cboLayer.SelectedItem?.ToString() ?? "";

        public DimDeleteForm(List<string> layers)
        {
            this.Text = "Xóa Dimension";
            this.Width = 520;
            this.Height = 320;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Chọn chức năng:",
                Left = 20,
                Top = 15,
                Width = 460,
                Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
            };

            rbSelected = new RadioButton { Text = "1. Xóa DIMENSION được chọn", Left = 20, Top = 45, Width = 460, Checked = true };
            rbByLayer = new RadioButton { Text = "2. Xóa DIMENSION theo layer", Left = 20, Top = 70, Width = 460 };
            rbMLeader = new RadioButton { Text = "3. Xóa LEADER NOTE (MLEADER)", Left = 20, Top = 95, Width = 460 };
            rbByLayerSelect = new RadioButton { Text = "4. Xóa DIMENSION theo layer và vùng chọn", Left = 20, Top = 120, Width = 460 };
            rbAll = new RadioButton { Text = "5. Xóa tất cả DIMENSION", Left = 20, Top = 145, Width = 460 };

            Label lblLayer = new Label { Text = "Chọn Layer (cho chức năng 2 và 4):", Left = 20, Top = 185, Width = 250 };

            cboLayer = new ComboBox
            {
                Left = 270,
                Top = 182,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboLayer.Items.AddRange(layers.ToArray());
            if (layers.Count > 0) cboLayer.SelectedIndex = 0;

            btnOK = new Button { Text = "OK", Left = 300, Top = 230, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Cancel", Left = 390, Top = 230, Width = 80, DialogResult = DialogResult.Cancel };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, rbSelected, rbByLayer, rbMLeader, rbByLayerSelect, rbAll,
                lblLayer, cboLayer, btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}