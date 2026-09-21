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
    public class BlockLayerChange
    {
        // Nhớ cấu hình giữa các lần chạy
        private static string _lastLayer = null;
        private static bool _lastIncludeNested = true;
        private static bool _lastApplyToAll = true;
        private static bool _lastSkipDim = true;      // ← MỚI: mặc định bỏ qua dim

        [CommandMethod("LAYERCHANGEBLOCK")]
        public void ChangeLayer()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("Lệnh LAYERCHANGEBLOCK - Đổi layer entity bên trong block.");

            // ═══════════════════════════════════════════════════════
            //  Bước 1: Chọn các block INSERT cần xử lý
            // ═══════════════════════════════════════════════════════
            ed.WriteMessage("\nChọn các block (INSERT) cần đổi layer entity bên trong:");
            var sel = ed.GetSelection(new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "INSERT") }));

            if (sel.Status != PromptStatus.OK || sel.Value.Count == 0)
            {
                Utils.Print("⏹️ Không chọn block nào.");
                return;
            }

            var layerNames = GetAllLayers(db);
            if (layerNames.Count == 0)
            {
                Utils.Print("❌ Bản vẽ chưa có layer nào.");
                return;
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 2: Vòng lặp — Form có thể đóng để Pick Layer rồi mở lại
            // ═══════════════════════════════════════════════════════
            string targetLayer;
            bool includeNested;
            bool applyToAllSameName;
            bool skipDim;                              // ← MỚI

            while (true)
            {
                using (var form = new BlockLayerChangeForm(
                    layerNames, _lastLayer, sel.Value.Count,
                    _lastIncludeNested, _lastApplyToAll, _lastSkipDim))
                {
                    var dr = AcApp.ShowModalDialog(form);

                    // ── User bấm "Pick Layer" → đóng form để pick ──
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
                                    _lastLayer = ent.Layer;
                                tr.Commit();
                            }
                        }
                        // Lưu các tùy chọn khác để mở lại form
                        _lastIncludeNested = form.IncludeNested;
                        _lastApplyToAll = form.ApplyToAllSameName;
                        _lastSkipDim = form.SkipDimension;

                        continue;   // quay lại mở form
                    }

                    // ── User bấm Cancel ──
                    if (dr != DialogResult.OK)
                    {
                        Utils.Print("⏹️ Hủy lệnh.");
                        return;
                    }

                    targetLayer = form.SelectedLayer;
                    includeNested = form.IncludeNested;
                    applyToAllSameName = form.ApplyToAllSameName;
                    skipDim = form.SkipDimension;    // ← MỚI
                }
                break;
            }

            if (string.IsNullOrEmpty(targetLayer))
            {
                Utils.Print("❌ Chưa chọn layer đích.");
                return;
            }

            // Lưu cấu hình cho lần sau
            _lastLayer = targetLayer;
            _lastIncludeNested = includeNested;
            _lastApplyToAll = applyToAllSameName;
            _lastSkipDim = skipDim;

            // ═══════════════════════════════════════════════════════
            //  Bước 3: Mở rộng selection nếu "áp dụng cho tất cả cùng tên"
            // ═══════════════════════════════════════════════════════
            var targetBlockIds = new HashSet<ObjectId>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var selectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    targetBlockIds.Add(so.ObjectId);

                    if (applyToAllSameName)
                    {
                        var br = tr.GetObject(so.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (br != null) selectedNames.Add(br.Name);
                    }
                }

                if (applyToAllSameName && selectedNames.Count > 0)
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    foreach (var btrId in bt)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                        if (btr.IsLayout) continue;
                        if (!selectedNames.Contains(btr.Name)) continue;

                        foreach (ObjectId refId in btr.GetBlockReferenceIds(true, true))
                        {
                            targetBlockIds.Add(refId);
                        }
                    }
                }

                tr.Commit();
            }

            Utils.Print($"→ Sẽ xử lý {targetBlockIds.Count} block ref.");

            // ═══════════════════════════════════════════════════════
            //  Bước 4: Thu thập block definition cần sửa
            // ═══════════════════════════════════════════════════════
            var btrIdsToFix = new HashSet<ObjectId>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var id in targetBlockIds)
                {
                    var br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (br == null) continue;

                    CollectBlockTableRecords(br.BlockTableRecord, tr, btrIdsToFix, includeNested);
                }
                tr.Commit();
            }

            Utils.Print($"→ Có {btrIdsToFix.Count} block definition cần sửa.");

            // ═══════════════════════════════════════════════════════
            //  Bước 5: Đổi layer — BỎ QUA DIM nếu user chọn
            // ═══════════════════════════════════════════════════════
            int totalEntities = 0;
            int skippedDims = 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var btrId in btrIdsToFix)
                {
                    var btr = tr.GetObject(btrId, OpenMode.ForWrite) as BlockTableRecord;
                    if (btr == null || btr.IsLayout) continue;

                    foreach (ObjectId eid in btr)
                    {
                        var ent = tr.GetObject(eid, OpenMode.ForWrite) as Entity;
                        if (ent == null) continue;

                        // ── Bỏ qua DIMENSION nếu user chọn ──
                        if (skipDim && ent is Dimension)
                        {
                            skippedDims++;
                            continue;
                        }

                        // Đổi layer
                        if (ent.Layer != targetLayer)
                        {
                            ent.Layer = targetLayer;
                            totalEntities++;
                        }
                    }
                }
                tr.Commit();
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 6: Regen + update block refs
            // ═══════════════════════════════════════════════════════
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var id in targetBlockIds)
                {
                    var br = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                    if (br != null) br.RecordGraphicsModified(true);
                }
                tr.Commit();
            }

            ed.Regen();

            Utils.Print($"✔ Đã đổi layer {totalEntities} entity trong {btrIdsToFix.Count} block definition.");
            Utils.Print($"  Layer đích: {targetLayer}");
            if (skipDim)
                Utils.Print($"  ⚠ Đã bỏ qua {skippedDims} DIMENSION bên trong block.");
        }

        // ═══════════════════════════════════════════════════════════
        //  Thu thập BlockTableRecord cần sửa (bao gồm anonymous + nested)
        // ═══════════════════════════════════════════════════════════
        private static void CollectBlockTableRecords(
            ObjectId btrId, Transaction tr, HashSet<ObjectId> result, bool includeNested)
        {
            if (btrId.IsNull || result.Contains(btrId)) return;

            var btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
            if (btr == null || btr.IsLayout) return;

            result.Add(btrId);

            if (btr.IsDynamicBlock)
            {
                foreach (ObjectId anonId in btr.GetAnonymousBlockIds())
                {
                    if (!result.Contains(anonId)) result.Add(anonId);
                }
            }

            if (includeNested)
            {
                foreach (ObjectId eid in btr)
                {
                    var ent = tr.GetObject(eid, OpenMode.ForRead) as Entity;
                    if (ent is BlockReference nestedBr)
                    {
                        CollectBlockTableRecords(nestedBr.BlockTableRecord, tr, result, true);
                    }
                }
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
            return list.OrderBy(x => x).ToList();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM
    // ═══════════════════════════════════════════════════════════
    public class BlockLayerChangeForm : Form
    {
        private TextBox txtFilter;
        private ComboBox cboLayer;
        private Button btnPickLayer;
        private CheckBox chkIncludeNested;
        private CheckBox chkApplyToAll;
        private CheckBox chkSkipDim;                 // ← MỚI
        private Button btnOK;
        private Button btnCancel;

        private readonly List<string> _allLayers;

        public string SelectedLayer => cboLayer.Text?.Trim() ?? "";
        public bool IncludeNested => chkIncludeNested.Checked;
        public bool ApplyToAllSameName => chkApplyToAll.Checked;
        public bool SkipDimension => chkSkipDim.Checked;    // ← MỚI
        public bool PickLayerRequested { get; private set; } = false;

        public BlockLayerChangeForm(List<string> layers, string preLayer, int selectedCount,
            bool preIncludeNested = true, bool preApplyToAll = true, bool preSkipDim = true)
        {
            _allLayers = layers ?? new List<string>();

            this.Text = "Đổi Layer cho entity trong Block";
            this.ClientSize = new Size(460, 335);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // ── Label đếm block ──
            Label lblSelected = new Label
            {
                Text = $"📌 Đang chọn: {selectedCount} block",
                Left = 20,
                Top = 15,
                Width = 420,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor = System.Drawing. Color.DarkGreen
            };

            // ── Ô lọc ──
            Label lblFilter = new Label
            {
                Text = "Lọc:",
                Left = 20,
                Top = 48,
                Width = 40
            };
            txtFilter = new TextBox
            {
                Left = 65,
                Top = 45,
                Width = 375
            };
            txtFilter.TextChanged += delegate { ApplyFilter(); };

            // ── ComboBox layer ──
            Label lblLayer = new Label
            {
                Text = "Layer đích:",
                Left = 20,
                Top = 88,
                Width = 80
            };
            cboLayer = new ComboBox
            {
                Left = 105,
                Top = 85,
                Width = 225,
                DropDownStyle = ComboBoxStyle.DropDown
            };

            ReloadCombo(_allLayers);

            if (!string.IsNullOrEmpty(preLayer))
            {
                int idx = cboLayer.Items.IndexOf(preLayer);
                if (idx >= 0) cboLayer.SelectedIndex = idx;
                else cboLayer.Text = preLayer;
            }
            else if (cboLayer.Items.Count > 0)
            {
                cboLayer.SelectedIndex = 0;
            }

            // ═══════════════════════════════════════════════════════
            //  Nút Pick Layer — đóng form để pick thật sự
            // ═══════════════════════════════════════════════════════
            btnPickLayer = new Button
            {
                Text = "Pick Layer",
                Left = 340,
                Top = 83,
                Width = 100,
                Height = 28,
                BackColor = Color.FromArgb(220, 240, 220),
                FlatStyle = FlatStyle.System
            };
            btnPickLayer.Click += delegate
            {
                PickLayerRequested = true;
                this.DialogResult = DialogResult.Cancel;   // đóng form
                this.Close();
            };

            // ── Tùy chọn ──
            chkIncludeNested = new CheckBox
            {
                Text = "Bao gồm block lồng nhau (nested blocks)",
                Left = 20,
                Top = 128,
                Width = 420,
                Height = 22,
                Checked = preIncludeNested,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkBlue
            };

            chkApplyToAll = new CheckBox
            {
                Text = "Áp dụng cho TẤT CẢ block cùng tên trong bản vẽ",
                Left = 20,
                Top = 155,
                Width = 420,
                Height = 22,
                Checked = preApplyToAll,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkGreen
            };

            // ═══════════════════════════════════════════════════════
            //  Tùy chọn MỚI — bỏ qua DIMENSION
            // ═══════════════════════════════════════════════════════
            chkSkipDim = new CheckBox
            {
                Text = "⚠ Không áp dụng cho DIMENSION bên trong block",
                Left = 20,
                Top = 182,
                Width = 420,
                Height = 22,
                Checked = preSkipDim,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkRed
            };

            // ── Hint ──
            Label lblHint = new Label
            {
                Text = "💡 Nếu chọn ô trên → DIM bên trong block giữ nguyên layer.\n" +
                       "    Bỏ trống ô trên → DIM cũng bị đổi layer như mọi entity.",
                Left = 20,
                Top = 210,
                Width = 420,
                Height = 40,
                ForeColor = System.Drawing.Color.DimGray,
                Font = new System.Drawing.Font(this.Font, FontStyle.Italic)
            };

            // ── Buttons ──
            btnOK = new Button
            {
                Text = "OK",
                Left = 260,
                Top = 265,
                Width = 85,
                Height = 30
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 355,
                Top = 265,
                Width = 85,
                Height = 30
            };

            btnOK.Click += delegate
            {
                if (string.IsNullOrEmpty(SelectedLayer))
                {
                    MessageBox.Show(
                        "Vui lòng chọn 1 Layer.",
                        "Lỗi",
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

            this.Controls.AddRange(new Control[]
            {
                lblSelected,
                lblFilter, txtFilter,
                lblLayer, cboLayer, btnPickLayer,
                chkIncludeNested, chkApplyToAll, chkSkipDim,
                lblHint,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            this.Shown += delegate { txtFilter.Focus(); };
        }

        private void ApplyFilter()
        {
            string filter = txtFilter.Text?.Trim() ?? "";
            string current = cboLayer.Text?.Trim();

            List<string> src = string.IsNullOrEmpty(filter)
                ? _allLayers
                : _allLayers
                    .Where(x => x.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            ReloadCombo(src);

            if (!string.IsNullOrEmpty(current))
                cboLayer.Text = current;
            else if (cboLayer.Items.Count > 0)
                cboLayer.SelectedIndex = 0;
        }

        private void ReloadCombo(List<string> items)
        {
            cboLayer.BeginUpdate();
            cboLayer.Items.Clear();
            cboLayer.Items.AddRange(items.ToArray());
            cboLayer.EndUpdate();
        }
    }
}