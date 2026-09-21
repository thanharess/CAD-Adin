using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using CADAddin.Common;                    // ← THÊM để gọi Utils

namespace CADAddin.Block
{
    public class BlockDelete
    {
        // Nhớ lựa chọn giữa các lần chạy
        private static string _lastOption = "sel_in_area";
        private static string _lastLayer = null;
        private static string _lastBlockName = null;

        // Lệnh chính — mở form chọn chế độ
        [CommandMethod("BLDELETE")]
        public void BlockDeleteCommand()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("Lệnh BLDELETE - Xóa block.");

            // ═══════════════════════════════════════════════════════
            //  Thu thập dữ liệu để đưa vào form
            // ═══════════════════════════════════════════════════════
            var layerNames = GetAllLayers(db);
            var blockNames = GetAllBlockNames(db);

            // ═══════════════════════════════════════════════════════
            //  Vòng lặp — cho phép pick layer rồi mở lại form
            // ═══════════════════════════════════════════════════════
            string choice;
            string layerName;
            string blockName;

            while (true)
            {
                using (var form = new BlockDeleteForm(
                    layerNames, blockNames,
                    _lastOption, _lastLayer, _lastBlockName))
                {
                    var dr = AcApp.ShowModalDialog(form);

                    // ── Pick Layer ──
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
                                if (ent != null) _lastLayer = ent.Layer;
                                tr.Commit();
                            }
                        }

                        // Lưu các lựa chọn khác
                        _lastOption = form.SelectedOption;
                        _lastBlockName = form.SelectedBlockName;
                        continue;
                    }

                    if (dr != DialogResult.OK)
                    {
                        Utils.Print("⏹️ Hủy lệnh.");
                        return;
                    }

                    choice = form.SelectedOption;
                    layerName = form.SelectedLayer;
                    blockName = form.SelectedBlockName;
                }

                _lastOption = choice;
                _lastLayer = layerName;
                _lastBlockName = blockName;

                break;
            }

            // ═══════════════════════════════════════════════════════
            //  Thực thi theo lựa chọn
            // ═══════════════════════════════════════════════════════
            switch (choice)
            {
                case "sel_by_pick":
                    DeleteByPickAndArea(ed, db);
                    break;

                case "all_same_name":
                    DeleteAllSameNameFromPick(ed, db);
                    break;

                case "all_blocks":
                    DeleteAllBlocks(ed, db);
                    break;

                case "by_layer":
                    if (string.IsNullOrEmpty(layerName))
                        MessageBox.Show("Chưa chọn Layer.");
                    else
                        DeleteBlocksByLayer(ed, layerName);
                    break;

                case "by_name":
                    if (string.IsNullOrEmpty(blockName))
                        MessageBox.Show("Chưa chọn tên Block.");
                    else
                        DeleteBlocksByName(ed, blockName);
                    break;

                case "sel_in_area":
                default:
                    DeleteSelectedInArea(ed, db);
                    break;
            }
        }

        // Giữ lại 2 lệnh cũ để tương thích
        [CommandMethod("BLERASE")]
        public void EraseAllSameName()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            DeleteAllSameNameFromPick(doc.Editor, doc.Database);
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 1 — Chọn block mẫu + chọn vùng → xóa
        // ═══════════════════════════════════════════════════════════
        private void DeleteByPickAndArea(Editor ed, Database db)
        {
            // ── Pick block mẫu ──
            var peo = new PromptEntityOptions("\nChọn block mẫu: ");
            peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peo.AddAllowedClass(typeof(BlockReference), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            string name;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                name = br.Name;
                tr.Commit();
            }
            Utils.Print($"🔹 Block được chọn: {name}");

            // ── Chọn vùng ──
            ed.WriteMessage("\nChọn vùng chứa block cần xóa:");
            var ss = ed.GetSelection(Utils.BlockNameFilter(name));
            if (ss.Status != PromptStatus.OK || ss.Value.Count == 0)
            {
                Utils.Print("❌ Không tìm thấy block trong vùng.");
                return;
            }

            EraseFromSelection(ed, db, ss, name);
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 2 — Chọn block mẫu → xóa tất cả cùng tên
        // ═══════════════════════════════════════════════════════════
        private void DeleteAllSameNameFromPick(Editor ed, Database db)
        {
            var peo = new PromptEntityOptions("\nChọn block mẫu: ");
            peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peo.AddAllowedClass(typeof(BlockReference), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            string name;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                name = br.Name;
                tr.Commit();
            }
            Utils.Print($"🔹 Block được chọn: {name}");

            var ss = ed.SelectAll(Utils.BlockNameFilter(name));
            if (ss.Status != PromptStatus.OK || ss.Value.Count == 0)
            {
                Utils.Print("❌ Không tìm thấy block.");
                return;
            }

            EraseFromSelection(ed, db, ss, name);
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 3 — Xóa TẤT CẢ BLOCK trong bản vẽ (mọi tên)
        // ═══════════════════════════════════════════════════════════
        private void DeleteAllBlocks(Editor ed, Database db)
        {
            var confirm = MessageBox.Show(
                "⚠ Bạn sắp xóa TẤT CẢ block trong bản vẽ!\n\n" +
                "Hành động này không thể hoàn tác.\n\nTiếp tục?",
                "Xác nhận xóa tất cả block",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
            {
                Utils.Print("⏹️ Đã hủy.");
                return;
            }

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "INSERT") });

            var ss = ed.SelectAll(filter);
            if (ss.Status != PromptStatus.OK || ss.Value.Count == 0)
            {
                Utils.Print("❌ Bản vẽ không có block nào.");
                return;
            }

            int cnt = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in ss.Value)
                {
                    if (so == null) continue;
                    var br = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as BlockReference;
                    if (br == null) continue;
                    br.Erase();
                    cnt++;
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"🗑️ Đã xóa {cnt} block (mọi loại) trong bản vẽ.");
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 4 — Xóa block theo Layer
        // ═══════════════════════════════════════════════════════════
        private void DeleteBlocksByLayer(Editor ed, string layerName)
        {
            TypedValue[] filterArr = {
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.LayerName, layerName)
            };
            var sf = new SelectionFilter(filterArr);
            var ss = ed.SelectAll(sf);

            if (ss.Status != PromptStatus.OK || ss.Value.Count == 0)
            {
                Utils.Print($"❌ Không có block nào trên layer '{layerName}'.");
                return;
            }

            int cnt = 0;
            using (var tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in ss.Value)
                {
                    if (so == null) continue;
                    var br = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as BlockReference;
                    if (br == null) continue;
                    br.Erase();
                    cnt++;
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"🗑️ Đã xóa {cnt} block trên layer '{layerName}'.");
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 5 — Xóa block theo tên (chọn từ list)
        // ═══════════════════════════════════════════════════════════
        private void DeleteBlocksByName(Editor ed, string blockName)
        {
            var ss = ed.SelectAll(Utils.BlockNameFilter(blockName));

            if (ss.Status != PromptStatus.OK || ss.Value.Count == 0)
            {
                Utils.Print($"❌ Không có block nào tên '{blockName}'.");
                return;
            }

            int cnt = 0;
            using (var tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in ss.Value)
                {
                    if (so == null) continue;
                    var br = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as BlockReference;
                    if (br == null) continue;
                    br.Erase();
                    cnt++;
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"🗑️ Đã xóa {cnt} block tên '{blockName}'.");
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG DEFAULT — Pick block mẫu + chọn vùng theo filter
        // ═══════════════════════════════════════════════════════════
        private void DeleteSelectedInArea(Editor ed, Database db)
        {
            DeleteByPickAndArea(ed, db);
        }

        // ═══════════════════════════════════════════════════════════
        //  Helper — xóa từ selection
        // ═══════════════════════════════════════════════════════════
        private void EraseFromSelection(Editor ed, Database db,
            PromptSelectionResult ss, string name)
        {
            int cnt = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in ss.Value)
                {
                    if (so == null) continue;
                    var br = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as BlockReference;
                    if (br == null) continue;
                    br.Erase();
                    cnt++;
                }
                tr.Commit();
            }
            ed.Regen();
            Utils.Print($"🗑️ Đã xóa {cnt} block \"{name}\".");
        }

        // ═══════════════════════════════════════════════════════════
        //  Lấy danh sách layer
        // ═══════════════════════════════════════════════════════════
        private static List<string> GetAllLayers(Database db)
        {
            var list = new List<string>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var lay = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (lay.Name != "Defpoints")
                        list.Add(lay.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // ═══════════════════════════════════════════════════════════
        //  Lấy danh sách tên block (đã lọc bỏ layout & anonymous)
        // ═══════════════════════════════════════════════════════════
        private static List<string> GetAllBlockNames(Database db)
        {
            var list = new List<string>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);

                    // Bỏ qua layout (*Model_Space, *Paper_Space...)
                    if (btr.IsLayout) continue;

                    // Bỏ qua anonymous block (*U1, *D1...)
                    if (btr.Name.StartsWith("*")) continue;

                    // Bỏ qua block không có reference nào trong bản vẽ
                    var refIds = btr.GetBlockReferenceIds(true, false);
                    if (refIds.Count == 0) continue;

                    list.Add(btr.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM
    // ═══════════════════════════════════════════════════════════
    public class BlockDeleteForm : Form
    {
        private RadioButton rbPickAndArea;
        private RadioButton rbAllSameName;
        private RadioButton rbAllBlocks;
        private RadioButton rbByLayer;
        private RadioButton rbByName;

        private ComboBox cboLayer;
        private Button btnPickLayer;
        private ComboBox cboBlockName;

        private Button btnOK;
        private Button btnCancel;

        private readonly List<string> _allLayers;
        private readonly List<string> _allBlocks;

        public string SelectedOption { get; private set; } = "sel_by_pick";
        public string SelectedLayer => cboLayer.Text?.Trim() ?? "";
        public string SelectedBlockName => cboBlockName.Text?.Trim() ?? "";
        public bool PickLayerRequested { get; private set; } = false;

        public BlockDeleteForm(List<string> layers, List<string> blocks,
            string preOption, string preLayer, string preBlock)
        {
            _allLayers = layers ?? new List<string>();
            _allBlocks = blocks ?? new List<string>();

            this.Text = "Xóa Block";
            this.ClientSize = new Size(520, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // ── Tiêu đề ──
            Label lblTitle = new Label
            {
                Text = "Chọn chức năng:",
                Left = 20,
                Top = 15,
                Width = 480,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            // ── Nhóm Radio ──
            Panel pnlOptions = new Panel
            {
                Left = 20,
                Top = 42,
                Width = 480,
                Height = 160
            };

            rbPickAndArea = new RadioButton
            {
                Text = "1. Chọn block mẫu → chọn vùng → xóa",
                Left = 5,
                Top = 3,
                Width = 470
            };
            rbAllSameName = new RadioButton
            {
                Text = "2. Chọn block mẫu → xóa tất cả block cùng tên",
                Left = 5,
                Top = 30,
                Width = 470
            };
            rbAllBlocks = new RadioButton
            {
                Text = "3. Xóa TẤT CẢ block trong bản vẽ (mọi loại)  ⚠",
                Left = 5,
                Top = 57,
                Width = 470,
                ForeColor = Color.DarkRed
            };
            rbByLayer = new RadioButton
            {
                Text = "4. Xóa tất cả block trên Layer được chọn",
                Left = 5,
                Top = 84,
                Width = 470
            };
            rbByName = new RadioButton
            {
                Text = "5. Xóa tất cả block theo Tên (chọn từ danh sách)",
                Left = 5,
                Top = 111,
                Width = 470
            };

            pnlOptions.Controls.AddRange(new Control[]
            {
                rbPickAndArea, rbAllSameName, rbAllBlocks, rbByLayer, rbByName
            });

            // ── Layer ──
            Label lblLayer = new Label
            {
                Text = "Layer:",
                Left = 20,
                Top = 215,
                Width = 60
            };
            cboLayer = new ComboBox
            {
                Left = 85,
                Top = 212,
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            cboLayer.Items.AddRange(_allLayers.ToArray());

            btnPickLayer = new Button
            {
                Text = "Pick Layer",
                Left = 395,
                Top = 210,
                Width = 105,
                Height = 26,
                BackColor = Color.FromArgb(220, 240, 220),
                FlatStyle = FlatStyle.System
            };

            // ── Block Name ──
            Label lblBlock = new Label
            {
                Text = "Block name:",
                Left = 20,
                Top = 250,
                Width = 60
            };
            cboBlockName = new ComboBox
            {
                Left = 85,
                Top = 247,
                Width = 415,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            cboBlockName.Items.AddRange(_allBlocks.ToArray());

            // ── Buttons ──
            btnOK = new Button
            {
                Text = "OK",
                Left = 320,
                Top = 320,
                Width = 85,
                Height = 30
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 415,
                Top = 320,
                Width = 85,
                Height = 30
            };

            // ═══════════════════════════════════════════════════════
            //  Khôi phục lựa chọn cũ
            // ═══════════════════════════════════════════════════════
            switch (preOption)
            {
                case "all_same_name": rbAllSameName.Checked = true; break;
                case "all_blocks": rbAllBlocks.Checked = true; break;
                case "by_layer": rbByLayer.Checked = true; break;
                case "by_name": rbByName.Checked = true; break;
                case "sel_by_pick":
                default: rbPickAndArea.Checked = true; break;
            }

            // Khôi phục layer
            if (!string.IsNullOrEmpty(preLayer))
            {
                int idx = cboLayer.Items.IndexOf(preLayer);
                if (idx >= 0) cboLayer.SelectedIndex = idx;
                else cboLayer.Text = preLayer;
            }
            else if (cboLayer.Items.Count > 0)
                cboLayer.SelectedIndex = 0;

            // Khôi phục block name
            if (!string.IsNullOrEmpty(preBlock))
            {
                int idx = cboBlockName.Items.IndexOf(preBlock);
                if (idx >= 0) cboBlockName.SelectedIndex = idx;
                else cboBlockName.Text = preBlock;
            }
            else if (cboBlockName.Items.Count > 0)
                cboBlockName.SelectedIndex = 0;

            // ═══════════════════════════════════════════════════════
            //  Events
            // ═══════════════════════════════════════════════════════
            btnPickLayer.Click += delegate
            {
                PickLayerRequested = true;
                SelectedOption = GetSelectedOptionFromRadio();
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            btnOK.Click += delegate
            {
                SelectedOption = GetSelectedOptionFromRadio();

                // Validate
                if (SelectedOption == "by_layer" && string.IsNullOrEmpty(SelectedLayer))
                {
                    MessageBox.Show("Vui lòng chọn 1 Layer.",
                        "Thiếu thông tin",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                if (SelectedOption == "by_name" && string.IsNullOrEmpty(SelectedBlockName))
                {
                    MessageBox.Show("Vui lòng chọn 1 Block name.",
                        "Thiếu thông tin",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                this.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
            };

            // ── Enable/disable các control theo option ──
            EventHandler onOptChanged = delegate
            {
                bool needLayer = rbByLayer.Checked;
                bool needBlock = rbByName.Checked;

                cboLayer.Enabled = needLayer;
                btnPickLayer.Enabled = needLayer;
                lblLayer.Enabled = needLayer;

                cboBlockName.Enabled = needBlock;
                lblBlock.Enabled = needBlock;
            };

            rbPickAndArea.CheckedChanged += onOptChanged;
            rbAllSameName.CheckedChanged += onOptChanged;
            rbAllBlocks.CheckedChanged += onOptChanged;
            rbByLayer.CheckedChanged += onOptChanged;
            rbByName.CheckedChanged += onOptChanged;

            this.Controls.AddRange(new Control[]
            {
                lblTitle, pnlOptions,
                lblLayer, cboLayer, btnPickLayer,
                lblBlock, cboBlockName,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // Khởi tạo trạng thái ban đầu
            onOptChanged(null, EventArgs.Empty);
        }

        private string GetSelectedOptionFromRadio()
        {
            if (rbPickAndArea.Checked) return "sel_by_pick";
            if (rbAllSameName.Checked) return "all_same_name";
            if (rbAllBlocks.Checked) return "all_blocks";
            if (rbByLayer.Checked) return "by_layer";
            if (rbByName.Checked) return "by_name";
            return "sel_by_pick";
        }
    }
}