using Autocad_addin.Framework;
using Autodesk.Windows;
using System.Windows.Controls;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button_LenhVe
    {
        // ===== CỘT 1 =====
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


        // ===== CỘT 2 (tự tràn sang) =====
        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Cung",
            ToolTip = "Vẽ arc",
            Size = RibbonItemSize.Standard,
                     Icon = "A1.png",
                NewRow = true,
            Order = 4)]
        public static void VeArc() { }

        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Hình Chữ Nhật",
            ToolTip = "Vẽ rectangle",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 5)]
        public static void VeRect() { }

        [RibbonButton("MY TOOLS 3", "Lệnh Vẽ 2", "Vẽ Elip",
            ToolTip = "Vẽ ellipse",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 6)]
        public static void VeEllipse() { }

        [RibbonButton("MY TOOLS 2", "Lệnh Vẽ 2", "Vẽ Điểm",
            ToolTip = "Vẽ point",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 7)]
        public static void VePoint() { }

        // Nút nhỏ bên dưới - Order cao hơn
        [RibbonButton("MY TOOLS", "3D", "Cylinder", Order = 100)]
        public static void Cylinder() { }

        [RibbonButton("MY TOOLS", "3D", "Cone", Order = 101)]
        public static void Cone() { }

        [RibbonButton("MY TOOLS", "3D", "Sphere", Order = 102)]
        public static void Sphere() { }
    }
}