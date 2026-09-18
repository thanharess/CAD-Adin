using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Dim
{
    public class DimScaleStyleNew
    {
        [CommandMethod("DIMSCALESYLENEW")]
        public void CreateScaledDimStyle()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // ── Bước 1: Chọn DIMENSION để lấy style gốc ──
            var peo = new PromptEntityOptions("\nChọn một DIMENSION để lấy Dimstyle gốc: ");
            peo.SetRejectMessage("\nĐối tượng chọn không phải DIMENSION.");
            peo.AddAllowedClass(typeof(Dimension), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            string oldStyle;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var dim = (Dimension)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                oldStyle = dim.DimensionStyleName;

                // Nếu style có tên kiểu "S30$0" → cắt phần trước dấu $
                int idx = oldStyle.IndexOf('$');
                if (idx > 0) oldStyle = oldStyle.Substring(0, idx);

                tr.Commit();
            }
            Utils.Print($"Dimstyle gốc: {oldStyle}");

            // ── Bước 2: Chọn chế độ ──
            var kwo = new PromptKeywordOptions("\nChọn chế độ [1:Phóng to / 2:Thu nhỏ] <1>: ")
            { AllowNone = true };
            kwo.Keywords.Add("1"); kwo.Keywords.Add("2");
            var kwr = ed.GetKeywords(kwo);
            string choice = (kwr.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr.StringResult))
                ? kwr.StringResult : "1";
            string mode = choice == "1" ? "Phóng to" : "Thu nhỏ";
            Utils.Print($"Chế độ: {mode}");

            // ── Bước 3: Nhập hệ số scale ──
            double scaleVal = 0;
            while (scaleVal <= 0)
            {
                var pr = ed.GetDouble("\nNhập hệ số scale >0 (VD: 2 = gấp đôi kích thước): ");
                if (pr.Status != PromptStatus.OK) return;
                scaleVal = pr.Value;
                if (scaleVal <= 0) Utils.Print("❌ Hệ số phải > 0.");
            }

            if (choice == "2") scaleVal = 1.0 / scaleVal;
            Utils.Print($"Hệ số scale áp dụng: {scaleVal:F2}");

            // ── Bước 4: Tạo tên style mới ──
            string newStyleName = (choice == "2")
                ? $"{oldStyle} Scale 1 chia {1.0 / scaleVal:F2}"
                : $"{oldStyle} Scale {scaleVal:F2}";

            if (newStyleName.Length > 31)
                newStyleName = newStyleName.Substring(0, 31);
            Utils.Print($"Tên style mới: {newStyleName}");

            // ── Bước 5: Kiểm tra tồn tại ──
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var dimStyles = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                if (dimStyles.Has(newStyleName))
                {
                    var kwo2 = new PromptKeywordOptions($"\nDimstyle '{newStyleName}' đã tồn tại. Ghi đè? [Yes/No] <Yes>: ")
                    { AllowNone = true };
                    kwo2.Keywords.Add("Yes"); kwo2.Keywords.Add("No");
                    var kwr2 = ed.GetKeywords(kwo2);
                    if (kwr2.Status == PromptStatus.OK && kwr2.StringResult == "No")
                    {
                        Utils.Print("⏹️ Hủy lệnh.");
                        return;
                    }

                    // Xóa style cũ
                    dimStyles.UpgradeOpen();
                    var old = (DimStyleTableRecord)tr.GetObject(dimStyles[newStyleName], OpenMode.ForWrite);
                    old.Erase();
                }

                if (!dimStyles.Has(oldStyle))
                {
                    Utils.Print($"❌ Không tìm thấy dimstyle gốc '{oldStyle}'.");
                    return;
                }

                // ── Bước 6: Tạo style mới ──
                dimStyles.UpgradeOpen();
                var newRec = new DimStyleTableRecord { Name = newStyleName };
                dimStyles.Add(newRec);
                tr.AddNewlyCreatedDBObject(newRec, true);

                var oldRec = (DimStyleTableRecord)tr.GetObject(dimStyles[oldStyle], OpenMode.ForRead);
                newRec.CopyFrom(oldRec);

                // Ghi DIMLFAC
                newRec.Dimlfac = scaleVal;

                tr.Commit();
            }

            // Đặt làm current
            AcApp.SetSystemVariable("DIMSTYLE", newStyleName);

            ed.WriteMessage(
                $"\n✅ Đã tạo Dimstyle mới:" +
                $"\n   - Tên: {newStyleName}" +
                $"\n   - Sao chép từ: {oldStyle}" +
                $"\n   - Measurement Scale Factor: {scaleVal:F2}" +
                $"\n(Đã đặt làm Current. Mở DIMSTYLE để kiểm tra Preview.)");
        }
    }
}