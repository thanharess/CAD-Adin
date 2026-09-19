using System;
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
    public class DimScaleStyleNewa
    {
        [CommandMethod("DIMSCALESYLENEW")]
        public void DimScaleStyleNew()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var db = doc.Database;

            // ═══════════════════════════════════════════════════════
            //  BƯỚC 1: Chọn DIMENSION để lấy Dimstyle gốc
            // ═══════════════════════════════════════════════════════
            var peo = new PromptEntityOptions(
                "\nChọn một DIMENSION để lấy Dimstyle gốc: ");
            peo.SetRejectMessage("\nChỉ được chọn DIMENSION!");
            peo.AddAllowedClass(typeof(Dimension), false);

            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                Utils.Print("⏹️ Hủy lệnh.");
                return;
            }

            ObjectId selectedDimId = per.ObjectId;
            string oldStyleName;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var dim = (Dimension)tr.GetObject(selectedDimId, OpenMode.ForRead);
                string fullStyleName = dim.DimensionStyleName;
                oldStyleName = fullStyleName;

                int dollarIndex = fullStyleName.IndexOf('$');
                if (dollarIndex > 0)
                {
                    string shortName = fullStyleName.Substring(0, dollarIndex);
                    var dimTable = (DimStyleTable)tr.GetObject(
                        db.DimStyleTableId, OpenMode.ForRead);
                    if (dimTable.Has(shortName))
                        oldStyleName = shortName;
                }

                tr.Commit();
            }

            Utils.Print("Dimstyle gốc: " + oldStyleName);

            // ═══════════════════════════════════════════════════════
            //  BƯỚC 2: Form
            // ═══════════════════════════════════════════════════════
            double userScale;
            bool isScaleTo;
            bool applyToSelectedDim;

            using (var form = new DimScaleForma(oldStyleName))
            {
                if (AcApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    Utils.Print("⏹️ Hủy lệnh.");
                    return;
                }
                userScale = form.ScaleValue;
                isScaleTo = form.IsScaleTo;
                applyToSelectedDim = form.ApplyToSelectedDim;   // ← MỚI
            }

            // ═══════════════════════════════════════════════════════
            //  BƯỚC 3: Hệ số hiệu dụng
            // ═══════════════════════════════════════════════════════
            double effectiveScale = isScaleTo ? userScale : (1.0 / userScale);

            // ═══════════════════════════════════════════════════════
            //  BƯỚC 4: Tên style mới
            // ═══════════════════════════════════════════════════════
            string newStyleName = isScaleTo
                ? oldStyleName + " Scale " + userScale.ToString("F2", CultureInfo.InvariantCulture)
                : oldStyleName + " Scale 1 chia " + userScale.ToString("F2", CultureInfo.InvariantCulture);

            if (newStyleName.Length > 31)
                newStyleName = newStyleName.Substring(0, 31);

            Utils.Print("Style mới: " + newStyleName);
            Utils.Print("Hệ số scale (DIMLFAC): " + effectiveScale.ToString("F4", CultureInfo.InvariantCulture));
            Utils.Print("Áp dụng cho dim đã chọn: " + (applyToSelectedDim ? "CÓ" : "KHÔNG"));

            // ═══════════════════════════════════════════════════════
            //  BƯỚC 5: Tạo style mới
            // ═══════════════════════════════════════════════════════
            try
            {
                ObjectId newStyleId = ObjectId.Null;

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var dimStyles = (DimStyleTable)tr.GetObject(
                        db.DimStyleTableId, OpenMode.ForRead);

                    if (!dimStyles.Has(oldStyleName))
                    {
                        MessageBox.Show("Không tìm thấy Dimstyle gốc:\n\n" + oldStyleName,
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // ── Ghi đè nếu đã tồn tại ──
                    if (dimStyles.Has(newStyleName))
                    {
                        var ask = MessageBox.Show(
                            "Dimstyle đã tồn tại:\n\n" + newStyleName +
                            "\n\nBạn có muốn ghi đè không?",
                            "Dimstyle đã tồn tại",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (ask != DialogResult.Yes)
                        {
                            Utils.Print("⏹️ Hủy lệnh.");
                            return;
                        }

                        dimStyles.UpgradeOpen();
                        ObjectId existedId = dimStyles[newStyleName];
                        var existed = (DimStyleTableRecord)tr.GetObject(
                            existedId, OpenMode.ForWrite);
                        existed.Erase();
                    }

                    // ── (1) Tạo record rỗng + Add vào table ──
                    dimStyles.UpgradeOpen();
                    var newStyle = new DimStyleTableRecord();
                    newStyleId = dimStyles.Add(newStyle);
                    tr.AddNewlyCreatedDBObject(newStyle, true);

                    // ── (2) CopyFrom style gốc ──
                    var oldStyle = (DimStyleTableRecord)tr.GetObject(
                        dimStyles[oldStyleName], OpenMode.ForRead);
                    newStyle.CopyFrom(oldStyle);

                    // ── (3) Gán tên SAU CopyFrom ──
                    newStyle.Name = newStyleName;

                    // ── (4) Ghi DIMLFAC ──
                    newStyle.Dimlfac = effectiveScale;

                    // ═══════════════════════════════════════════════
                    //  (5) ÁP DỤNG CHO DIM ĐÃ CHỌN — CHỈ KHI USER CHỌN
                    // ═══════════════════════════════════════════════
                    if (applyToSelectedDim)
                    {
                        var dim = (Dimension)tr.GetObject(selectedDimId, OpenMode.ForWrite);
                        dim.DimensionStyle = newStyleId;
                        dim.SetDimstyleData(newStyle);   // xóa override cũ
                    }

                    // Set current (backup 1)
                    db.Dimstyle = newStyleId;

                    tr.Commit();
                }

                // ── (6) SET CURRENT SAU COMMIT ──
                try
                {
                    AcApp.SetSystemVariable("DIMSTYLE", newStyleName);
                }
                catch
                {
                    db.Dimstyle = newStyleId;
                }

                ed.Regen();

                string modeText = isScaleTo
                    ? "Scale DIM to " + userScale.ToString("F2", CultureInfo.InvariantCulture)
                    : "Scale DIM nhỏ 1/" + userScale.ToString("F2", CultureInfo.InvariantCulture);

                string applyText = applyToSelectedDim
                    ? "✔ Đã gán cho Dimension đã chọn (xóa override cũ)."
                    : "○ Không gán cho Dimension đã chọn (giữ style cũ).";

                MessageBox.Show(
                    "Đã tạo Dimstyle mới và áp dụng Scale!\n\n" +
                    "Dimstyle gốc: " + oldStyleName + "\n" +
                    "Dimstyle mới: " + newStyleName + "\n\n" +
                    "Chế độ: " + modeText + "\n" +
                    "Measurement Scale Factor (DIMLFAC): " +
                        effectiveScale.ToString("F4", CultureInfo.InvariantCulture) + "\n\n" +
                    "✔ Style mới đã được đặt làm CURRENT.\n" +
                    applyText + "\n\n" +
                    "Mở DIMSTYLE để kiểm tra.",
                    "DIMSCALESYLENEW",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi khi tạo Dimstyle:\n\n" + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Utils.Print("❌ DIMSCALESYLENEW: " + ex.Message);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM
    // ═══════════════════════════════════════════════════════════
    public class DimScaleForma : Form
    {
        private RadioButton rbScaleTo;
        private RadioButton rbScaleSmall;
        private TextBox txtScale;
        private Label lblPreviewValue;
        private CheckBox chkApplyToDim;                 // ← MỚI
        private Button btnOK;
        private Button btnCancel;
        private readonly string _oldStyle;

        public double ScaleValue { get; private set; }
        public bool IsScaleTo => rbScaleTo.Checked;
        public bool ApplyToSelectedDim => chkApplyToDim.Checked;   // ← MỚI

        public DimScaleForma(string oldStyle)
        {
            _oldStyle = oldStyle ?? "";

            this.Text = "Tạo Dimstyle mới theo Scale";
            this.ClientSize = new Size(460, 360);       // tăng chiều cao
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // ── Dimstyle gốc ──
            Label lblOldStyle = new Label
            {
                Text = "Dimstyle gốc:",
                Left = 20,
                Top = 20,
                Width = 110
            };
            Label lblOldStyleValue = new Label
            {
                Text = _oldStyle,
                Left = 135,
                Top = 20,
                Width = 300,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            // ── Chế độ ──
            Label lblMode = new Label
            {
                Text = "Chế độ:",
                Left = 20,
                Top = 60,
                Width = 110,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            Panel pnlMode = new Panel
            {
                Left = 20,
                Top = 85,
                Width = 415,
                Height = 65
            };

            rbScaleTo = new RadioButton
            {
                Text = "1. Scale DIM to N lần (phóng to)",
                Left = 5,
                Top = 5,
                Width = 400,
                Checked = true
            };
            rbScaleSmall = new RadioButton
            {
                Text = "2. Scale DIM nhỏ = 1/N (thu nhỏ)",
                Left = 5,
                Top = 32,
                Width = 400
            };
            pnlMode.Controls.Add(rbScaleTo);
            pnlMode.Controls.Add(rbScaleSmall);

            // ── Hệ số N ──
            Label lblScale = new Label
            {
                Text = "Hệ số N (>0):",
                Left = 20,
                Top = 170,
                Width = 120
            };
            txtScale = new TextBox
            {
                Left = 145,
                Top = 167,
                Width = 290,
                Text = "2"
            };
            txtScale.TextChanged += delegate { UpdatePreview(); };

            // ── Preview ──
            Label lblPreview = new Label
            {
                Text = "Style mới:",
                Left = 20,
                Top = 215,
                Width = 120,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };
            lblPreviewValue = new Label
            {
                Left = 145,
                Top = 215,
                Width = 290,
                Height = 35,
                ForeColor = Color.DarkGreen,
                Font = new System.Drawing.Font(this.Font, FontStyle.Bold)
            };

            // ── CheckBox áp dụng cho dim đã chọn ──
            chkApplyToDim = new CheckBox
            {
                Text = "Áp dụng luôn cho Dimension đã chọn",
                Left = 20,
                Top = 258,
                Width = 415,
                Height = 22,
                Checked = false                             
            };

            // ── Buttons ──
            btnOK = new Button
            {
                Text = "OK",
                Left = 255,
                Top = 300,
                Width = 85,
                Height = 30
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 350,
                Top = 300,
                Width = 85,
                Height = 30
            };

            btnOK.Click += delegate
            {
                double value;
                if (!TryParseScale(txtScale.Text, out value) || value <= 0)
                {
                    MessageBox.Show(
                        "Hệ số N phải là số lớn hơn 0.",
                        "Lỗi nhập liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    txtScale.Focus();
                    txtScale.SelectAll();
                    return;
                }
                ScaleValue = value;
                this.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
            };

            rbScaleTo.CheckedChanged += delegate { UpdatePreview(); };
            rbScaleSmall.CheckedChanged += delegate { UpdatePreview(); };

            this.Controls.AddRange(new Control[]
            {
                lblOldStyle, lblOldStyleValue,
                lblMode, pnlMode,
                lblScale, txtScale,
                lblPreview, lblPreviewValue,
                chkApplyToDim,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            this.Shown += delegate
            {
                txtScale.Focus();
                txtScale.SelectAll();
            };

            UpdatePreview();
        }

        private static bool TryParseScale(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out value);
        }

        private void UpdatePreview()
        {
            double value;
            if (!TryParseScale(txtScale.Text, out value) || value <= 0)
            {
                lblPreviewValue.Text = "(Nhập hệ số N > 0)";
                lblPreviewValue.ForeColor = Color.Gray;
                return;
            }

            string name = rbScaleTo.Checked
                ? _oldStyle + " Scale " + value.ToString("F2", CultureInfo.InvariantCulture)
                : _oldStyle + " Scale 1 chia " + value.ToString("F2", CultureInfo.InvariantCulture);

            if (name.Length > 31)
                name = name.Substring(0, 31);

            lblPreviewValue.Text = name;
            lblPreviewValue.ForeColor = Color.DarkGreen;
        }
    }
}