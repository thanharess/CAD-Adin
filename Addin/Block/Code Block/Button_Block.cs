using Autocad_addin.Framework;
using Autodesk.Windows;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button_Block
    {
        [RibbonButton("Tool CAD", "Block Tool", "Layer Change Block",
            ToolTip = "Thay đổi layer của block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "Block",          // ⬅️ NHÓM BLOCK
            Order = 1)]
        public static void LayerChangeBlock() { }

        [RibbonButton("Tool CAD", "Block Tool", "Create New Block",
            ToolTip = "Tạo block mới",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "Block",          // ⬅️ CÙNG NHÓM
            Order = 2)]
        public static void CreateNewBlock() { }

        [RibbonButton("Tool CAD", "Block Tool", "Đếm Block Trùng Tên",
            ToolTip = "Đếm block trùng tên",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "Block",          // ⬅️ CÙNG NHÓM
            Order = 3)]
        public static void DemBlockTrungTen() { }
    }
}