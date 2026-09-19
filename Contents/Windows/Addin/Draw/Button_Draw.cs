using CADAddin.Common;                    // ← THÊM để gọi Utils (nếu cần)
using CADAddin.Framework;                 // ← THÊM để dùng RibbonButton
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace CADAddin.Draw

{
    public static class Button_Draw
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

        //1
        [RibbonButton("Tool CAD", "Layer Tool", "Replace Layer line",
            ToolTip = "Chuyển toàn bộ line layer được chọn sang line layer khác",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void DIMSCALESYLENEW()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("DIMSCALESYLENEW");
        }
       
          


                // Thêm nút khác tương tự...
                // [RibbonButton(...)]
                // public static void TenNut()
                // {
                //     RunCommand("TENLENH");
                // }
            
    }
}