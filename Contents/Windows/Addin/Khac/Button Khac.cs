using CADAddin.Common;                    // ← THÊM để gọi Utils (nếu cần)
using CADAddin.Framework;                 // ← THÊM để dùng RibbonButton
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace CADAddin.Khac

{
    public static class Button_Khac
    {
        // ===== Hàm gọi lệnh C# có sẵn =====
        private static void RunCommand(string commandName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Gọi lệnh đúng tên CommandMethod
            doc.SendStringToExecute(commandName + " ", true, false, false);
        }

        // =====================================================
        // CÁC NÚT GỌI LỆNH CÓ SẴN
        // =====================================================

        [RibbonButton("Tool CAD", "Tools Khác", "Tính diện tích",
            ToolTip = "Tính diện tích các đối tượng kín đã join line.",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 1)]
        public static void AT()
        {
            RunCommand("AT");          // ← tên CommandMethod
        }












        // Thêm nút khác tương tự...
        // [RibbonButton(...)]
        // public static void TenNut()
        // {
        //     RunCommand("TENLENH");
        // }
    }
}