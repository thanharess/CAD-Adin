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
    public class BlockExplode
    {
        // Nhớ lựa chọn giữa các lần chạy
        private static string _lastOption = "sel_by_pick";
        private static string _lastLayer = null;
        private static string _lastBlockName = null;

        [CommandMethod("BLEXPLODE")]
        public void ExplodeCommand()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("Lệnh BLEXPLODE - Phá khối (explode block).");

            // ═══════════════════════════════════════════════════════
            //  Thu thập dữ liệu cho form
            // ═══════════════════════════════════════════════════════
            var layerNames = GetAllLayers(db);
            var blockNames = GetAllBlockNames(db);

            // ═══════════════════════════════════════════════════════
            //  Vòng lặp — cho phép pick layer rồi mở lại form
            // ═══════════════════════════════════════════════════════
            string choice;
            string layerName;
            string blockName;
            bool alsoXref;

            while (true)
            {
                using (var form = new BlockExplodeForm(
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
                    alsoXref = form.AlsoExplodeXref;
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
                    ExplodeByPickAndArea(ed, db, alsoXref);
                    break;

                case "all_same_name":
                    ExplodeAllSameNameFromPick(ed, db, alsoXref);
                    break;

                case "all_blocks":
                    ExplodeAllBlocks(ed, db, alsoXref);
                    break;

                case "by_layer":
                    if (string.IsNullOrEmpty(layerName))
                        MessageBox.Show("Chưa chọn Layer.");
                    else
                        ExplodeByLayer(ed, layerName, alsoXref);
                    break;

                case "by_name":
                    if (string.IsNullOrEmpty(blockName))
                        MessageBox.Show("Chưa chọn tên Block.");
                    else
                        ExplodeByName(ed, blockName, alsoXref);
                    break;

                default:
                    ExplodeByPickAndArea(ed, db, alsoXref);
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 1 — Pick block mẫu + chọn vùng → explode
        // ═══════════════════════════════════════════════════════════
        private void ExplodeByPickAndArea(Editor ed, Database db, bool alsoXref)
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

            ed.WriteMessage($"\nChọn vùng chứa block '{name}' cần phá:");
            var ss = ed.GetSelection(Utils.BlockNameFilter(name));
            if (ss.Status != PromptStatus.OK || ss.Value.Count == 0)
            {
                Utils.Print("❌ Không tìm thấy block trong vùng.");
                return;
            }

            ExplodeFromSelection(ed, db, ss, name, alsoXref);
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 2 — Pick mẫu → explode tất cả cùng tên
        // ═══════════════════════════════════════════════════════════
        private void ExplodeAllSameNameFromPick(Editor ed, Database db, bool alsoXref)
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

            ExplodeFromSelection(ed, db, ss, name, alsoXref);
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 3 — Explode TẤT CẢ block trong bản vẽ
        // ═══════════════════════════════════════════════════════════
        private void ExplodeAllBlocks(Editor ed, Database db, bool alsoXref)
        {
            var confirm = MessageBox.Show(
                "⚠ Bạn sắp PHÁ TẤT CẢ block trong bản vẽ!\n\n" +
                "Hành động này không thể hoàn tác.\n\nTiếp tục?",
                "Xác nhận phá tất cả block",
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

            ExplodeFromSelection(ed, db, ss, "(tất cả)", alsoXref);
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 4 — Explode block theo Layer
        // ═══════════════════════════════════════════════════════════
        private void ExplodeByLayer(Editor ed, string layerName, bool alsoXref)
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

            ExplodeFromSelection(ed, db: null, sel: ss,
                displayName: $"layer '{layerName}'", alsoXref: alsoXref);
        }

        // ═══════════════════════════════════════════════════════════
        //  CHỨC NĂNG 5 — Explode block theo Tên
        // ═══════════════════════════════════════════════════════════
        private void ExplodeByName(Editor ed, string blockName, bool alsoXref)
        {
            var ss = ed.SelectAll(Utils.BlockNameFilter(blockName));

            if (ss.Status != PromptStatus.OK || ss.Value.Count == 0)
            {
                Utils.Print($"❌ Không có block nào tên '{blockName}'.");
                return;
            }

            ExplodeFromSelection(ed, db: null, sel: ss,
                displayName: $"'{blockName}'", alsoXref: alsoXref);
        }

        // ═══════════════════════════════════════════════════════════
        //  ★ HÀM CHÍNH — Explode từ selection
        // ═══════════════════════════════════════════════════════════
        private void ExplodeFromSelection(Editor ed, Database db,
            PromptSelectionResult sel, string displayName, bool alsoXref)
        {
            if (db == null) db = ed.Document.Database;

            int success = 0;
            int failed = 0;
            var failedReasons = new List<string>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // ═══════════════════════════════════════════════════
                //  Bước 1: Thu thập danh sách ObjectId cần xử lý
                // ═══════════════════════════════════════════════════
                var idsToExplode = new List<ObjectId>();

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    idsToExplode.Add(so.ObjectId);
                }

                // ═══════════════════════════════════════════════════
                //  Bước 2: Explode từng block
                //  ★ LƯU Ý: ExplodeToOwnerSpace đã tự xoá block gốc
                //     → KHÔNG gọi Erase() sau đó
                //     → Mở ForWrite để có thể modify
                // ═══════════════════════════════════════════════════
                foreach (var id in idsToExplode)
                {
                    try
                    {
                        if (id.IsErased || id.IsNull)
                            continue;

                        var br = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                        if (br == null) continue;

                        // ═══════════════════════════════════════════════════════
                        //  Kiểm tra XREF qua BlockTableRecord (không phải BlockReference)
                        // ═══════════════════════════════════════════════════════
                        bool isXref = false;
                        var btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        if (btr != null && btr.IsFromExternalReference)
                            isXref = true;

                        if (isXref && !alsoXref)
                        {
                            failed++;
                            failedReasons.Add($"{br.Name} (xref)");
                            continue;
                        }

                        // ⚠ KHÔNG gọi br.Erase() — ExplodeToOwnerSpace đã xoá block gốc
                        br.ExplodeToOwnerSpace();
                        success++;
                    }
                    catch (System.Exception ex)
                    {
                        failed++;
                        failedReasons.Add(ex.Message);
                    }
                }

                tr.Commit();
            }

            ed.Regen();

            // ═══════════════════════════════════════════════════════
            //  Báo cáo kết quả
            // ═══════════════════════════════════════════════════════
            Utils.Print($"💥 Đã phá khối {success} instance(s) {displayName}.");

            if (failed > 0)
            {
                Utils.Print($"⚠ Không thể phá {failed} block:");
                foreach (var reason in failedReasons.Take(5))
                    Utils.Print($"   • {reason}");
                if (failedReasons.Count > 5)
                    Utils.Print($"   ... và {failedReasons.Count - 5} cái khác");
            }
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
        //  Lấy danh sách tên block có trong bản vẽ
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

                    if (btr.IsLayout) continue;
                    if (btr.Name.StartsWith("*")) continue;

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
    public class BlockExplodeForm : Form
    {
        private RadioButton rbPickAndArea;
        private RadioButton rbAllSameName;
        private RadioButton rbAllBlocks;
        private RadioButton rbByLayer;
        private RadioButton rbByName;

        private ComboBox cboLayer;
        private Button btnPickLayer;
        private ComboBox cboBlockName;

        private CheckBox chkAlsoXref;                  // ← MỚI

        private Button btnOK;
        private Button btnCancel;

        private readonly List<string> _allLayers;
        private readonly List<string> _allBlocks;

        public string SelectedOption { get; private set; } = "sel_by_pick";
        public string SelectedLayer => cboLayer.Text?.Trim() ?? "";
        public string SelectedBlockName => cboBlockName.Text?.Trim() ?? "";
        public bool AlsoExplodeXref => chkAlsoXref.Checked;   // ← MỚI
        public bool PickLayerRequested { get; private set; } = false;

        public BlockExplodeForm(List<string> layers, List<string> blocks,
            string preOption, string preLayer, string preBlock)
        {
            _allLayers = layers ?? new List<string>();
            _allBlocks = blocks ?? new List<string>();

            this.Text = "Phá khối (Explode Block)";
            this.ClientSize = new Size(540, 425);
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
                Width = 500,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            // ── Nhóm Radio ──
            Panel pnlOptions = new Panel
            {
                Left = 20,
                Top = 42,
                Width = 500,
                Height = 160
            };

            rbPickAndArea = new RadioButton
            {
                Text = "1. Chọn block mẫu → chọn vùng → phá khối",
                Left = 5,
                Top = 3,
                Width = 490
            };
            rbAllSameName = new RadioButton
            {
                Text = "2. Chọn block mẫu → phá tất cả block cùng tên",
                Left = 5,
                Top = 30,
                Width = 490
            };
            rbAllBlocks = new RadioButton
            {
                Text = "3. Phá TẤT CẢ block trong bản vẽ (mọi loại)  ⚠",
                Left = 5,
                Top = 57,
                Width = 490,
                ForeColor = Color.DarkRed
            };
            rbByLayer = new RadioButton
            {
                Text = "4. Phá tất cả block trên Layer được chọn",
                Left = 5,
                Top = 84,
                Width = 490
            };
            rbByName = new RadioButton
            {
                Text = "5. Phá tất cả block theo Tên (chọn từ danh sách)",
                Left = 5,
                Top = 111,
                Width = 490
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
                Width = 125,
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
                Width = 435,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            cboBlockName.Items.AddRange(_allBlocks.ToArray());

            // ── CheckBox Xref ──
            chkAlsoXref = new CheckBox
            {
                Text = "Cũng phá khối XREF (không khuyến khích)",
                Left = 20,
                Top = 288,
                Width = 500,
                Height = 22,
                ForeColor = Color.DarkRed
            };

            // ── Buttons ──
            btnOK = new Button
            {
                Text = "OK",
                Left = 340,
                Top = 340,
                Width = 85,
                Height = 30
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 435,
                Top = 340,
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

            if (!string.IsNullOrEmpty(preLayer))
            {
                int idx = cboLayer.Items.IndexOf(preLayer);
                if (idx >= 0) cboLayer.SelectedIndex = idx;
                else cboLayer.Text = preLayer;
            }
            else if (cboLayer.Items.Count > 0)
                cboLayer.SelectedIndex = 0;

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

                if (SelectedOption == "by_layer" && string.IsNullOrEmpty(SelectedLayer))
                {
                    MessageBox.Show("Vui lòng chọn 1 Layer.",
                        "Thiếu thông tin",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                if (SelectedOption == "by_name" && string.IsNullOrEmpty(SelectedBlockName))
                {
                    MessageBox.Show("Vui lòng chọn 1 Block name.",
                        "Thiếu thông tin",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                this.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
            };

            // ── Enable/disable control theo option ──
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
                chkAlsoXref,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

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