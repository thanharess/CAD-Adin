using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Dim
{
    public class ScaleDimValue
    {
        // Hệ số chuyển đổi inch ↔ mm
        private const double MM_PER_INCH = 25.4;

        [CommandMethod("SCALEDIMVALUE")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("Lệnh SCALEDIMVALUE - Scale Dim Linear factor.");

            // ═══════════════════════════════════════════════════════
            //  Bước 1: Chọn DIMENSION lần đầu
            // ═══════════════════════════════════════════════════════
            var allIds = new HashSet<ObjectId>();     // tích lũy ObjectId

            var sel = PromptSelectDims(ed, "Chọn các DIM cần scale:");
            if (sel == null) return;

            foreach (SelectedObject so in sel.Value)
                if (so != null) allIds.Add(so.ObjectId);

            // ═══════════════════════════════════════════════════════
            //  Bước 2: Vòng lặp form — tích lũy selection, chỉ chạy khi OK
            // ═══════════════════════════════════════════════════════
            string mode = "1";
            double factor = 1.0;
            bool resetFirst = true;

            while (true)
            {
                bool applyMoreRequested;

                using (var form = new ScaleDimValueForm(allIds.Count))
                {
                    var dr = AcApp.ShowModalDialog(form);

                    // User bấm "Chọn thêm DIM"
                    if (form.ApplyMoreRequested)
                    {
                        applyMoreRequested = true;
                    }
                    // User bấm Cancel / ESC → hủy
                    else if (dr != DialogResult.OK)
                    {
                        Utils.Print("⏹️ Hủy lệnh — không áp dụng gì.");
                        return;
                    }
                    // User bấm OK → chuẩn bị chạy
                    else
                    {
                        applyMoreRequested = false;
                    }

                    mode = form.Mode;
                    factor = form.Factor;
                    resetFirst = form.ResetFirst;
                }

                if (!applyMoreRequested)
                    break;   // Thoát vòng lặp → chạy lệnh

                // ── Chọn thêm DIM (chưa apply) ──
                var newSel = PromptSelectDims(ed,
                    $"Chọn thêm DIM cần scale (đang có {allIds.Count} DIM):");

                if (newSel != null)
                {
                    int added = 0;
                    foreach (SelectedObject so in newSel.Value)
                    {
                        if (so == null) continue;
                        if (allIds.Add(so.ObjectId)) added++;
                    }
                    Utils.Print($"  ▸ Đã thêm {added} DIM mới (trùng {newSel.Value.Count - added})");
                    Utils.Print($"  ▸ Tổng selection: {allIds.Count} DIM");
                }
                else
                {
                    Utils.Print($"  ▸ Không chọn thêm. Giữ nguyên {allIds.Count} DIM.");
                }

                // Quay lại vòng lặp → mở lại form
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 3: Áp dụng cho TẤT CẢ DIM đã tích lũy
            // ═══════════════════════════════════════════════════════
            if (allIds.Count == 0)
            {
                Utils.Print("⏹️ Không có DIM nào để xử lý.");
                return;
            }

            int count = ApplyToIds(db, allIds, mode, factor, resetFirst);

            ed.Regen();

            // ═══════════════════════════════════════════════════════
            //  Thông báo tổng kết
            // ═══════════════════════════════════════════════════════
            string resetNote = resetFirst && mode != "3"
                ? "  (reset về 1.0 trước)"
                : "";

            string modeText;
            switch (mode)
            {
                case "3": modeText = "reset về 1.0"; break;
                case "4": modeText = $"mm → inch (÷{MM_PER_INCH})"; break;
                case "5": modeText = $"inch → mm (×{MM_PER_INCH})"; break;
                default:
                    modeText = $"scale ×{factor.ToString("F4", CultureInfo.InvariantCulture)}";
                    break;
            }

            Utils.Print($"✔ Hoàn thành — Đã xử lý {count} DIM — {modeText}{resetNote}");
        }

        // ═══════════════════════════════════════════════════════════
        //  Chọn DIM từ CAD
        // ═══════════════════════════════════════════════════════════
        private static PromptSelectionResult PromptSelectDims(Editor ed, string message)
        {
            ed.WriteMessage("\n" + message);

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "DIMENSION") });

            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK || sel.Value.Count == 0)
            {
                Utils.Print("⏹️ Không chọn DIM nào.");
                return null;
            }
            return sel;
        }

        // ═══════════════════════════════════════════════════════════
        //  Áp dụng cho tập ObjectId tích lũy
        // ═══════════════════════════════════════════════════════════
        private static int ApplyToIds(Database db, HashSet<ObjectId> ids,
            string mode, double factor, bool resetFirst)
        {
            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    if (id.IsNull || id.IsErased) continue;
                    var dim = tr.GetObject(id, OpenMode.ForWrite) as Dimension;
                    if (dim == null) continue;

                    ApplyFactor(dim, mode, factor, resetFirst);
                    count++;
                }
                tr.Commit();
            }
            return count;
        }

        // ═══════════════════════════════════════════════════════════
        //  Áp dụng hệ số theo chế độ
        // ═══════════════════════════════════════════════════════════
        private static void ApplyFactor(Dimension dim, string mode, double factor, bool resetFirst)
        {
            double old = dim.Dimlfac;
            if (old == 0) old = 1.0;

            double baseValue = resetFirst ? 1.0 : old;

            switch (mode)
            {
                case "3":
                    dim.Dimlfac = 1.0;
                    break;

                case "4":
                    dim.Dimlfac = baseValue / MM_PER_INCH;
                    break;

                case "5":
                    dim.Dimlfac = baseValue * MM_PER_INCH;
                    break;

                case "2":
                case "1":
                default:
                    dim.Dimlfac = baseValue * factor;
                    break;
            }

            // Force rebuild
            try
            {
                var mi = dim.GetType().GetMethod(
                    "RecomputeDimBlock",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (mi != null)
                    mi.Invoke(dim, new object[] { true });
            }
            catch { }

            dim.RecordGraphicsModified(true);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM
    // ═══════════════════════════════════════════════════════════
    public class ScaleDimValueForm : Form
    {
        private RadioButton rbIncrease;
        private RadioButton rbDecrease;
        private RadioButton rbReset;
        private RadioButton rbMmToInch;
        private RadioButton rbInchToMm;

        private TextBox txtFactor;
        private Label lblHint;
        private CheckBox chkResetFirst;
        private Label lblPreviewValue;
        private Button btnApplyMore;
        private Button btnOK;
        private Button btnCancel;

        public string Mode { get; private set; } = "1";
        public double Factor { get; private set; } = 1.0;
        public bool ResetFirst => chkResetFirst.Checked;
        public bool ApplyMoreRequested { get; private set; } = false;

        public ScaleDimValueForm() : this(0) { }

        public ScaleDimValueForm(int selectedCount)
        {
            this.Text = "Scale Dim Value";
            this.ClientSize = new Size(440, 470);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // ── Label hiển thị số DIM đang chọn ──
            Label lblSelected = new Label
            {
                Text = $"📌 Đang chọn: {selectedCount} DIM",
                Left = 20,
                Top = 15,
                Width = 400,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor = selectedCount > 0 ? Color.DarkGreen : Color.Gray
            };

            // ── Nhóm mode ──
            Panel pnlMode = new Panel
            {
                Left = 20,
                Top = 42,
                Width = 400,
                Height = 175
            };

            rbIncrease = new RadioButton
            {
                Text = "1. Tăng     (nhân hệ số N)",
                Left = 5,
                Top = 3,
                Width = 390,
                Checked = true
            };
            rbDecrease = new RadioButton
            {
                Text = "2. Giảm     (chia hệ số N)",
                Left = 5,
                Top = 30,
                Width = 390
            };
            rbReset = new RadioButton
            {
                Text = "3. Reset về 1:1   (Dimlfac = 1.0)",
                Left = 5,
                Top = 57,
                Width = 390
            };
            rbMmToInch = new RadioButton
            {
                Text = "4. mm → inch   (chia 25.4)",
                Left = 5,
                Top = 84,
                Width = 390
            };
            rbInchToMm = new RadioButton
            {
                Text = "5. inch → mm   (nhân 25.4)",
                Left = 5,
                Top = 111,
                Width = 390
            };

            pnlMode.Controls.Add(rbIncrease);
            pnlMode.Controls.Add(rbDecrease);
            pnlMode.Controls.Add(rbReset);
            pnlMode.Controls.Add(rbMmToInch);
            pnlMode.Controls.Add(rbInchToMm);

            // ── Hệ số ──
            Label lblFactor = new Label
            {
                Text = "Hệ số N (>0):",
                Left = 20,
                Top = 230,
                Width = 110
            };
            txtFactor = new TextBox
            {
                Left = 135,
                Top = 227,
                Width = 285,
                Text = "2"
            };
            txtFactor.TextChanged += delegate { UpdatePreview(); };

            lblHint = new Label
            {
                Text = "(VD: N=2 → nhân 2 lần; N=5 → chia 1/5)",
                Left = 135,
                Top = 252,
                Width = 285,
                ForeColor = Color.Gray
            };

            // ── CheckBox Reset First ──
            chkResetFirst = new CheckBox
            {
                Text = "Reset Dimlfac về 1.0 trước khi scale",
                Left = 20,
                Top = 282,
                Width = 400,
                Checked = true,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkRed
            };
            chkResetFirst.CheckedChanged += delegate { UpdatePreview(); };

            // ── Preview ──
            Label lblPreview = new Label
            {
                Text = "Hệ số áp dụng:",
                Left = 20,
                Top = 318,
                Width = 110,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };
            lblPreviewValue = new Label
            {
                Left = 135,
                Top = 318,
                Width = 285,
                ForeColor = Color.DarkGreen,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            // ═══════════════════════════════════════════════════
            //  Buttons
            // ═══════════════════════════════════════════════════
            btnApplyMore = new Button
            {
                Text = "Chọn thêm DIM",
                Left = 20,
                Top = 405,
                Width = 130,
                Height = 30,
                BackColor = Color.FromArgb(220, 240, 220),
                FlatStyle = FlatStyle.System
            };

            btnOK = new Button
            {
                Text = "OK  (Chạy)",
                Left = 230,
                Top = 405,
                Width = 95,
                Height = 30
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 335,
                Top = 405,
                Width = 85,
                Height = 30
            };

            // ── Nút "Chọn thêm DIM" — chỉ mở rộng selection, KHÔNG chạy ──
            btnApplyMore.Click += delegate
            {
                if (!ValidateAndFill())
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }

                ApplyMoreRequested = true;
                this.DialogResult = DialogResult.Cancel;   // đóng form
                this.Close();
            };

            // ── Nút OK — chạy lệnh với tất cả DIM đã chọn ──
            btnOK.Click += delegate
            {
                if (!ValidateAndFill())
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
                this.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
            };

            rbIncrease.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };
            rbDecrease.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };
            rbReset.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };
            rbMmToInch.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };
            rbInchToMm.CheckedChanged += delegate { UpdatePreview(); UpdateUIState(); };

            this.Controls.AddRange(new Control[]
            {
                lblSelected, pnlMode,
                lblFactor, txtFactor, lblHint,
                chkResetFirst,
                lblPreview, lblPreviewValue,
                btnApplyMore, btnOK, btnCancel
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

        private bool ValidateAndFill()
        {
            if (rbReset.Checked)
            {
                Mode = "3"; Factor = 1.0;
                return true;
            }
            if (rbMmToInch.Checked)
            {
                Mode = "4"; Factor = 1.0 / 25.4;
                return true;
            }
            if (rbInchToMm.Checked)
            {
                Mode = "5"; Factor = 25.4;
                return true;
            }

            double value;
            if (!TryParsePositive(txtFactor.Text, out value))
            {
                MessageBox.Show(
                    "Hệ số N phải là số lớn hơn 0.",
                    "Lỗi nhập liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtFactor.Focus();
                txtFactor.SelectAll();
                return false;
            }

            if (rbIncrease.Checked)
            {
                Mode = "1"; Factor = value;
            }
            else
            {
                Mode = "2"; Factor = 1.0 / value;
            }
            return true;
        }

        private void UpdateUIState()
        {
            bool enableFactor = rbIncrease.Checked || rbDecrease.Checked;
            txtFactor.Enabled = enableFactor;
            lblHint.Enabled = enableFactor;
            chkResetFirst.Enabled = !rbReset.Checked;
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
            string prefix = chkResetFirst.Checked && !rbReset.Checked
                ? "từ 1.0 → "
                : "";

            if (rbReset.Checked)
            {
                lblPreviewValue.Text = "1.0000 (về gốc)";
                lblPreviewValue.ForeColor = Color.DarkRed;
                return;
            }
            if (rbMmToInch.Checked)
            {
                lblPreviewValue.Text = prefix + "0.0394  (÷ 25.4)";
                lblPreviewValue.ForeColor = Color.DarkBlue;
                return;
            }
            if (rbInchToMm.Checked)
            {
                lblPreviewValue.Text = prefix + "25.4000 (× 25.4)";
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
            lblPreviewValue.Text = prefix + effective.ToString("F4", CultureInfo.InvariantCulture);
            lblPreviewValue.ForeColor = Color.DarkGreen;
        }
    }
}