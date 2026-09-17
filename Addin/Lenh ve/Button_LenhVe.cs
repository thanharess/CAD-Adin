using Autocad_addin.Framework;
using Autocad_addin.Framework.Autocad_addin.Framework;
using Autodesk.Windows;
using System.Windows.Controls;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button_LenhVe
    {
        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Đường Thẳng",
            ToolTip = "Vẽ line",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 1)]
        public static void VeLine() { }

        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Đa Giác",
            ToolTip = "Vẽ polyline",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 2)]
        public static void VePolyline() { }

        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Đường Tròn",
            ToolTip = "Vẽ circle",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 3)]
        public static void VeCircle() { }
    }
}