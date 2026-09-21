using CADAddin.Common;
using CADAddin.Framework;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;

namespace CADAddin.Dim
{
    public static class Button_Dim
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
        // DROPDOWN 1: Dim Create
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Dim Tool", "Dim Create",
            ToolTip = "Tạo dim tự động",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            RowsPerColumn = 3,
            Order = 1)]
        public static void DimCreateDD()
        {
            RunCommand("DIMAUTOPLATES");
        }

        [RibbonDropItem("Dim Create", "Auto Dim Polyline",
            ToolTip = "Tự động tạo DIMLINEAR cho từng cạnh LWPOLYLINE",
            Icon = ICON_SMALL, Order = 1)]
        public static void Create_DIMAUTOPLATES()
        {
            RunCommand("DIMAUTOPLATES");
        }

        // ═══════════════════════════════════════════════════════════
        // DROPDOWN 2: Dim Scale
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Dim Tool", "Dim Scale",
            ToolTip = "Scale giá trị dim và dimstyle",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            RowsPerColumn = 3,
            Order = 2)]
        public static void DimScaleDD()
        {
            RunCommand("DIMSCALESYLENEW");
        }

        [RibbonDropItem("Dim Scale", "Tạo Dim Scale",
            ToolTip = "Tạo DimStyle mới bằng cách copy từ style gốc và đổi scale",
            Icon = ICON_SMALL, Order = 1)]
        public static void Scale_DIMSCALESYLENEW()
        {
            RunCommand("DIMSCALESYLENEW");
        }

        [RibbonDropItem("Dim Scale", "Scale Dim Value Block",
            ToolTip = "Scale giá trị DIM trong block",
            Icon = ICON_SMALL   , Order = 2)]
        public static void Scale_SCALEDIMVALUEBLOCK()
        {
            RunCommand("SCALEDIMVALUEBLOCK");
        }

        [RibbonDropItem("Dim Scale", "Scale Dim Value",
            ToolTip = "Scale giá trị các DIM được chọn",
            Icon = ICON_SMALL, Order = 3)]
        public static void Scale_SCALEDIMVALUE()
        {
            RunCommand("SCALEDIMVALUE");
        }

        // ═══════════════════════════════════════════════════════════
        // DROPDOWN 3: Dim Edit
        // ═══════════════════════════════════════════════════════════

        [RibbonDropDown("Tool CAD", "Dim Tool", "Dim Edit",
            ToolTip = "Chỉnh sửa và xóa dim",
            Size = RibbonItemSize.Standard,
            Icon = ICON_SMALL,
            LargeIcon = ICON_LARGE,
            RowsPerColumn = 3,
            Order = 3)]
        public static void DimEditDD()
        {
            RunCommand("DIMCHANGESTYLEWRITE");
        }

        [RibbonDropItem("Dim Edit", "Change Dim Style",
            ToolTip = "Đổi Dim Style cho các DIM được chọn",
            Icon = ICON_SMALL, Order = 1)]
        public static void Edit_DIMCHANGESTYLEWRITE()
        {
            RunCommand("DIMCHANGESTYLEWRITE");
        }

        [RibbonDropItem("Dim Edit", "Dim Text Style",
            ToolTip = "Đổi TextStyle cho các DIM được chọn",
            Icon = ICON_SMALL, Order = 2)]
        public static void Edit_DimTextStyle()
        {
            RunCommand("DimTextStyle");
        }

     


        [RibbonDropItem("Dim Edit", "Delete Dim Auto",
            ToolTip = "Xóa DIMENSION",
            Icon = ICON_SMALL, Order = 4)]
        public static void Edit_DimDelete()
        {
            RunCommand("DimDelete");
        }
    }
}