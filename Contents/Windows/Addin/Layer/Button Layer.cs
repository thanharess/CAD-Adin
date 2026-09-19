using CADAddin.Common;
using CADAddin.Framework;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace CADAddin.Layer
{
    public static class Button_Layer
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
        // DROPDOWN 1: LAYER CHANGE — Đổi layer
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Layer Tool", "Layer Change",
            ToolTip = "Đổi layer cho đối tượng",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            RowsPerColumn = 3,
            Order = 1)]
        public static void LayerChangeDD()
        {
            RunCommand("CHANGELAYER");
        }

        [RibbonDropItem("Layer Change", "Change Layer",
            ToolTip = "Đổi layer cho đối tượng được chọn",
            Icon = ICON_SMALL, Order = 1)]
        public static void Change_ChangeLayer()
        {
            RunCommand("CHANGELAYER");
        }

        [RibbonDropItem("Layer Change", "Đổi layer theo Linetype",
            ToolTip = "Đổi layer cho các linetype trùng nhau",
            Icon = ICON_SMALL, Order = 2)]
        public static void Change_LayerChangeAM()
        {
            RunCommand("LAYERCHANGEAM");
        }

     
        // ═══════════════════════════════════════════════════════════
        // DROPDOWN 2: LAYER TOOLS — Xóa và công cụ
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Layer Tool", "Layer Tools",
            ToolTip = "Xóa và quản lý layer",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            RowsPerColumn = 3,
            Order = 2)]
        public static void LayerToolsDD()
        {
            RunCommand("LAYERDELETE");
        }

        [RibbonDropItem("Layer Tools", "Delete Layer",
            ToolTip = "Xóa layer khỏi bản vẽ",
            Icon = ICON_SMALL, Order = 1)]
        public static void Tools_LayerDelete()
        {
            RunCommand("LAYERDELETE");
        }
    }
}