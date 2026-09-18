using Autocad_addin.Framework;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button_Dim
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

        [RibbonButton("Tool CAD", "Dim Tool", "Auto dim polyline",
            ToolTip = "Tự động tạo DIMLINEAR cho từng cạnh của LWPOLYLINE.",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 1)]
        public static void DIMAUTOPLATES()
        {
            RunCommand("DIMAUTOPLATES");          // ← tên CommandMethod
        }

        //2
        [RibbonButton("Tool CAD", "Dim Tool", "Tạo dim scale",
            ToolTip = "Tạo DimStyle mới bằng cách copy từ style gốc đổi scale dim style",
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
        //3

        [RibbonButton("Tool CAD", "Dim Tool", "Scale Dim Value Block",
            ToolTip = "Scale giá trị của các DIM trong block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void SCALEDIMVALUEBLOCK()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("SCALEDIMVALUEBLOCK");
        }

        //4

        [RibbonButton("Tool CAD", "Dim Tool", "Scale Dim Value",
            ToolTip = "Scale giá trị của các DIM",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void SCALEDIMVALUE()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("SCALEDIMVALUE");
        }

        //6

        [RibbonButton("Tool CAD", "Dim Tool", "Block Highlight",
            ToolTip = "Highlight tất cả block cùng tên bằng cách chuyển layer và đổi màu layer tạm",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void BLHLAYER()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("BLHLAYER");
        }

        //6

        [RibbonButton("Tool CAD", "Dim Tool", "Change Layer Block",
            ToolTip = "Change layer of all blocks with the same name",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void LAYERCHANGEBLOCK()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("LAYERCHANGEBLOCK");
        }

        //4
        [RibbonButton("Tool CAD", "Dim Tool", "Thay tên block",
            ToolTip = "Đổi tên block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void BLRENAME()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("BLRENAME");
        }

        //4
        [RibbonButton("Tool CAD", "Dim Tool", "Replace block",
            ToolTip = "Thay thế block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void BLREPLACE()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("BLREPLACE");
        }

        //4

        [RibbonButton("Tool CAD", "Dim Tool", "SAVE AS NEW BLOCK",
            ToolTip = "Lưu block hiện tại dưới tên mới",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void BLSAVEASNEWBLOCK()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("BLSAVEASNEWBLOCK");
        }

        //4

        [RibbonButton("Tool CAD", "Dim Tool", "Đổi Block",
            ToolTip = "Hoán đổi vị trí giữa hai block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void BLSWAP()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("BLSWAP");
        }



        [RibbonButton("Tool CAD", "Dim Tool", "Change Units Block",
            ToolTip = "Đổi tất cả đơn vị về mm",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 3)]
        public static void BLchangeallunitmm()
        {
            RunCommand("BLchangeallunitmm");    // ← tên CommandMethod của bạn
        }

        // Thêm nút khác tương tự...
        // [RibbonButton(...)]
        // public static void TenNut()
        // {
        //     RunCommand("TENLENH");
        // }
    }
}