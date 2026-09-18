using Autocad_addin.Framework;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button_Text
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
        [RibbonButton("Tool CAD", "Text Tool", "Xóa Text và Leader",
            ToolTip = "Xóa text và leader",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void DeleteTextAndLeader()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("DeleteTextAndLeader");
        }
        //2
        [RibbonButton("Tool CAD", "Layer Tool", "Đổi thành chữ Time new roman",
            ToolTip = "Chuyển toàn bộ Font chữ được chọn sang Times New Roman",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void FontChangeAllTimeNewRoman()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("FontChangeAllTimeNewRoman");
        }

        //3
        [RibbonButton("Tool CAD", "Text Tool", "Tắt màu nền TXT",
            ToolTip = "Tắt màu nền của text",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void TextFillNone()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("TextFillNone");
        }
        //4
        [RibbonButton("Tool CAD", "Text Tool", "Copy dán text",
            ToolTip = "Thay nội dung text được chọn theo text mẫu",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void TextReplace()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("TextReplace");
        }


        //5
        [RibbonButton("Tool CAD", "Text Tool", "Thêm chữ vào text",
            ToolTip = "Thêm nội dung vào text được chọn",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void ThemChuVaoText()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("ThemChuVaoText");
        }


        //6
        [RibbonButton("Tool CAD", "Text Tool", "Đổi chữ hoa/thường",
            ToolTip = "Đổi giữa chữ hoa và chữ thường trong text được chọn",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void Doichuinhoa()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("Doichuinhoa");
        }

        //7
        [RibbonButton("Tool CAD", "Text Tool", "Xóa chữ trong text",
            ToolTip = "Xóa nội dung trong text được ghi",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void XoaChuTrongText()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("XoaChuTrongText");
        }





        // Thêm nút khác tương tự...
        // [RibbonButton(...)]
        // public static void TenNut()
        // {
        //     RunCommand("TENLENH");
        // }

    }
}