using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// Alias tránh xung đột Application
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;


namespace CADAddin.Dim
{
    public class DimDeleteCommands
    {
        // ═══════════════════════════════════════════════════════════
        //  NHỚ CÀI ĐẶT giữa các lần chạy (static fields)
        // ═══════════════════════════════════════════════════════════
        private static string _lastPickedLayer = null;   // layer đã pick/gõ
        private static string _lastOption = "selected_dim";   // option radio cuối cùng

        [CommandMethod("DimDelete")]
        public void DimDelete()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            // ═══════════════════════════════════════════════════════
            //  Vòng lặp: nếu user bấm "Pick Layer" → đóng form, pick, mở lại
            // ═══════════════════════════════════════════════════════
            string choice;
            string layname;

            while (true)
            {
                List<string> layerList = GetAllLayers(db);

                using (DimDeleteForm form = new DimDeleteForm(
                    layerList, _lastPickedLayer, _lastOption))
                {
                    var dr = AcadApp.ShowModalDialog(form);

                    // ── Nếu user bấm "Pick Layer" → pick entity rồi mở lại form ──
                    if (form.PickLayerRequested)
                    {
                        var peo = new PromptEntityOptions(
                            "\nChọn 1 đối tượng bất kỳ để lấy Layer: ");
                        var per = ed.GetEntity(peo);

                        if (per.Status == PromptStatus.OK)
                        {
                            using (var tr = db.TransactionManager.StartTransaction())
                            {
                                var ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                                if (ent != null)
                                    _lastPickedLayer = ent.Layer;
                                tr.Commit();
                            }
                        }

                        // Nhớ option đã chọn để mở lại form đúng trạng thái
                        _lastOption = form.SelectedOption;
                        continue;
                    }

                    // ── Nếu user bấm Cancel → thoát ──
                    if (dr != DialogResult.OK)
                    {
                        ed.WriteMessage("\nĐã hủy lệnh.");
                        return;
                    }

                    choice = form.SelectedOption;
                    layname = form.SelectedLayer;
                }

                // ═══════════════════════════════════════════════════
                //  LƯU cài đặt để lần sau mở lại form giống vậy
                // ═══════════════════════════════════════════════════
                _lastOption = choice;
                if (!string.IsNullOrEmpty(layname) && layname != "Không có layer")
                    _lastPickedLayer = layname;

                break;   // OK → thoát vòng lặp
            }

            // ═══════════════════════════════════════════════════════
            //  Thực thi theo lựa chọn
            // ═══════════════════════════════════════════════════════
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
                    DeleteMLeaderAndLeader(ed);
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

        // ═══════════════════════════════════════════════════════════
        //  Xóa MLEADER + LEADER thường
        // ═══════════════════════════════════════════════════════════
        private void DeleteMLeaderAndLeader(Editor ed)
        {
            TypedValue[] filter = {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "MULTILEADER"),
                new TypedValue((int)DxfCode.Start, "LEADER"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.SelectAll(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show("Không có LEADER NOTE (MULTILEADER) hoặc LEADER nào trong bản vẽ.");
                return;
            }

            int cntMLeader = 0;
            int cntLeader = 0;

            using (Transaction tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent == null) continue;

                    if (ent is MLeader) cntMLeader++;
                    else if (ent is Leader) cntLeader++;

                    ent.Erase();
                }
                tr.Commit();
            }

            int total = cntMLeader + cntLeader;
            MessageBox.Show(
                $"Đã xóa {total} đối tượng:\n" +
                $"  • Leader Note (MLeader): {cntMLeader}\n" +
                $"  • Leader thường: {cntLeader}");
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
            return list.Count > 0
                ? list.OrderBy(x => x).ToList()
                : new List<string> { "Không có layer" };
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Windows Form
    // ═══════════════════════════════════════════════════════════
    public class DimDeleteForm : Form
    {
        private RadioButton rbSelected;
        private RadioButton rbByLayer;
        private RadioButton rbMLeader;
        private RadioButton rbByLayerSelect;
        private RadioButton rbAll;
        private ComboBox cboLayer;
        private Button btnPickLayer;
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
        public bool PickLayerRequested { get; private set; } = false;

        /// <summary>
        /// Constructor nhận thêm preSelectedOption để nhớ lựa chọn radio lần trước
        /// </summary>
        public DimDeleteForm(List<string> layers,
                             string preSelectedLayer,
                             string preSelectedOption = "selected_dim")
        {
            this.Text = "Xóa Dimension";
            this.Width = 560;
            this.Height = 400;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // ── Tiêu đề ──
            Label lblTitle = new Label
            {
                Text = "Chọn chức năng:",
                Left = 20,
                Top = 15,
                Width = 500,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            // ── Nhóm RadioButton ──
            Panel pnlOptions = new Panel
            {
                Left = 20,
                Top = 42,
                Width = 500,
                Height = 155
            };

            rbSelected = new RadioButton
            {
                Text = "1. Xóa DIMENSION được chọn",
                Left = 5,
                Top = 3,
                Width = 490
            };
            rbByLayer = new RadioButton
            {
                Text = "2. Xóa DIMENSION theo layer",
                Left = 5,
                Top = 30,
                Width = 490
            };
            rbMLeader = new RadioButton
            {
                Text = "3. Xóa LEADER NOTE (MLeader) + LEADER thường",
                Left = 5,
                Top = 57,
                Width = 490
            };
            rbByLayerSelect = new RadioButton
            {
                Text = "4. Xóa DIMENSION theo layer và vùng chọn",
                Left = 5,
                Top = 84,
                Width = 490
            };
            rbAll = new RadioButton
            {
                Text = "5. Xóa tất cả DIMENSION",
                Left = 5,
                Top = 111,
                Width = 490
            };

            pnlOptions.Controls.AddRange(new Control[]
            {
                rbSelected, rbByLayer, rbMLeader, rbByLayerSelect, rbAll
            });

            // ═══════════════════════════════════════════════════════
            //  KHÔI PHỤC option cuối cùng
            // ═══════════════════════════════════════════════════════
            switch (preSelectedOption)
            {
                case "dim_by_layer": rbByLayer.Checked = true; break;
                case "dim_mleader": rbMLeader.Checked = true; break;
                case "dim_by_layer_select": rbByLayerSelect.Checked = true; break;
                case "all_dim": rbAll.Checked = true; break;
                case "selected_dim":
                default: rbSelected.Checked = true; break;
            }

            // ── Layer ──
            Label lblLayer = new Label
            {
                Text = "Chọn Layer (cho chức năng 2 và 4):",
                Left = 20,
                Top = 215,
                Width = 240
            };

            cboLayer = new ComboBox
            {
                Left = 20,
                Top = 240,
                Width = 350,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboLayer.Items.AddRange(layers.ToArray());

            // Khôi phục layer đã chọn
            if (!string.IsNullOrEmpty(preSelectedLayer))
            {
                int idx = cboLayer.Items.IndexOf(preSelectedLayer);
                cboLayer.SelectedIndex = (idx >= 0) ? idx : 0;
            }
            else if (layers.Count > 0)
            {
                cboLayer.SelectedIndex = 0;
            }

            // ── Nút Pick Layer ──
            btnPickLayer = new Button
            {
                Text = "Pick Layer",
                Left = 380,
                Top = 238,
                Width = 140,
                Height = 28,
                BackColor = Color.FromArgb(220, 240, 220),
                FlatStyle = FlatStyle.System
            };
            btnPickLayer.Click += delegate
            {
                PickLayerRequested = true;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            // ── Buttons ──
            btnOK = new Button
            {
                Text = "OK",
                Left = 345,
                Top = 285,
                Width = 80,
                Height = 28
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 435,
                Top = 285,
                Width = 80,
                Height = 28
            };

            btnOK.Click += delegate
            {
                this.DialogResult = DialogResult.OK;
            };
            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
            };

            // ── Enable/disable layer controls theo option ──
            EventHandler onOptChanged = delegate
            {
                bool needLayer = rbByLayer.Checked || rbByLayerSelect.Checked;
                cboLayer.Enabled = needLayer;
                btnPickLayer.Enabled = needLayer;
                lblLayer.Enabled = needLayer;
            };
            rbSelected.CheckedChanged += onOptChanged;
            rbByLayer.CheckedChanged += onOptChanged;
            rbMLeader.CheckedChanged += onOptChanged;
            rbByLayerSelect.CheckedChanged += onOptChanged;
            rbAll.CheckedChanged += onOptChanged;

            this.Controls.AddRange(new Control[]
            {
                lblTitle, pnlOptions,
                lblLayer, cboLayer, btnPickLayer,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // Chạy 1 lần để set trạng thái ban đầu đúng với option đã chọn
            onOptChanged(null, EventArgs.Empty);
        }
    }
}