using Autocad_addin.Framework;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button_Layer
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
        //2
        [RibbonButton("Tool CAD", "Layer Tool", "Thay đổi Linetype AM",
            ToolTip = "Chuyển toàn bộ line layer được chọn sang line layer khác",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void LayerChangeAM()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("LayerChangeAM");
        }


        //3

        [RibbonButton("Tool CAD", "Layer Tool", "Change Layer",
            ToolTip = "Chuyển toàn bộ line layer được chọn sang line layer khác",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void ChangeLayer()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("ChangeLayer");


        }


        //3

        [RibbonButton("Tool CAD", "Layer Tool", "Delete Layer",
            ToolTip = "Xóa layer",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 2)]
        public static void LayerDelete()
        {
            // Nếu bạn đã viết bằng C# thuần thì gọi method trực tiếp
            // còn nếu vẫn muốn dùng lệnh thì:
            RunCommand("LayerDelete");


        }





        // Thêm nút khác tương tự...
        // [RibbonButton(...)]
        // public static void TenNut()
        // {
        //     RunCommand("TENLENH");
        // }


    }
    }