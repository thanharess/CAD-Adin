using Autocad_addin.Framework;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button_Block
    {
        // ===== Hàm gọi lệnh C# có sẵn =====
        private static void RunCommand(string commandName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.SendStringToExecute(commandName + " ", true, false, false);
        }

        // =====================================================
        // CÁC NÚT
        // =====================================================

        [RibbonButton("Tool CAD", "Block Tool", "Block Base Point",
            ToolTip = "Thay đổi điểm gốc của block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 1)]
        public static void BlockBasePoint()
        {
            RunCommand("BMBASEPOINT");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Đếm block",
            ToolTip = "Đếm số lần xuất hiện của block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 2)]
        public static void BLCOUNT()
        {
            RunCommand("BLCOUNT");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Xóa Block",
            ToolTip = "Xóa các block có cùng tên",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 3)]
        public static void BLDELETE()
        {
            RunCommand("BLDELETE");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Phá block",
            ToolTip = "Phá các block trong model có cùng tên",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 4)]
        public static void BLEXPLODE()
        {
            RunCommand("BLEXPLODE");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Block Highlight",
            ToolTip = "Highlight tất cả block cùng tên",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "Block",
            Order = 5)]
        public static void BLHLAYER()
        {
            RunCommand("BLHLAYER");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Change Layer Block",
            ToolTip = "Change layer of all blocks with the same name",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 6)]
        public static void LAYERCHANGEBLOCK()
        {
            RunCommand("LAYERCHANGEBLOCK");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Thay tên block",
            ToolTip = "Đổi tên block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 7)]
        public static void BLRENAME()
        {
            RunCommand("BLRENAME");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Replace block",
            ToolTip = "Thay thế block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 8)]
        public static void BLREPLACE()
        {
            RunCommand("BLREPLACE");
        }

        [RibbonButton("Tool CAD", "Block Tool", "SAVE AS NEW BLOCK",
            ToolTip = "Lưu block hiện tại dưới tên mới",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 9)]
        public static void BLSAVEASNEWBLOCK()
        {
            RunCommand("BLSAVEASNEWBLOCK");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Đổi Block",
            ToolTip = "Hoán đổi vị trí giữa hai block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 10)]
        public static void BLSWAP()
        {
            RunCommand("BLSWAP");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Change Units Block",
            ToolTip = "Đổi tất cả đơn vị về mm",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "BlockTools",
            Order = 11)]
        public static void BLchangeallunitmm()
        {
            RunCommand("BLchangeallunitmm");
        }
    }
}