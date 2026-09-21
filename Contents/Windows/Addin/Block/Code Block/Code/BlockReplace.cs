using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using CADAddin.Common;

// ALIAS tránh xung đột Font
using WinFont = System.Drawing.Font;

namespace CADAddin.Block
{
    public class BlockReplace
    {
        // Nhớ cấu hình
        private static string _lastTargetBlock = null;
        private static bool _lastDeleteSample = false;
        private static bool _lastPreserveAttr = true;
        private static bool _lastAllSameName = true;
        private static bool _lastChangeLayer = false;
        private static string _lastLayerName = null;
        private static bool _lastKeepTargetLayer = false;

        [CommandMethod("BLREPLACE")]
        public void Replace()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("Lệnh BLREPLACE - Thay thế block.");

            // ═══════════════════════════════════════════════════════
            //  Bước 1: Pick block NGUỒN
            // ═══════════════════════════════════════════════════════
            var peo = new PromptEntityOptions("\nChọn block cần thay thế (block sẽ bị thay thế): ");
            peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peo.AddAllowedClass(typeof(BlockReference), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            ObjectId sampleId = per.ObjectId;
            string sourceBlockName;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(sampleId, OpenMode.ForRead);
                sourceBlockName = br.Name;
                tr.Commit();
            }
            Utils.Print($"🔹 Block cần thay thế: {sourceBlockName}");

            // ═══════════════════════════════════════════════════════
            //  Bước 2: PICK BLOCK ĐÍCH NGAY
            // ═══════════════════════════════════════════════════════
            string pickedTargetName = null;
            string pickedTargetLayer = null;

            var peoPick = new PromptEntityOptions(
                "\nChọn block sẽ thay thế (hoặc Enter/ESC để bỏ qua, chọn từ danh sách sau): ");
            peoPick.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peoPick.AddAllowedClass(typeof(BlockReference), false);
            peoPick.AllowNone = true;

            var perPick = ed.GetEntity(peoPick);

            if (perPick.Status == PromptStatus.OK)
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var br = tr.GetObject(perPick.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (br != null)
                    {
                        pickedTargetName = br.Name;
                        pickedTargetLayer = br.Layer;
                        Utils.Print($"✔ Block sẽ thay thế đã chọn: {pickedTargetName}");
                        Utils.Print($"   • Layer gốc của block đích: {pickedTargetLayer}");
                    }
                    tr.Commit();
                }
            }
            else
            {
                Utils.Print("→ Bỏ qua Block sẽ thay thế — sẽ chọn từ form.");
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 3: Vòng lặp mở form
            // ═══════════════════════════════════════════════════════
            var blockNames = GetAllBlockNames(db);
            if (blockNames.Count == 0)
            {
                Utils.Print("❌ Bản vẽ chưa có block nào.");
                return;
            }

            var layerNames = GetAllLayers(db);

            string targetBlockName = pickedTargetName ?? _lastTargetBlock;
            bool deleteSample = _lastDeleteSample;
            bool preserveAttributes = _lastPreserveAttr;
            bool applyToAllSameName = _lastAllSameName;
            bool changeLayer = _lastChangeLayer;
            string targetLayerName = _lastLayerName;
            bool keepTargetLayer = _lastKeepTargetLayer;

            if (!string.IsNullOrEmpty(pickedTargetName) &&
                !blockNames.Contains(pickedTargetName, StringComparer.OrdinalIgnoreCase))
            {
                blockNames.Add(pickedTargetName);
                blockNames.Sort(StringComparer.OrdinalIgnoreCase);
            }

            while (true)
            {
                bool pickTargetRequested;

                using (var form = new BlockReplaceForm(
                    sourceBlockName, blockNames, layerNames,
                    targetBlockName, deleteSample,
                    preserveAttributes, applyToAllSameName,
                    changeLayer, targetLayerName,
                    keepTargetLayer, !string.IsNullOrEmpty(pickedTargetLayer)))
                {
                    var dr = AcApp.ShowModalDialog(form);

                    if (form.PickTargetRequested)
                    {
                        pickTargetRequested = true;
                        deleteSample = form.DeleteSample;
                        preserveAttributes = form.PreserveAttributes;
                        applyToAllSameName = form.ApplyToAllSameName;
                        changeLayer = form.ChangeLayer;
                        targetLayerName = form.TargetLayer;
                        keepTargetLayer = form.KeepTargetLayer;
                        targetBlockName = form.TargetBlock;
                    }
                    else if (dr != DialogResult.OK)
                    {
                        Utils.Print("⏹️ Hủy lệnh.");
                        return;
                    }
                    else
                    {
                        pickTargetRequested = false;
                    }

                    if (!pickTargetRequested)
                    {
                        targetBlockName = form.TargetBlock;
                        deleteSample = form.DeleteSample;
                        preserveAttributes = form.PreserveAttributes;
                        applyToAllSameName = form.ApplyToAllSameName;
                        changeLayer = form.ChangeLayer;
                        targetLayerName = form.TargetLayer;
                        keepTargetLayer = form.KeepTargetLayer;
                    }
                }

                if (!pickTargetRequested)
                    break;

                // ── Pick block đích từ bản vẽ (nút trên form) ──
                var peo2 = new PromptEntityOptions("\nChọn block sẽ thay thế: ");
                peo2.SetRejectMessage("\nĐối tượng được chọn không phải block.");
                peo2.AddAllowedClass(typeof(BlockReference), false);
                var per2 = ed.GetEntity(peo2);

                if (per2.Status == PromptStatus.OK)
                {
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var br = tr.GetObject(per2.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (br != null)
                        {
                            targetBlockName = br.Name;
                            pickedTargetLayer = br.Layer;
                            Utils.Print($"✔ Block sẽ thay thế đã chọn: {targetBlockName}");
                            Utils.Print($"   • Layer gốc của block đích: {pickedTargetLayer}");

                            if (!blockNames.Contains(targetBlockName,
                                    StringComparer.OrdinalIgnoreCase))
                            {
                                blockNames.Add(targetBlockName);
                                blockNames.Sort(StringComparer.OrdinalIgnoreCase);
                            }
                        }
                        tr.Commit();
                    }
                }
                else
                {
                    Utils.Print("⏹️ Không chọn được block sẽ thay thế.");
                }
            }

            // ═══════════════════════════════════════════════════════
            //  Validate
            // ═══════════════════════════════════════════════════════
            if (string.IsNullOrEmpty(targetBlockName))
            {
                Utils.Print("❌ Chưa chọn block sẽ thay thế.");
                return;
            }
            if (string.Equals(targetBlockName, sourceBlockName,
                              StringComparison.OrdinalIgnoreCase))
            {
                Utils.Print("❌ Block sẽ thay trùng với block cần thay thế.");
                return;
            }

            _lastTargetBlock = targetBlockName;
            _lastDeleteSample = deleteSample;
            _lastPreserveAttr = preserveAttributes;
            _lastAllSameName = applyToAllSameName;
            _lastChangeLayer = changeLayer;
            _lastLayerName = targetLayerName;
            _lastKeepTargetLayer = keepTargetLayer;

            Utils.Print($"🔹 Block sẽ thay thế: {targetBlockName}");
            if (changeLayer)
                Utils.Print($"🔹 Chế độ: Đổi sang layer '{targetLayerName}'");
            else if (keepTargetLayer && !string.IsNullOrEmpty(pickedTargetLayer))
                Utils.Print($"🔹 Chế độ: Giữ nguyên layer gốc của block đích '{pickedTargetLayer}'");
            else
                Utils.Print($"🔹 Chế độ: Giữ layer của block nguồn (mặc định)");

            // ═══════════════════════════════════════════════════════
            //  Bước 4: Thu thập danh sách block cần thay
            // ═══════════════════════════════════════════════════════
            var idsToReplace = new List<ObjectId>();

            if (applyToAllSameName)
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    foreach (ObjectId btrId in bt)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                        foreach (ObjectId refId in btr.GetBlockReferenceIds(true, true))
                        {
                            var br = tr.GetObject(refId, OpenMode.ForRead) as BlockReference;
                            if (br != null && string.Equals(br.Name, sourceBlockName,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                idsToReplace.Add(refId);
                            }
                        }
                    }
                    tr.Commit();
                }

                Utils.Print($"→ Áp dụng cho TẤT CẢ {idsToReplace.Count} block cùng tên.");
            }
            else
            {
                idsToReplace.Add(sampleId);
                Utils.Print($"→ Chỉ thay đổi cho 1 block đã chọn.");
            }

            if (idsToReplace.Count == 0)
            {
                Utils.Print($"❌ Không tìm thấy block nào tên '{sourceBlockName}'.");
                return;
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 5: Thực hiện thay thế
            // ═══════════════════════════════════════════════════════
            int success = 0;
            int failed = 0;
            var failedReasons = new List<string>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                if (!bt.Has(targetBlockName))
                {
                    Utils.Print($"❌ Block sẽ thay thế:  '{targetBlockName}' không tồn tại.");
                    return;
                }
                ObjectId targetDefId = bt[targetBlockName];

                foreach (var id in idsToReplace)
                {
                    try
                    {
                        if (id.IsErased || id.IsNull) continue;

                        var oldRef = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                        if (oldRef == null) continue;

                        var position = oldRef.Position;
                        var rotation = oldRef.Rotation;
                        var scale = oldRef.ScaleFactors;
                        string sourceLayer = oldRef.Layer;
                        int colorIdx = oldRef.ColorIndex;
                        string linetype = oldRef.Linetype;
                        double ltScale = oldRef.LinetypeScale;
                        var lineWeight = oldRef.LineWeight;
                        bool visible = oldRef.Visible;

                        var attrValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        if (preserveAttributes)
                        {
                            foreach (ObjectId attId in oldRef.AttributeCollection)
                            {
                                var att = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                                if (att != null)
                                    attrValues[att.Tag] = att.TextString;
                            }
                        }

                        var ownerBtr = (BlockTableRecord)tr.GetObject(oldRef.OwnerId, OpenMode.ForWrite);

                        // ═══════════════════════════════════════════════
                        //  XÁC ĐỊNH LAYER CHO BLOCK MỚI
                        //    1. changeLayer → layer chỉ định
                        //    2. keepTargetLayer + có layer pick → layer gốc block đích
                        //    3. Mặc định → layer của block nguồn
                        // ═══════════════════════════════════════════════
                        string newLayer;
                        if (changeLayer && !string.IsNullOrEmpty(targetLayerName))
                            newLayer = targetLayerName;
                        else if (keepTargetLayer && !string.IsNullOrEmpty(pickedTargetLayer))
                            newLayer = pickedTargetLayer;
                        else
                            newLayer = sourceLayer;

                        var newRef = new BlockReference(position, targetDefId)
                        {
                            Rotation = rotation,
                            ScaleFactors = scale,
                            Layer = newLayer,
                            ColorIndex = (short)colorIdx,
                            Linetype = linetype,
                            LinetypeScale = ltScale,
                            LineWeight = lineWeight,
                            Visible = visible
                        };

                        ownerBtr.AppendEntity(newRef);
                        tr.AddNewlyCreatedDBObject(newRef, true);

                        if (preserveAttributes && attrValues.Count > 0)
                        {
                            var targetBtr = (BlockTableRecord)tr.GetObject(
                                targetDefId, OpenMode.ForRead);

                            if (targetBtr.HasAttributeDefinitions)
                            {
                                foreach (ObjectId defId in targetBtr)
                                {
                                    var attDef = tr.GetObject(defId, OpenMode.ForRead) as AttributeDefinition;
                                    if (attDef == null || attDef.Constant) continue;
                                    if (!attrValues.ContainsKey(attDef.Tag)) continue;

                                    var newAtt = new AttributeReference();
                                    newAtt.SetAttributeFromBlock(attDef, newRef.BlockTransform);
                                    newAtt.TextString = attrValues[attDef.Tag];
                                    newAtt.Position = attDef.Position.TransformBy(newRef.BlockTransform);

                                    newRef.AttributeCollection.AppendAttribute(newAtt);
                                    tr.AddNewlyCreatedDBObject(newAtt, true);
                                }
                            }
                        }

                        oldRef.Erase();
                        success++;
                    }
                    catch (System.Exception ex)
                    {
                        failed++;
                        failedReasons.Add(ex.Message);
                    }
                }

                if (deleteSample && !sampleId.IsNull && !sampleId.IsErased)
                {
                    var sref = tr.GetObject(sampleId, OpenMode.ForWrite) as BlockReference;
                    if (sref != null) sref.Erase();
                    Utils.Print("  (đã xóa block sẽ thay thế)");
                }

                tr.Commit();
            }

            ed.Regen();

            Utils.Print($"✅ Đã thay thế {success} block '{sourceBlockName}' → '{targetBlockName}'.");
            if (failed > 0)
            {
                Utils.Print($"⚠ Không thể thay {failed} block:");
                foreach (var r in failedReasons.Take(5))
                    Utils.Print($"   • {r}");
            }
        }

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
                    if (btr.GetBlockReferenceIds(true, false).Count == 0) continue;
                    list.Add(btr.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

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
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM
    // ═══════════════════════════════════════════════════════════
    public class BlockReplaceForm : Form
    {
        private Label lblSourceName;
        private TextBox txtFilter;
        private ComboBox cboTarget;
        private Button btnPickTarget;
        private CheckBox chkDeleteSample;
        private CheckBox chkPreserveAttr;
        private CheckBox chkAllSameName;

        private CheckBox chkKeepTargetLayer;             // ← MỚI
        private CheckBox chkChangeLayer;
        private Label lblLayerFilter;
        private TextBox txtLayerFilter;
        private ComboBox cboLayer;

        private Button btnOK;
        private Button btnCancel;

        private readonly List<string> _allBlocks;
        private readonly List<string> _allLayers;
        private readonly bool _canKeepTargetLayer;

        public string TargetBlock => cboTarget.Text?.Trim() ?? "";
        public bool DeleteSample => chkDeleteSample.Checked;
        public bool PreserveAttributes => chkPreserveAttr.Checked;
        public bool ApplyToAllSameName => chkAllSameName.Checked;
        public bool KeepTargetLayer => chkKeepTargetLayer.Checked;
        public bool ChangeLayer => chkChangeLayer.Checked;
        public string TargetLayer => cboLayer.Text?.Trim() ?? "";
        public bool PickTargetRequested { get; private set; } = false;

        public BlockReplaceForm(string sourceName,
            List<string> blocks, List<string> layers,
            string preTarget, bool preDeleteSample,
            bool prePreserveAttr, bool preAllSameName,
            bool preChangeLayer, string preLayer,
            bool preKeepTargetLayer, bool canKeepTargetLayer)
        {
            _allBlocks = blocks ?? new List<string>();
            _allLayers = layers ?? new List<string>();
            _canKeepTargetLayer = canKeepTargetLayer;

            this.Text = "Thay thế Block";
            this.ClientSize = new Size(490, 455);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // ── Block nguồn ──
            Label lblSource = new Label
            {
                Text = "Block cần thay thế:",
                Left = 20,
                Top = 18,
                Width = 100
            };
            lblSourceName = new Label
            {
                Text = sourceName,
                Left = 125,
                Top = 18,
                Width = 345,
                Font = new WinFont(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkRed
            };

            Label lblArrow = new Label
            {
                Text = "⬇  Chọn block sẽ thay thế và các lựa chọn khác:",
                Left = 20,
                Top = 50,
                Width = 450,
                Font = new WinFont(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };

            // ── Ô lọc block ──
            Label lblFilter = new Label
            {
                Text = "Lọc:",
                Left = 20,
                Top = 82,
                Width = 40
            };
            txtFilter = new TextBox
            {
                Left = 65,
                Top = 79,
                Width = 405
            };
            txtFilter.TextChanged += delegate { ApplyFilter(); };

            // ── ComboBox block đích ──
            Label lblTarget = new Label
            {
                Text = "Block sẽ thay thế:",
                Left = 20,
                Top = 115,
                Width = 90
            };
            cboTarget = new ComboBox
            {
                Left = 115,
                Top = 112,
                Width = 255,
                DropDownStyle = ComboBoxStyle.DropDown
            };

            ReloadCombo(cboTarget, _allBlocks);

            if (!string.IsNullOrEmpty(preTarget))
            {
                int idx = cboTarget.Items.IndexOf(preTarget);
                if (idx >= 0) cboTarget.SelectedIndex = idx;
                else cboTarget.Text = preTarget;
            }
            else if (cboTarget.Items.Count > 0)
                cboTarget.SelectedIndex = 0;

            // ── Nút Pick ──
            btnPickTarget = new Button
            {
                Text = "Chọn từ bản vẽ",
                Left = 375,
                Top = 110,
                Width = 95,
                Height = 28,
                BackColor = Color.FromArgb(220, 240, 220),
                FlatStyle = FlatStyle.System
            };
            btnPickTarget.Click += delegate
            {
                PickTargetRequested = true;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            // ── CheckBox Xóa mẫu ──
            chkDeleteSample = new CheckBox
            {
                Text = "Xóa block sẽ thay thế",
                Left = 20,
                Top = 155,
                Width = 450,
                Height = 22,
                Checked = preDeleteSample,
                Font = new WinFont(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkRed
            };

            // ── CheckBox giữ attribute ──
            chkPreserveAttr = new CheckBox
            {
                Text = "Giữ giá trị ATTRIBUTE từ block cũ (nếu tên tag khớp)",
                Left = 20,
                Top = 182,
                Width = 450,
                Height = 22,
                Checked = prePreserveAttr,
                Font = new WinFont(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            // ── CheckBox áp dụng cho tất cả ──
            chkAllSameName = new CheckBox
            {
                Text = "Áp dụng cho TẤT CẢ block cùng tên",
                Left = 20,
                Top = 209,
                Width = 450,
                Height = 22,
                Checked = preAllSameName,
                Font = new WinFont(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };

            // ═══════════════════════════════════════════════════════
            //  CheckBox "Giữ nguyên layer gốc của block đích" — MỚI
            // ═══════════════════════════════════════════════════════
            chkKeepTargetLayer = new CheckBox
            {
                Text = "Giữ nguyên layer gốc của block sẽ thay thế",
                Left = 20,
                Top = 238,
                Width = 450,
                Height = 22,
                Checked = preKeepTargetLayer && canKeepTargetLayer,
                Enabled = canKeepTargetLayer,
                Font = new WinFont(this.Font, FontStyle.Bold),
                ForeColor = canKeepTargetLayer ? Color.DarkOrange : Color.Gray
            };

            // Nếu chưa pick block đích → không có layer để giữ → disable
            if (!canKeepTargetLayer)
            {
                chkKeepTargetLayer.Text += "  (chưa pick block đích)";
            }

            // ── CheckBox đổi layer ──
            chkChangeLayer = new CheckBox
            {
                Text = "Đổi layer cho block sau khi thay thế:",
                Left = 20,
                Top = 265,
                Width = 260,
                Height = 22,
                Checked = preChangeLayer,
                Font = new WinFont(this.Font, FontStyle.Bold),
                ForeColor = Color.Purple
            };

            // Ô lọc layer
            lblLayerFilter = new Label
            {
                Text = "Lọc:",
                Left = 290,
                Top = 265,
                Width = 35
            };
            txtLayerFilter = new TextBox
            {
                Left = 325,
                Top = 263,
                Width = 145
            };
            txtLayerFilter.TextChanged += delegate { ApplyLayerFilter(); };

            // ComboBox chọn layer
            cboLayer = new ComboBox
            {
                Left = 20,
                Top = 290,
                Width = 450,
                DropDownStyle = ComboBoxStyle.DropDown
            };

            ReloadCombo(cboLayer, _allLayers);

            if (!string.IsNullOrEmpty(preLayer))
            {
                int idx = cboLayer.Items.IndexOf(preLayer);
                if (idx >= 0) cboLayer.SelectedIndex = idx;
                else cboLayer.Text = preLayer;
            }
            else if (cboLayer.Items.Count > 0)
                cboLayer.SelectedIndex = 0;

            // ═══════════════════════════════════════════════════════
            //  ★ LOGIC LOẠI TRỪ — 2 checkbox không được tick cùng lúc
            // ═══════════════════════════════════════════════════════
            EventHandler onLayerModeChanged = delegate
            {
                bool keepOn = chkKeepTargetLayer.Checked;
                bool changeOn = chkChangeLayer.Checked;

                // ── ComboBox layer + ô lọc chỉ enable khi "Đổi layer" được tick ──
                bool layerControlsEnabled = changeOn;
                cboLayer.Enabled = layerControlsEnabled;
                txtLayerFilter.Enabled = layerControlsEnabled;
                lblLayerFilter.Enabled = layerControlsEnabled;

                // ── Nếu "Đổi layer" đang bật → khóa "Giữ nguyên layer" ──
                chkKeepTargetLayer.Enabled = _canKeepTargetLayer && !changeOn;

                // ── Nếu "Giữ nguyên layer" đang bật → khóa "Đổi layer" + controls ──
                chkChangeLayer.Enabled = !keepOn;
            };

            chkChangeLayer.CheckedChanged += onLayerModeChanged;
            chkKeepTargetLayer.CheckedChanged += onLayerModeChanged;

            // ── Hint ──
            Label lblHint = new Label
            {
                Text = "💡 2 tùy chọn layer loại trừ nhau: tick cái này sẽ khóa cái kia.\n" +
                       "    Không tick ô nào = giữ layer của block nguồn (block bị thay).",
                Left = 20,
                Top = 320,
                Width = 450,
                Height = 40,
                ForeColor = Color.DimGray,
                Font = new WinFont(this.Font, FontStyle.Italic)
            };

            // ── Buttons ──
            btnOK = new Button
            {
                Text = "OK",
                Left = 285,
                Top = 385,
                Width = 85,
                Height = 30
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 380,
                Top = 385,
                Width = 85,
                Height = 30
            };

            btnOK.Click += delegate
            {
                if (string.IsNullOrEmpty(TargetBlock))
                {
                    MessageBox.Show("Vui lòng chọn 1 block sẽ thay thế.",
                        "Thiếu thông tin",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                if (string.Equals(TargetBlock, sourceName, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Block sẽ thay thế trùng với block cần thay.",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                if (chkChangeLayer.Checked && string.IsNullOrEmpty(TargetLayer))
                {
                    MessageBox.Show("Vui lòng chọn 1 Layer đích.",
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

            this.Controls.AddRange(new Control[]
            {
                lblSource, lblSourceName,
                lblArrow,
                lblFilter, txtFilter,
                lblTarget, cboTarget, btnPickTarget,
                chkDeleteSample,
                chkPreserveAttr,
                chkAllSameName,
                chkKeepTargetLayer,
                chkChangeLayer, lblLayerFilter, txtLayerFilter,
                cboLayer,
                lblHint,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            this.Shown += delegate { txtFilter.Focus(); };

            // Chạy 1 lần để set trạng thái ban đầu đúng
            onLayerModeChanged(null, EventArgs.Empty);
        }

        private void ApplyFilter()
        {
            string filter = txtFilter.Text?.Trim() ?? "";
            string current = cboTarget.Text?.Trim();

            List<string> src = string.IsNullOrEmpty(filter)
                ? _allBlocks
                : _allBlocks
                    .Where(x => x.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            ReloadCombo(cboTarget, src);

            if (!string.IsNullOrEmpty(current))
                cboTarget.Text = current;
            else if (cboTarget.Items.Count > 0)
                cboTarget.SelectedIndex = 0;
        }

        private void ApplyLayerFilter()
        {
            string filter = txtLayerFilter.Text?.Trim() ?? "";
            string current = cboLayer.Text?.Trim();

            List<string> src = string.IsNullOrEmpty(filter)
                ? _allLayers
                : _allLayers
                    .Where(x => x.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            ReloadCombo(cboLayer, src);

            if (!string.IsNullOrEmpty(current))
                cboLayer.Text = current;
            else if (cboLayer.Items.Count > 0)
                cboLayer.SelectedIndex = 0;
        }

        private static void ReloadCombo(ComboBox cbo, List<string> items)
        {
            cbo.BeginUpdate();
            cbo.Items.Clear();
            cbo.Items.AddRange(items.ToArray());
            cbo.EndUpdate();
        }
    }
}