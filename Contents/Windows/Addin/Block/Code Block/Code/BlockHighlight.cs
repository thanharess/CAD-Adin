using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using CADAddin.Common;

// ═══════════════════════════════════════════════════════════
//  ALIAS để tránh xung đột Color giữa System.Drawing và AutoCAD
// ═══════════════════════════════════════════════════════════
using AcadColor = Autodesk.AutoCAD.Colors.Color;

namespace CADAddin.Block
{
    public class BlockHighlight
    {
        // Nhớ lựa chọn lần trước
        private static int _lastColorIndex = 1;
        private static bool _lastUseColorMode = false;

        [CommandMethod("BLHLAYER")]
        public void Highlight()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var db = doc.Database;

            // ═══════════════════════════════════════════════════════
            //  Bước 1: Pick block mẫu
            // ═══════════════════════════════════════════════════════
            var peo = new PromptEntityOptions("\nChọn một block (INSERT): ");
            peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peo.AddAllowedClass(typeof(BlockReference), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            string blkName;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                blkName = br.Name;
                tr.Commit();
            }
            Utils.Print($"🔹 Block được chọn: {blkName}");

            // ═══════════════════════════════════════════════════════
            //  Bước 2: Form chọn chế độ + màu + tùy chọn
            // ═══════════════════════════════════════════════════════
            int colorIndex;
            bool highlightAll;
            bool keepExistingColor;
            bool useColorMode;

            using (var form = new BlockHighlightForm(
                blkName, _lastColorIndex, _lastUseColorMode))
            {
                if (AcApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    Utils.Print("⏹️ Hủy lệnh.");
                    return;
                }
                colorIndex = form.ColorIndex;
                highlightAll = form.HighlightAll;
                keepExistingColor = form.KeepExistingLayerColor;
                useColorMode = form.UseColorMode;
            }
            _lastColorIndex = colorIndex;
            _lastUseColorMode = useColorMode;

            // ═══════════════════════════════════════════════════════
            //  Bước 3: Danh sách block refs cần highlight
            // ═══════════════════════════════════════════════════════
            const string HL_LAYER = "BM_HIGHLIGHT";
            var blockRefIds = new List<ObjectId>();

            if (highlightAll)
            {
                var ss = ed.SelectAll(Utils.BlockNameFilter(blkName));
                if (ss.Status != PromptStatus.OK || ss.Value.Count == 0)
                {
                    Utils.Print("❌ Không tìm thấy block cùng tên.");
                    return;
                }
                foreach (SelectedObject so in ss.Value)
                    if (so != null) blockRefIds.Add(so.ObjectId);
            }
            else
            {
                blockRefIds.Add(per.ObjectId);
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 4: Áp dụng highlight
            // ═══════════════════════════════════════════════════════
            // Lưu trạng thái gốc — tùy chế độ mà lưu layer hay color
            var oldBlockRefLayers = new Dictionary<ObjectId, string>();
            var oldEntityLayers = new Dictionary<ObjectId, string>();
            var oldBlockRefColors = new Dictionary<ObjectId, int>();
            var oldEntityColors = new Dictionary<ObjectId, int>();

            int entityCount = 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // ── Thu thập block definitions (bao gồm nested + anonymous) ──
                var btrIdsToFix = new HashSet<ObjectId>();
                foreach (var id in blockRefIds)
                {
                    var br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (br == null) continue;
                    CollectBlockTableRecords(br.BlockTableRecord, tr, btrIdsToFix, includeNested: true);
                }

                if (useColorMode)
                {
                    // ═══════════════════════════════════════════════
                    //  CHẾ ĐỘ 2 — ĐỔI MÀU TRỰC TIẾP
                    // ═══════════════════════════════════════════════
                    Utils.Print($"→ Chế độ: ĐỔI MÀU TRỰC TIẾP (ACI = {colorIndex})");

                    // Đổi màu cho block reference
                    foreach (var id in blockRefIds)
                    {
                        var br = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                        if (br == null) continue;

                        oldBlockRefColors[id] = br.ColorIndex;
                        br.ColorIndex = (short)colorIndex;
                        br.RecordGraphicsModified(true);
                    }

                    // Đổi màu cho entity con trong block def
                    foreach (var btrId in btrIdsToFix)
                    {
                        var btr = tr.GetObject(btrId, OpenMode.ForWrite) as BlockTableRecord;
                        if (btr == null || btr.IsLayout) continue;

                        foreach (ObjectId eid in btr)
                        {
                            var ent = tr.GetObject(eid, OpenMode.ForWrite) as Entity;
                            if (ent == null) continue;

                            if (ent.ColorIndex == colorIndex) continue;

                            if (!oldEntityColors.ContainsKey(eid))
                            {
                                oldEntityColors[eid] = ent.ColorIndex;
                                ent.ColorIndex = (short)colorIndex;
                                entityCount++;
                            }
                        }
                    }

                    tr.Commit();

                    Utils.Print($"🔍 Highlight {blockRefIds.Count} block ref + {entityCount} entity con → MÀU ACI {colorIndex}.");
                }
                else
                {
                    // ═══════════════════════════════════════════════
                    //  CHẾ ĐỘ 1 — ĐỔI LAYER
                    // ═══════════════════════════════════════════════
                    Utils.Print($"→ Chế độ: ĐỔI LAYER sang '{HL_LAYER}'");

                    // Tạo/cập nhật layer HL_LAYER
                    var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    if (!lt.Has(HL_LAYER))
                    {
                        lt.UpgradeOpen();
                        var lay = new LayerTableRecord
                        {
                            Name = HL_LAYER,
                            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, (short)colorIndex),
                            IsFrozen = false,
                            IsOff = false,
                            IsLocked = false
                        };
                        lt.Add(lay);
                        tr.AddNewlyCreatedDBObject(lay, true);
                        Utils.Print($"  → Tạo layer '{HL_LAYER}' với màu mới.");
                    }
                    else
                    {
                        lt.UpgradeOpen();
                        var lay = (LayerTableRecord)tr.GetObject(lt[HL_LAYER], OpenMode.ForWrite);

                        if (!keepExistingColor)
                        {
                            lay.Color = AcadColor.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);
                            Utils.Print($"  → Cập nhật màu layer '{HL_LAYER}'.");
                        }
                        else
                        {
                            Utils.Print($"  → Giữ nguyên màu layer '{HL_LAYER}' (user đã setup).");
                        }

                        if (lay.IsFrozen) { lay.IsFrozen = false; Utils.Print("  (bỏ frozen)"); }
                        if (lay.IsOff) { lay.IsOff = false; Utils.Print("  (bật layer)"); }
                        if (lay.IsLocked) { lay.IsLocked = false; Utils.Print("  (unlock)"); }
                    }

                    // Đổi layer của block ref
                    foreach (var id in blockRefIds)
                    {
                        var br = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                        if (br == null) continue;

                        oldBlockRefLayers[id] = br.Layer;
                        br.Layer = HL_LAYER;
                        br.RecordGraphicsModified(true);
                    }

                    // Đổi layer của entity con trong block def
                    foreach (var btrId in btrIdsToFix)
                    {
                        var btr = tr.GetObject(btrId, OpenMode.ForWrite) as BlockTableRecord;
                        if (btr == null || btr.IsLayout) continue;

                        foreach (ObjectId eid in btr)
                        {
                            var ent = tr.GetObject(eid, OpenMode.ForWrite) as Entity;
                            if (ent == null) continue;

                            if (ent.Layer == HL_LAYER) continue;

                            if (!oldEntityLayers.ContainsKey(eid))
                            {
                                oldEntityLayers[eid] = ent.Layer;
                                ent.Layer = HL_LAYER;
                                entityCount++;
                            }
                        }
                    }

                    tr.Commit();

                    Utils.Print($"🔍 Highlight {blockRefIds.Count} block ref + {entityCount} entity con → layer '{HL_LAYER}'.");
                }
            }

            ed.Regen();

            // ═══════════════════════════════════════════════════════
            //  Bước 5: Prompt cho phép zoom/pan/select tự do
            // ═══════════════════════════════════════════════════════
            ed.WriteMessage("\n─────────────────────────────────────────────────");
            ed.WriteMessage("\n🖱️  Bạn có thể ZOOM / PAN / chọn đối tượng để xem vị trí.");
            ed.WriteMessage("\n    Khi xem xong:");

            var kwo = new PromptKeywordOptions(
                "\n[Enter = khôi phục gốc / G = giữ nguyên highlight] <khôi phục>: ")
            { AllowNone = true };
            kwo.Keywords.Add("G");
            kwo.Keywords.Add("GiuNguyen");

            var kwr = ed.GetKeywords(kwo);

            bool keepHighlight =
                (kwr.Status == PromptStatus.OK &&
                 (kwr.StringResult == "G" || kwr.StringResult == "GiuNguyen"))
                || kwr.Status == PromptStatus.Cancel;

            if (keepHighlight)
            {
                Utils.Print("✅ Giữ nguyên highlight.");
                return;
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 6: Khôi phục trạng thái gốc
            // ═══════════════════════════════════════════════════════
            using (var tr = db.TransactionManager.StartTransaction())
            {
                if (useColorMode)
                {
                    // Khôi phục màu block ref
                    foreach (var kv in oldBlockRefColors)
                    {
                        var br = tr.GetObject(kv.Key, OpenMode.ForWrite) as BlockReference;
                        if (br == null) continue;
                        br.ColorIndex = (short)kv.Value;
                        br.RecordGraphicsModified(true);
                    }

                    // Khôi phục màu entity con
                    foreach (var kv in oldEntityColors)
                    {
                        var ent = tr.GetObject(kv.Key, OpenMode.ForWrite) as Entity;
                        if (ent == null) continue;
                        ent.ColorIndex = (short)kv.Value;
                    }
                }
                else
                {
                    // Khôi phục layer block ref
                    foreach (var kv in oldBlockRefLayers)
                    {
                        var br = tr.GetObject(kv.Key, OpenMode.ForWrite) as BlockReference;
                        if (br == null) continue;
                        br.Layer = kv.Value;
                        br.RecordGraphicsModified(true);
                    }

                    // Khôi phục layer entity con
                    foreach (var kv in oldEntityLayers)
                    {
                        var ent = tr.GetObject(kv.Key, OpenMode.ForWrite) as Entity;
                        if (ent == null) continue;
                        ent.Layer = kv.Value;
                    }
                }

                tr.Commit();
            }

            ed.Regen();
            Utils.Print("✅ Đã khôi phục trạng thái gốc.");
        }

        // ═══════════════════════════════════════════════════════════
        //  Thu thập BlockTableRecord (bao gồm anonymous + nested)
        // ═══════════════════════════════════════════════════════════
        private static void CollectBlockTableRecords(
            ObjectId btrId, Transaction tr, HashSet<ObjectId> result, bool includeNested)
        {
            if (btrId.IsNull || result.Contains(btrId)) return;

            var btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
            if (btr == null || btr.IsLayout) return;

            result.Add(btrId);

            // Dynamic block → thêm anonymous blocks
            if (btr.IsDynamicBlock)
            {
                foreach (ObjectId anonId in btr.GetAnonymousBlockIds())
                {
                    if (!result.Contains(anonId)) result.Add(anonId);
                }
            }

            // Đệ quy vào nested block
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
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM — có 2 chế độ: Đổi Layer / Đổi Màu trực tiếp
    // ═══════════════════════════════════════════════════════════
    public class BlockHighlightForm : Form
    {
        // Chế độ
        private RadioButton rbModeLayer;
        private RadioButton rbModeColor;

        // Màu
        private RadioButton rbRed;
        private RadioButton rbYellow;
        private RadioButton rbGreen;
        private RadioButton rbCyan;
        private RadioButton rbBlue;
        private RadioButton rbMagenta;

        // Tùy chọn
        private CheckBox chkHighlightAll;
        private CheckBox chkKeepColor;

        private Button btnOK;
        private Button btnCancel;

        private static readonly int[] COLOR_INDICES = { 1, 2, 3, 4, 5, 6 };

        public int ColorIndex { get; private set; } = 1;
        public bool HighlightAll => chkHighlightAll.Checked;
        public bool KeepExistingLayerColor => chkKeepColor.Checked;
        public bool UseColorMode => rbModeColor.Checked;

        public BlockHighlightForm(string blkName, int preColor, bool preUseColorMode)
        {
            this.Text = "Highlight Block";
            this.ClientSize = new Size(430, 440);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // ── Tên block ──
            Label lblBlock = new Label
            {
                Text = "Block:",
                Left = 20,
                Top = 18,
                Width = 50
            };
            Label lblBlockName = new Label
            {
                Text = blkName,
                Left = 75,
                Top = 18,
                Width = 335,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor =     System.Drawing.Color.DarkBlue
            };

            // ═══════════════════════════════════════════════════════
            //  CHẾ ĐỘ HIGHLIGHT
            // ═══════════════════════════════════════════════════════
            Label lblMode = new Label
            {
                Text = "Chế độ highlight:",
                Left = 20,
                Top = 48,
                Width = 380,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            Panel pnlMode = new Panel
            {
                Left = 20,
                Top = 72,
                Width = 390,
                Height = 55
            };

            rbModeLayer = new RadioButton
            {
                Text = "1. Đổi sang Layer BM_HIGHLIGHT (giữ layer chuẩn)",
                Left = 5,
                Top = 3,
                Width = 380,
                Checked = !preUseColorMode
            };
            rbModeColor = new RadioButton
            {
                Text = "2. Đổi MÀU trực tiếp (không dùng layer)",
                Left = 5,
                Top = 28,
                Width = 380,
                Checked = preUseColorMode,
                ForeColor = System.Drawing.Color.DarkRed
            };

            pnlMode.Controls.AddRange(new Control[] { rbModeLayer, rbModeColor });

            // ═══════════════════════════════════════════════════════
            //  CHỌN MÀU
            // ═══════════════════════════════════════════════════════
            Label lblColor = new Label
            {
                Text = "Chọn màu highlight:",
                Left = 20,
                Top = 138,
                Width = 360,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            Panel pnlColors = new Panel
            {
                Left = 20,
                Top = 162,
                Width = 390,
                Height = 90
            };

            rbRed = MakeColorRadio("Đỏ", System.Drawing.Color.Red, 5, 3);
            rbYellow = MakeColorRadio("Vàng", System.Drawing.Color.Gold, 5, 30);
            rbGreen = MakeColorRadio("Xanh lá", System.Drawing.Color.LimeGreen, 5, 57);
            rbCyan = MakeColorRadio("Cyan", System.Drawing.Color.Cyan, 200, 3);
            rbBlue = MakeColorRadio("Xanh dương", System.Drawing.Color.DeepSkyBlue, 200, 30);
            rbMagenta = MakeColorRadio("Tím / Hồng", System.Drawing.Color.Magenta, 200, 57);

            pnlColors.Controls.AddRange(new Control[]
            {
                rbRed, rbYellow, rbGreen, rbCyan, rbBlue, rbMagenta
            });

            // ═══════════════════════════════════════════════════════
            //  TÙY CHỌN
            // ═══════════════════════════════════════════════════════
            chkHighlightAll = new CheckBox
            {
                Text = "Highlight TẤT CẢ block cùng tên trong bản vẽ",
                Left = 20,
                Top = 260,
                Width = 390,
                Height = 22,
                Checked = true,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkGreen
            };

            chkKeepColor = new CheckBox
            {
                Text = "Giữ màu layer có sẵn (chỉ dùng chế độ 1)",
                Left = 20,
                Top = 288,
                Width = 390,
                Height = 22,
                Checked = true,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkRed
            };

            // ═══════════════════════════════════════════════════════
            //  Hint
            // ═══════════════════════════════════════════════════════
            Label lblHint = new Label
            {
                Text = "💡 Sau khi highlight, bạn có thể zoom/pan để xem vị trí.\n" +
                       "    Nhấn Enter để khôi phục — nhấn G để giữ nguyên.",
                Left = 20,
                Top = 318,
                Width = 390,
                Height = 40,
                ForeColor = System.Drawing.Color.DimGray,
                Font = new System.Drawing.Font(this.Font, FontStyle.Italic)
            };

            // ═══════════════════════════════════════════════════════
            //  Buttons
            // ═══════════════════════════════════════════════════════
            btnOK = new Button
            {
                Text = "OK",
                Left = 230,
                Top = 370,
                Width = 85,
                Height = 30
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 325,
                Top = 370,
                Width = 85,
                Height = 30
            };

            btnOK.Click += delegate
            {
                if (rbRed.Checked) ColorIndex = COLOR_INDICES[0];
                else if (rbYellow.Checked) ColorIndex = COLOR_INDICES[1];
                else if (rbGreen.Checked) ColorIndex = COLOR_INDICES[2];
                else if (rbCyan.Checked) ColorIndex = COLOR_INDICES[3];
                else if (rbBlue.Checked) ColorIndex = COLOR_INDICES[4];
                else if (rbMagenta.Checked) ColorIndex = COLOR_INDICES[5];

                this.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
            };

            // ── Enable/disable chkKeepColor theo chế độ ──
            EventHandler onModeChanged = delegate
            {
                chkKeepColor.Enabled = rbModeLayer.Checked;
            };
            rbModeLayer.CheckedChanged += onModeChanged;
            rbModeColor.CheckedChanged += onModeChanged;

            // ── Khôi phục màu đã chọn ──
            switch (preColor)
            {
                case 1: rbRed.Checked = true; break;
                case 2: rbYellow.Checked = true; break;
                case 3: rbGreen.Checked = true; break;
                case 4: rbCyan.Checked = true; break;
                case 5: rbBlue.Checked = true; break;
                case 6: rbMagenta.Checked = true; break;
                default: rbRed.Checked = true; break;
            }

            this.Controls.AddRange(new Control[]
            {
                lblBlock, lblBlockName,
                lblMode, pnlMode,
                lblColor, pnlColors,
                chkHighlightAll, chkKeepColor,
                lblHint,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // Khởi tạo trạng thái ban đầu
            onModeChanged(null, EventArgs.Empty);
        }

        private RadioButton MakeColorRadio(string text, System.Drawing.Color color, int left, int top)
        {
            return new RadioButton
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 185,
                ForeColor = color == System.Drawing.Color.Gold ? System.Drawing.Color.DarkGoldenrod : color
            };
        }
    }
}