using CADAddin.Common;
using CADAddin.Framework;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace CADAddin.Block
{
    public static class Button_Block
    {
        // ═══════════════════════════════════════════════════════════
        // ICON — ĐỔI TẠI ĐÂY
        // ═══════════════════════════════════════════════════════════
        private const string ICON_SMALL = "A1.png";   // 16×16 cho item con
        private const string ICON_LARGE = "A2.png";   // 32×32 cho nút DropDown cha

        // ═══════════════════════════════════════════════════════════
        // HÀM GỌI LỆNH
        // ═══════════════════════════════════════════════════════════
        private static void RunCommand(string commandName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.SendStringToExecute(commandName + " ", true, false, false);
        }

        // ═══════════════════════════════════════════════════════════
        // DROPDOWN 1: BLOCK INFO — Xem thông tin block
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Block Tool", "Block Info",
            ToolTip = "Xem thông tin block",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            Order = 1)]
        public static void BlockInfoDD()
        {
            RunCommand("BLCOUNT");   // bấm trực tiếp nút cha → chạy BLCOUNT
        }

        [RibbonDropItem("Block Info", "Đếm block",
            ToolTip = "Đếm số lần xuất hiện của block",
            Icon = ICON_SMALL, Order = 1)]
        public static void Info_BLCOUNT()
        {
            RunCommand("BLCOUNT");
        }

        [RibbonDropItem("Block Info", "Block Highlight",
            ToolTip = "Highlight tất cả block cùng tên",
            Icon = ICON_SMALL, Order = 2)]
        public static void Info_BLHLAYER()
        {
            RunCommand("BLHLAYER");
        }

        [RibbonDropItem("Block Info", "Change Layer Block",
            ToolTip = "Đổi layer của block cùng tên",
            Icon = ICON_SMALL, Order = 3)]
        public static void Info_LAYERCHANGEBLOCK()
        {
            RunCommand("LAYERCHANGEBLOCK");
        }

        [RibbonDropItem("Block Info", "Block Base Point",
            ToolTip = "Thay đổi điểm gốc của block",
            Icon = ICON_SMALL, Order = 4)]
        public static void Info_BMBASEPOINT()
        {
            RunCommand("BMBASEPOINT");
        }

        // ═══════════════════════════════════════════════════════════
        // DROPDOWN 2: BLOCK EDIT — Chỉnh sửa block
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Block Tool", "Block Edit",
            ToolTip = "Chỉnh sửa block",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            Order = 2)]
        public static void BlockEditDD()
        {
            RunCommand("BLRENAME");   // mặc định
        }

        [RibbonDropItem("Block Edit", "Xóa Block",
            ToolTip = "Xóa các block có cùng tên",
            Icon = ICON_SMALL, Order = 1)]
        public static void Edit_BLDELETE()
        {
            RunCommand("BLDELETE");
        }

        [RibbonDropItem("Block Edit", "Phá block",
            ToolTip = "Phá các block trong model có cùng tên",
            Icon = ICON_SMALL, Order = 2)]
        public static void Edit_BLEXPLODE()
        {
            RunCommand("BLEXPLODE");
        }

        [RibbonDropItem("Block Edit", "Thay tên block",
            ToolTip = "Đổi tên block",
            Icon = ICON_SMALL, Order = 3)]
        public static void Edit_BLRENAME()
        {
            RunCommand("BLRENAME");
        }

        [RibbonDropItem("Block Edit", "Replace block",
            ToolTip = "Thay thế block",
            Icon = ICON_SMALL, Order = 4)]
        public static void Edit_BLREPLACE()
        {
            RunCommand("BLREPLACE");
        }

        // ═══════════════════════════════════════════════════════════
        // DROPDOWN 3: BLOCK TOOLS — Công cụ khác
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Block Tool", "Block Tools",
            ToolTip = "Công cụ block khác",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            Order = 3)]
        public static void BlockToolsDD()
        {
            RunCommand("BLSAVEASNEWBLOCK");   // mặc định
        }

        [RibbonDropItem("Block Tools", "Save As New Block",
            ToolTip = "Lưu block hiện tại dưới tên mới",
            Icon = ICON_SMALL, Order = 1)]
        public static void Tools_BLSAVEASNEWBLOCK()
        {
            RunCommand("BLSAVEASNEWBLOCK");
        }

       
        [RibbonDropItem("Block Tools", "Change Units Block",
            ToolTip = "Đổi tất cả đơn vị về mm",
            Icon = ICON_SMALL, Order = 3)]
        public static void Tools_BLchangeallunitmm()
        {
            RunCommand("BLchangeallunitmm");
        }
    }
}