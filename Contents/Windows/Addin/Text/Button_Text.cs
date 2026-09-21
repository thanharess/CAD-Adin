using CADAddin.Common;
using CADAddin.Framework;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace CADAddin.TextTools
{
    public static class Button_Text
    {
        // ═══════════════════════════════════════════════════════════
        // ICON
        // ═══════════════════════════════════════════════════════════
        private const string ICON_SMALL = "A1.png";
        private const string ICON_LARGE = "A2.png";

        private static void RunCommand(string commandName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            doc.SendStringToExecute(commandName + " ", true, false, false);
        }

        // ═══════════════════════════════════════════════════════════
        // DROPDOWN 1: TEXT CLEAN — Xóa / Dọn
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Text Tool", "Text Clean",
            ToolTip = "Xóa chữ, tắt nền và xóa text",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            RowsPerColumn = 3,
            Order = 1)]
        public static void TextCleanDD()
        {
            RunCommand("XoahoacthemChuTrongText");
        }

        [RibbonDropItem("Text Clean", "Xóa và thêm chữ text",
            ToolTip = "Xóa hoặc thêm cụm từ trong text được chọn",
            Icon = ICON_SMALL, Order = 1)]
        public static void Clean_XoaChuTrongText()
        {
            RunCommand("XoahoacthemChuTrongText");
        }

        [RibbonDropItem("Text Clean", "Tắt màu nền TXT",
            ToolTip = "Tắt màu nền (Background Fill) của MTEXT",
            Icon = ICON_SMALL, Order = 2)]
        public static void Clean_TextFillNone()
        {
            RunCommand("Textfillnone");
        }

        [RibbonDropItem("Text Clean", "Xóa Text & Leader",
            ToolTip = "Xóa Text, MText, Leader, MLeader",
            Icon = ICON_SMALL, Order = 3)]
        public static void Clean_DeleteTextAndLeader()
        {
            RunCommand("Deletetextandleader");
        }

        // ═══════════════════════════════════════════════════════════
        // DROPDOWN 2: TEXT EDIT — Sửa / Định dạng
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Text Tool", "Text Edit",
            ToolTip = "Sửa nội dung và định dạng text",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            RowsPerColumn = 3,
            Order = 2)]
        public static void TextEditDD()
        {
            RunCommand("TextReplace");
        }

        [RibbonDropItem("Text Edit", "Copy dán text",
            ToolTip = "Thay nội dung text theo text mẫu",
            Icon = ICON_SMALL, Order = 1)]
        public static void Edit_TextReplace()
        {
            RunCommand("TextReplace");
        }

        [RibbonDropItem("Text Edit", "Thêm chữ vào text",
            ToolTip = "Thêm nội dung vào text được chọn",
            Icon = ICON_SMALL, Order = 2)]
        public static void Edit_ThemChuVaoText()
        {
            RunCommand("ThemChuVaoText");
        }

        [RibbonDropItem("Text Edit", "Đổi chữ HOA/thường",
            ToolTip = "Đổi giữa chữ HOA và chữ thường",
            Icon = ICON_SMALL, Order = 3)]
        public static void Edit_Doichuinhoa()
        {
            RunCommand("Doiinhoachu");
        }

        [RibbonDropItem("Text Edit", "Times New Roman",
            ToolTip = "Đổi toàn bộ Font chữ sang Times New Roman",
            Icon = ICON_SMALL, Order = 4)]
        public static void Edit_FontTNR()
        {
            RunCommand("Fontchangealltimenewroman");
        }
    }
}