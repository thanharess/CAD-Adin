using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Dim
{
    public class ScaleDimValueBlock
    {
        [CommandMethod("SCALEDIMVALUEBLOCK")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("Lệnh SCALEDIMVALUEBLOCK - Scale Dim Linear factor.");

            // ═══════════════════════════════════════════════════════
            //  Bước 1: Chọn đối tượng
            // ═══════════════════════════════════════════════════════
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "DIMENSION,INSERT")
            });

            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có đối tượng nào được chọn.");
                return;
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 2: Form
            // ═══════════════════════════════════════════════════════
            string mode;
            double factor;

            using (var form = new ScaleDimForm())
            {
                if (AcApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    Utils.Print("⏹️ Hủy lệnh.");
                    return;
                }
                mode = form.Mode;
                factor = form.Factor;
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 3: Thu thập danh sách dim cần sửa
            // ═══════════════════════════════════════════════════════
            var directDimIds = new List<ObjectId>();
            var blockNamesToFix = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;

                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    if (ent is Dimension)
                    {
                        directDimIds.Add(so.ObjectId);
                    }
                    else if (ent is BlockReference br)
                    {
                        if (!string.IsNullOrEmpty(br.Name))
                            blockNamesToFix.Add(br.Name);
                    }
                }
                tr.Commit();
            }

            int count = 0;

            // ═══════════════════════════════════════════════════════
            //  Bước 4: Sửa DIM trực tiếp
            // ═══════════════════════════════════════════════════════
            if (directDimIds.Count > 0)
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var id in directDimIds)
                    {
                        var dim = tr.GetObject(id, OpenMode.ForWrite) as Dimension;
                        if (dim == null) continue;

                        ApplyFactor(dim, mode, factor);
                        count++;
                    }
                    tr.Commit();
                }
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 5: Sửa DIM bên trong block definition
            // ═══════════════════════════════════════════════════════
            foreach (var blkName in blockNamesToFix)
            {
                ObjectId btrId = ObjectId.Null;

                // TRANSACTION 1: Sửa dim trong block
                using (var tr1 = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr1.GetObject(db.BlockTableId, OpenMode.ForRead);
                    if (!bt.Has(blkName)) continue;

                    btrId = bt[blkName];
                    var btr = (BlockTableRecord)tr1.GetObject(btrId, OpenMode.ForWrite);

                    if (btr.IsLayout) continue;

                    bool modified = false;

                    foreach (ObjectId id in btr)
                    {
                        var sub = tr1.GetObject(id, OpenMode.ForWrite) as Entity;
                        if (sub is Dimension subDim)
                        {
                            ApplyFactor(subDim, mode, factor);
                            modified = true;
                            count++;
                        }
                    }

                    if (modified)
                    {
                        if (btr.IsDynamicBlock)
                        {
                            try { btr.UpdateAnonymousBlocks(); } catch { }
                        }
                        tr1.Commit();
                    }
                    else
                    {
                        tr1.Abort();
                    }
                }

                if (btrId.IsNull) continue;

                // TRANSACTION 2: Rename trick
                using (var tr2 = db.TransactionManager.StartTransaction())
                {
                    var btr = tr2.GetObject(btrId, OpenMode.ForWrite) as BlockTableRecord;
                    if (btr != null && !btr.IsLayout)
                    {
                        string originalName = btr.Name;
                        if (!originalName.StartsWith("*"))
                        {
                            string tempName = "~TMP~" + Guid.NewGuid().ToString("N").Substring(0, 8);
                            try
                            {
                                btr.Name = tempName;
                                btr.Name = originalName;
                            }
                            catch { }
                        }
                    }
                    tr2.Commit();
                }

                // TRANSACTION 3: Update block references
                using (var tr3 = db.TransactionManager.StartTransaction())
                {
                    var btr = tr3.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                    if (btr != null)
                    {
                        foreach (ObjectId brId in btr.GetBlockReferenceIds(true, true))
                        {
                            var brRef = tr3.GetObject(brId, OpenMode.ForWrite, false, true) as BlockReference;
                            if (brRef != null)
                            {
                                brRef.RecordGraphicsModified(true);
                            }
                        }
                    }
                    tr3.Commit();
                }
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 6: Force regen
            // ═══════════════════════════════════════════════════════
            ed.Regen();
            doc.SendStringToExecute("_.REGENALL ", true, false, false);

            // ═══════════════════════════════════════════════════════
            //  Thông báo kết quả
            // ═══════════════════════════════════════════════════════
            switch (mode)
            {
                case "3":
                    Utils.Print($"✔ Đã đặt {count} DIM về giá trị 1.0");
                    break;
                case "4":
                    Utils.Print($"✔ Đã chuyển {count} DIM sang hệ Inch (× 1/25.4)");
                    break;
                case "5":
                    Utils.Print($"✔ Đã chuyển {count} DIM từ Inch sang mm (× 25.4)");
                    break;
                default:
                    Utils.Print($"✔ Đã scale {count} DIM, hệ số nhân: {factor.ToString("F6", CultureInfo.InvariantCulture)}");
                    break;
            }
        }

        // ============================================================
        //  Hàm áp dụng hệ số
        // ============================================================
        private static void ApplyFactor(Dimension dim, string mode, double factor)
        {
            double old = dim.Dimlfac;
            if (old == 0) old = 1.0;

            switch (mode)
            {
                case "3":   // Reset về 1.0
                    dim.Dimlfac = 1.0;
                    break;

                case "4":   // Nhân thành hệ Inch (mm → Inch)
                    dim.Dimlfac = old * (1.0 / 25.4);
                    break;

                case "5":   // Chia hệ Inch thành mm (Inch → mm)
                    dim.Dimlfac = old * 25.4;
                    break;

                default:    // 1 = Tăng, 2 = Giảm
                    dim.Dimlfac = old * factor;
                    break;
            }

            try
            {
                dim.RecomputeDimensionBlock(true);
            }
            catch { }

            dim.RecordGraphicsModified(true);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM (đã thêm Mode 4 và 5)
    // ═══════════════════════════════════════════════════════════
    public class ScaleDimForm : Form
    {
        private RadioButton rbIncrease;
        private RadioButton rbDecrease;
        private RadioButton rbReset;
        private RadioButton rbToInch;
        private RadioButton rbToMm;
        private TextBox txtFactor;
        private Label lblHint;
        private Label lblPreviewValue;
        private Button btnOK;
        private Button btnCancel;

        public string Mode { get; private set; } = "1";
        public double Factor { get; private set; } = 1.0;

        public ScaleDimForm()
        {
            this.Text = "Scale Dim Value";
            this.ClientSize = new Size(440, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            Label lblTitle = new Label
            {
                Text = "Chọn chế độ:",
                Left = 20,
                Top = 12,
                Width = 400,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            Panel pnlMode = new Panel
            {
                Left = 20,
                Top = 38,
                Width = 400,
                Height = 150
            };

            rbIncrease = new RadioButton
            {
                Text = "1. Tăng   (nhân hệ số N)",
                Left = 5,
                Top = 3,
                Width = 390,
                Checked = true
            };

            rbDecrease = new RadioButton
            {
                Text = "2. Giảm   (chia hệ số N)",
                Left = 5,
                Top = 28,
                Width = 390
            };

            rbReset = new RadioButton
            {
                Text = "3. Reset về 1:1   (Dimlfac = 1.0)",
                Left = 5,
                Top = 53,
                Width = 390
            };

            rbToInch = new RadioButton
            {
                Text = "4. Từ hệ mm thành Inch  (mm → Inch)   × 1/25.4",
                Left = 5,
                Top = 78,
                Width = 390
            };

            rbToMm = new RadioButton
            {
                Text = "5. Từ hệ Inch thành mm   (Inch → mm)   × 25.4",
                Left = 5,
                Top = 103,
                Width = 390
            };

            pnlMode.Controls.Add(rbIncrease);
            pnlMode.Controls.Add(rbDecrease);
            pnlMode.Controls.Add(rbReset);
            pnlMode.Controls.Add(rbToInch);
            pnlMode.Controls.Add(rbToMm);

            Label lblFactor = new Label
            {
                Text = "Hệ số N (>0):",
                Left = 20,
                Top = 200,
                Width = 110
            };

            txtFactor = new TextBox
            {
                Left = 135,
                Top = 197,
                Width = 260,
                Text = "2"
            };
            txtFactor.TextChanged += delegate { UpdatePreview(); };

            lblHint = new Label
            {
                Text = "(VD: N=2 → nhân 2 lần)",
                Left = 135,
                Top = 222,
                Width = 260,
                ForeColor = Color.Gray
            };

            Label lblPreview = new Label
            {
                Text = "Hệ số áp dụng:",
                Left = 20,
                Top = 255,
                Width = 110,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            lblPreviewValue = new Label
            {
                Left = 135,
                Top = 255,
                Width = 280,
                ForeColor = Color.DarkGreen,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            btnOK = new Button
            {
                Text = "OK",
                Left = 240,
                Top = 300,
                Width = 80,
                Height = 30
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 330,
                Top = 300,
                Width = 80,
                Height = 30
            };

            btnOK.Click += delegate
            {
                if (rbReset.Checked)
                {
                    Mode = "3";
                    Factor = 1.0;
                    this.DialogResult = DialogResult.OK;
                    return;
                }

                if (rbToInch.Checked)
                {
                    Mode = "4";
                    Factor = 1.0 / 25.4;
                    this.DialogResult = DialogResult.OK;
                    return;
                }

                if (rbToMm.Checked)
                {
                    Mode = "5";
                    Factor = 25.4;
                    this.DialogResult = DialogResult.OK;
                    return;
                }

                // Mode 1 hoặc 2
                double value;
                if (!TryParsePositive(txtFactor.Text, out value))
                {
                    MessageBox.Show(
                        "Hệ số N phải là số lớn hơn 0.",
                        "Lỗi nhập liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    txtFactor.Focus();
                    txtFactor.SelectAll();
                    return;
                }

                if (rbIncrease.Checked)
                {
                    Mode = "1";
                    Factor = value;
                }
                else
                {
                    Mode = "2";
                    Factor = 1.0 / value;
                }

                this.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
            };

            // Sự kiện thay đổi radio
            rbIncrease.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };
            rbDecrease.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };
            rbReset.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };
            rbToInch.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };
            rbToMm.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, pnlMode,
                lblFactor, txtFactor, lblHint,
                lblPreview, lblPreviewValue,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            this.Shown += delegate
            {
                txtFactor.Focus();
                txtFactor.SelectAll();
            };

            UpdatePreview();
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            // Chỉ cho nhập hệ số khi chọn 1 hoặc 2
            bool enableFactor = rbIncrease.Checked || rbDecrease.Checked;
            txtFactor.Enabled = enableFactor;
            lblHint.Enabled = enableFactor;
        }

        private static bool TryParsePositive(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Trim().Replace(',', '.');

            if (!double.TryParse(text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out value))
                return false;

            return value > 0;
        }

        private void UpdatePreview()
        {
            if (rbReset.Checked)
            {
                lblPreviewValue.Text = "1.0000 (trở về gốc)";
                lblPreviewValue.ForeColor = Color.DarkRed;
                return;
            }

            if (rbToInch.Checked)
            {
                lblPreviewValue.Text = "× 0.039370 (mm → Inch)";
                lblPreviewValue.ForeColor = Color.DarkBlue;
                return;
            }

            if (rbToMm.Checked)
            {
                lblPreviewValue.Text = "× 25.4000 (Inch → mm)";
                lblPreviewValue.ForeColor = Color.DarkBlue;
                return;
            }

            double v;
            if (!TryParsePositive(txtFactor.Text, out v))
            {
                lblPreviewValue.Text = "(Nhập hệ số > 0)";
                lblPreviewValue.ForeColor = Color.Gray;
                return;
            }

            double effective = rbIncrease.Checked ? v : (1.0 / v);
            lblPreviewValue.Text = effective.ToString("F6", CultureInfo.InvariantCulture);
            lblPreviewValue.ForeColor = Color.DarkGreen;
        }
    }
}