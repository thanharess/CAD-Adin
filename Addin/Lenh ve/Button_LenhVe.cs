using Autocad_addin.Framework;
using Autodesk.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button
    {
        // ===== CỘT 1 =====
        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Đường Thẳng",
            ToolTip = "Vẽ line",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Namespace = "dim",
            Order = 1)]
        public static void SCALEDIMVALUE()
        {
            RunLisp("(c:SCALEDIMVALUE)");
        }

        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Đa Giác",
            ToolTip = "Vẽ polyline",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 2)]
        public static void VePolyline()
        {
            RunLisp("(command \"_.PLINE\")");
        }

        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Đường Tròn",
            ToolTip = "Vẽ circle",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 3)]
        public static void VeCircle()
        {
            RunLisp("(command \"_.CIRCLE\")");
        }

        // ===== CỘT 2 =====
        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Cung",
            ToolTip = "Vẽ arc",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Order = 4)]
        public static void VeArc()
        {
            RunLisp("(command \"_.ARC\")");
        }

        [RibbonButton("MY TOOLS", "Lệnh Vẽ", "Vẽ Hình Chữ Nhật",
            ToolTip = "Vẽ rectangle",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 5)]
        public static void VeRect()
        {
            RunLisp("(command \"_.RECTANG\")");
        }

        [RibbonButton("MY TOOLS 3", "Lệnh Vẽ 2", "Vẽ Elip",
            ToolTip = "Vẽ ellipse",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 6)]
        public static void VeEllipse()
        {
            RunLisp("(command \"_.ELLIPSE\")");
        }

        [RibbonButton("MY TOOLS 2", "Lệnh Vẽ 2", "Vẽ Điểm",
            ToolTip = "Vẽ point",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            Order = 7)]
        public static void VePoint()
        {
            RunLisp("(command \"_.POINT\")");
        }

        // Nút 3D
        [RibbonButton("MY TOOLS", "3D", "Cylinder", Order = 100)]
        public static void Cylinder()
        {
            RunLisp("(command \"_.CYLINDER\")");
        }

        [RibbonButton("MY TOOLS", "3D", "Cone", Order = 101)]
        public static void Cone()
        {
            RunLisp("(command \"_.CONE\")");
        }

        [RibbonButton("MY TOOLS", "3D", "Sphere", Order = 102)]
        public static void Sphere()
        {
            RunLisp("(command \"_.SPHERE\")");
        }

        // ===== Hàm chạy LISP =====
        private static void RunLisp(string lispCode)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            doc.SendStringToExecute(lispCode + " ", true, false, false);
        }
    }
}