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
        [RibbonButton("Tool CAD", "Draw Tool", "Scale line",
            ToolTip = "Scale mật độ chiều dài line",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void ScaleLine()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("ScaleLine");
        }


        //1
        [RibbonButton("Tool CAD", "Draw Tool", "Equalize",
            ToolTip = "Thay đổi kích thước các đối tượng bằng nhau theo đối tượng mẫu giống hình dạng",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void Equalize()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("Equalize");
        }


        // Thêm nút khác tương tự...
        // [RibbonButton(...)]
        // public static void TenNut()
        // {
        //     RunCommand("TENLENH");
        // }

    }
}