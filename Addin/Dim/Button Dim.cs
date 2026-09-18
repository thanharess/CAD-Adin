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

        //1
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
        //2

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

        //3

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

        //4

        [RibbonButton("Tool CAD", "Dim Tool", "Deletedim V2",
            ToolTip = "Xóa dim nâng cao [1:Chọn / 2:Theo layer / 3:MLEADER / 4:Theo layer+vùng / 5:Tất cả] ",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void Deletedim()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("Deletedim");
        }

        //5

        [RibbonButton("Tool CAD", "Dim Tool", "Change Dim Style",
            ToolTip = "Đổi Dim Style cho các DIM được chọn",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void DIMCHANGESTYLEWRITE()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("DIMCHANGESTYLEWRITE");
        }


        //5

        [RibbonButton("Tool CAD", "Dim Tool", "DimDelete Auto",
            ToolTip = "Xóa DIMENSION",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void DimDelete()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("DimDelete");
        }

        //5

        [RibbonButton("Tool CAD", "Dim Tool", "DimTextStyle",
            ToolTip = "Đổi TextStyle cho các DIM được chọn",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void DimTextStyle()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("DimTextStyle");
        }








        // Thêm nút khác tương tự...
        // [RibbonButton(...)]
        // public static void TenNut()
        // {
        //     RunCommand("TENLENH");
        // }
    }
}