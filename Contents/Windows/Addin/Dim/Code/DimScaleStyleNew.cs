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
    public class DimScaleStyleNew
    {
        [CommandMethod("DIMSCALESYLENEW")]
        public void CreateScaledDimStyle()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;

            if (doc == null)
                return;

            var ed = doc.Editor;
            var db = doc.Database;

            // =====================================================
            // 1. CHỌN DIMENSION
            // =====================================================

            PromptEntityOptions peo =
                new PromptEntityOptions(
                    "\nChọn một DIMENSION để lấy Dimstyle gốc: ");

            peo.SetRejectMessage(
                "\nChỉ được chọn DIMENSION!");

            peo.AddAllowedClass(
                typeof(Dimension),
                false);

            PromptEntityResult per =
                ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
            {
                Utils.Print("⏹️ Hủy lệnh.");
                return;
            }

            string fullStyleName;
            string oldStyleName;

            // =====================================================
            // LẤY DIMSTYLE CỦA DIM ĐƯỢC CHỌN
            // =====================================================

            using (Transaction tr =
                db.TransactionManager.StartTransaction())
            {
                Dimension dim =
                    (Dimension)tr.GetObject(
                        per.ObjectId,
                        OpenMode.ForRead);

                fullStyleName =
                    dim.DimensionStyleName;

                oldStyleName =
                    fullStyleName;

                // Xử lý tên dạng:
                // ABC$0$DEF
                //
                // lấy:
                // ABC

                int dollarIndex =
                    fullStyleName.IndexOf('$');

                if (dollarIndex > 0)
                {
                    string shortName =
                        fullStyleName.Substring(
                            0,
                            dollarIndex);

                    DimStyleTable dimTable =
                        (DimStyleTable)tr.GetObject(
                            db.DimStyleTableId,
                            OpenMode.ForRead);

                    if (dimTable.Has(shortName))
                        oldStyleName = shortName;
                }

                tr.Commit();
            }

            Utils.Print(
                "Dimstyle gốc: " +
                oldStyleName);


            // =====================================================
            // 2. FORM CHỌN CHẾ ĐỘ
            // =====================================================

            double userScale;
            bool isScaleTo;

            using (DimScaleForm form =
                new DimScaleForm(oldStyleName))
            {
                DialogResult result =
                    AcApp.ShowModalDialog(form);

                if (result != DialogResult.OK)
                {
                    Utils.Print("⏹️ Hủy lệnh.");
                    return;
                }

                userScale =
                    form.ScaleValue;

                isScaleTo =
                    form.IsScaleTo;
            }


            // =====================================================
            // 3. TÍNH DIMLFAC
            // =====================================================

            double effectiveScale;

            if (isScaleTo)
                effectiveScale = userScale;
            else
                effectiveScale = 1.0 / userScale;


            // =====================================================
            // 4. TÊN STYLE MỚI
            // =====================================================

            string newStyleName;

            if (isScaleTo)
            {
                newStyleName =
                    oldStyleName +
                    " Scale " +
                    userScale.ToString(
                        "F2",
                        CultureInfo.InvariantCulture);
            }
            else
            {
                newStyleName =
                    oldStyleName +
                    " Scale 1 chia " +
                    userScale.ToString(
                        "F2",
                        CultureInfo.InvariantCulture);
            }


            // AutoCAD DIMSTYLE tối đa 31 ký tự

            if (newStyleName.Length > 31)
            {
                newStyleName =
                    newStyleName.Substring(
                        0,
                        31);
            }


            Utils.Print(
                "Style mới: " +
                newStyleName);

            Utils.Print(
                "DIMLFAC: " +
                effectiveScale.ToString(
                    "F4",
                    CultureInfo.InvariantCulture));


            // =====================================================
            // 5. TẠO STYLE
            // =====================================================

            try
            {
                using (Transaction tr =
                    db.TransactionManager.StartTransaction())
                {
                    DimStyleTable dimStyles =
                        (DimStyleTable)tr.GetObject(
                            db.DimStyleTableId,
                            OpenMode.ForRead);


                    // =================================================
                    // KIỂM TRA STYLE GỐC
                    // =================================================

                    if (!dimStyles.Has(oldStyleName))
                    {
                        MessageBox.Show(
                            "Không tìm thấy Dimstyle gốc:\n\n" +
                            oldStyleName,
                            "Lỗi",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        return;
                    }


                    // =================================================
                    // NẾU STYLE MỚI ĐÃ TỒN TẠI
                    // =================================================

                    if (dimStyles.Has(newStyleName))
                    {
                        DialogResult ask =
                            MessageBox.Show(
                                "Dimstyle đã tồn tại:\n\n" +
                                newStyleName +
                                "\n\nBạn có muốn ghi đè không?",
                                "Dimstyle đã tồn tại",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                        if (ask != DialogResult.Yes)
                        {
                            Utils.Print(
                                "⏹️ Hủy lệnh.");

                            return;
                        }


                        // Mở bảng DimStyle để xóa

                        dimStyles.UpgradeOpen();

                        ObjectId existedId =
                            dimStyles[newStyleName];

                        DimStyleTableRecord existed =
                            (DimStyleTableRecord)
                            tr.GetObject(
                                existedId,
                                OpenMode.ForWrite);

                        existed.Erase();
                    }


                    // =================================================
                    // LẤY STYLE GỐC
                    // =================================================

                    ObjectId oldStyleId =
                        dimStyles[oldStyleName];

                    DimStyleTableRecord oldStyle =
                        (DimStyleTableRecord)
                        tr.GetObject(
                            oldStyleId,
                            OpenMode.ForRead);


                    // =================================================
                    // TẠO STYLE MỚI
                    // =================================================

                    dimStyles.UpgradeOpen();

                    DimStyleTableRecord newStyle =
                        new DimStyleTableRecord();

                    // -------------------------------------------------
                    // Copy toàn bộ thuộc tính từ style gốc
                    // -------------------------------------------------

                    newStyle.CopyFrom(oldStyle);

                    // -------------------------------------------------
                    // QUAN TRỌNG:
                    //
                    // CopyFrom có thể copy luôn Name của style gốc.
                    // Vì vậy PHẢI đặt Name lại sau CopyFrom.
                    // -------------------------------------------------

                    newStyle.Name =
                        newStyleName;

                    // -------------------------------------------------
                    // Ghi DIMLFAC sau CopyFrom
                    // -------------------------------------------------

                    newStyle.Dimlfac =
                        effectiveScale;

                    // -------------------------------------------------
                    // Add vào DimStyleTable
                    // -------------------------------------------------

                    ObjectId newStyleId =
                        dimStyles.Add(newStyle);

                    tr.AddNewlyCreatedDBObject(
                        newStyle,
                        true);

                    // -------------------------------------------------
                    // Đặt làm Current
                    // -------------------------------------------------

                    db.Dimstyle =
                        newStyleId;

                    tr.Commit();
                }


                // =====================================================
                // REGEN
                // =====================================================

                ed.Regen();


                // =====================================================
                // THÔNG BÁO
                // =====================================================

                string modeText;

                if (isScaleTo)
                {
                    modeText =
                        "Scale DIM to " +
                        userScale.ToString(
                            "F2",
                            CultureInfo.InvariantCulture);
                }
                else
                {
                    modeText =
                        "Scale DIM nhỏ 1/" +
                        userScale.ToString(
                            "F2",
                            CultureInfo.InvariantCulture);
                }


                string message =
                    "Đã tạo Dimstyle mới!\n\n" +

                    "Dimstyle gốc:\n" +
                    oldStyleName +
                    "\n\n" +

                    "Dimstyle mới:\n" +
                    newStyleName +
                    "\n\n" +

                    "Chế độ:\n" +
                    modeText +
                    "\n\n" +

                    "DIMLFAC = " +
                    effectiveScale.ToString(
                        "F4",
                        CultureInfo.InvariantCulture) +
                    "\n\n" +

                    "Style mới đã được đặt làm Current.\n\n" +

                    "DIM đã chọn ban đầu không bị thay đổi.";

                MessageBox.Show(
                    message,
                    "DIMSCALESYLENEW",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi tạo Dimstyle:\n\n" +
                    ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Utils.Print(
                    "❌ DIMSCALESYLENEW: " +
                    ex.Message);
            }
        }
    }


    // =============================================================
    // FORM CHỌN SCALE
    // =============================================================

    public class DimScaleForm : Form
    {
        private RadioButton rbScaleTo;
        private RadioButton rbScaleSmall;

        private TextBox txtScale;

        private Label lblPreviewValue;

        private Button btnOK;
        private Button btnCancel;

        private readonly string _oldStyle;


        // =========================================================
        // GIÁ TRỊ TRẢ VỀ
        // =========================================================

        public double ScaleValue
        {
            get;
            private set;
        }


        public bool IsScaleTo
        {
            get
            {
                return rbScaleTo.Checked;
            }
        }


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public DimScaleForm(
            string oldStyle)
        {
            _oldStyle =
                oldStyle ?? "";


            // =====================================================
            // FORM
            // =====================================================

            this.Text =
                "Tạo Dimstyle mới theo Scale";

            this.ClientSize =
                new Size(460, 315);

            this.FormBorderStyle =
                FormBorderStyle.FixedDialog;

            this.StartPosition =
                FormStartPosition.CenterScreen;

            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.ShowInTaskbar = false;


            // =====================================================
            // DIMSTYLE GỐC
            // =====================================================

            Label lblOldStyle =
                new Label();

            lblOldStyle.Text =
                "Dimstyle gốc:";

            lblOldStyle.Left = 20;
            lblOldStyle.Top = 20;
            lblOldStyle.Width = 110;


            Label lblOldStyleValue =
                new Label();

            lblOldStyleValue.Text =
                _oldStyle;

            lblOldStyleValue.Left = 135;
            lblOldStyleValue.Top = 20;
            lblOldStyleValue.Width = 300;

            lblOldStyleValue.Font =
                new System.Drawing.Font(
                    this.Font,
                    FontStyle.Bold);

            lblOldStyleValue.ForeColor =
                Color.DarkBlue;


            // =====================================================
            // CHẾ ĐỘ
            // =====================================================

            Label lblMode =
                new Label();

            lblMode.Text =
                "Chế độ:";

            lblMode.Left = 20;
            lblMode.Top = 60;
            lblMode.Width = 110;

            lblMode.Font =
                new System.Drawing.Font(
                    this.Font,
                    FontStyle.Bold);


            Panel pnlMode =
                new Panel();

            pnlMode.Left = 20;
            pnlMode.Top = 85;
            pnlMode.Width = 415;
            pnlMode.Height = 65;


            rbScaleTo =
                new RadioButton();

            rbScaleTo.Text =
                "1. Scale DIM to N lần";

            rbScaleTo.Left = 5;
            rbScaleTo.Top = 5;
            rbScaleTo.Width = 400;

            rbScaleTo.Checked = true;


            rbScaleSmall =
                new RadioButton();

            rbScaleSmall.Text =
                "2. Scale DIM nhỏ = 1/N";

            rbScaleSmall.Left = 5;
            rbScaleSmall.Top = 32;
            rbScaleSmall.Width = 400;


            pnlMode.Controls.Add(
                rbScaleTo);

            pnlMode.Controls.Add(
                rbScaleSmall);


            // =====================================================
            // HỆ SỐ
            // =====================================================

            Label lblScale =
                new Label();

            lblScale.Text =
                "Hệ số N (>0):";

            lblScale.Left = 20;
            lblScale.Top = 170;
            lblScale.Width = 120;


            txtScale =
                new TextBox();

            txtScale.Left = 145;
            txtScale.Top = 167;
            txtScale.Width = 290;

            txtScale.Text = "2";


            txtScale.TextChanged +=
                delegate
                {
                    UpdatePreview();
                };


            // =====================================================
            // PREVIEW
            // =====================================================

            Label lblPreview =
                new Label();

            lblPreview.Text =
                "Style mới:";

            lblPreview.Left = 20;
            lblPreview.Top = 215;
            lblPreview.Width = 120;

            lblPreview.Font =
                new System.Drawing.Font(
                    this.Font,
                    FontStyle.Bold);


            lblPreviewValue =
                new Label();

            lblPreviewValue.Left = 145;
            lblPreviewValue.Top = 215;
            lblPreviewValue.Width = 290;
            lblPreviewValue.Height = 35;

            lblPreviewValue.ForeColor =
                Color.DarkGreen;

            lblPreviewValue.Font =
                new System.Drawing.Font(
                    this.Font,
                    FontStyle.Bold);


            // =====================================================
            // BUTTON OK
            // =====================================================

            btnOK =
                new Button();

            btnOK.Text =
                "OK";

            btnOK.Left = 255;
            btnOK.Top = 260;
            btnOK.Width = 85;
            btnOK.Height = 30;


            // =====================================================
            // BUTTON CANCEL
            // =====================================================

            btnCancel =
                new Button();

            btnCancel.Text =
                "Cancel";

            btnCancel.Left = 350;
            btnCancel.Top = 260;
            btnCancel.Width = 85;
            btnCancel.Height = 30;


            // =====================================================
            // OK CLICK
            // =====================================================

            btnOK.Click +=
                delegate
                {
                    double value;

                    if (!TryParseScale(
                            txtScale.Text,
                            out value)
                        || value <= 0)
                    {
                        MessageBox.Show(
                            "Hệ số N phải là số lớn hơn 0.",
                            "Lỗi nhập liệu",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        this.DialogResult =
                            DialogResult.None;

                        txtScale.Focus();
                        txtScale.SelectAll();

                        return;
                    }


                    ScaleValue =
                        value;

                    this.DialogResult =
                        DialogResult.OK;
                };


            // =====================================================
            // CANCEL CLICK
            // =====================================================

            btnCancel.Click +=
                delegate
                {
                    this.DialogResult =
                        DialogResult.Cancel;
                };


            // =====================================================
            // RADIO CHANGE
            // =====================================================

            rbScaleTo.CheckedChanged +=
                delegate
                {
                    UpdatePreview();
                };

            rbScaleSmall.CheckedChanged +=
                delegate
                {
                    UpdatePreview();
                };


            // =====================================================
            // ADD CONTROLS
            // =====================================================

            this.Controls.Add(
                lblOldStyle);

            this.Controls.Add(
                lblOldStyleValue);

            this.Controls.Add(
                lblMode);

            this.Controls.Add(
                pnlMode);

            this.Controls.Add(
                lblScale);

            this.Controls.Add(
                txtScale);

            this.Controls.Add(
                lblPreview);

            this.Controls.Add(
                lblPreviewValue);

            this.Controls.Add(
                btnOK);

            this.Controls.Add(
                btnCancel);


            // =====================================================
            // ENTER / ESC
            // =====================================================

            this.AcceptButton =
                btnOK;

            this.CancelButton =
                btnCancel;


            // =====================================================
            // FOCUS
            // =====================================================

            this.Shown +=
                delegate
                {
                    txtScale.Focus();
                    txtScale.SelectAll();
                };


            UpdatePreview();
        }


        // =========================================================
        // PARSE SCALE
        // =========================================================

        private static bool TryParseScale(
            string text,
            out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            text =
                text.Trim()
                    .Replace(',', '.');

            return double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }


        // =========================================================
        // UPDATE PREVIEW
        // =========================================================

        private void UpdatePreview()
        {
            double value;

            if (!TryParseScale(
                    txtScale.Text,
                    out value)
                || value <= 0)
            {
                lblPreviewValue.Text =
                    "(Nhập hệ số N > 0)";

                lblPreviewValue.ForeColor =
                    Color.Gray;

                return;
            }


            string name;

            if (rbScaleTo.Checked)
            {
                name =
                    _oldStyle +
                    " Scale " +
                    value.ToString(
                        "F2",
                        CultureInfo.InvariantCulture);
            }
            else
            {
                name =
                    _oldStyle +
                    " Scale 1 chia " +
                    value.ToString(
                        "F2",
                        CultureInfo.InvariantCulture);
            }


            if (name.Length > 31)
            {
                name =
                    name.Substring(
                        0,
                        31);
            }


            lblPreviewValue.Text =
                name;

            lblPreviewValue.ForeColor =
                Color.DarkGreen;
        }
    }
}
