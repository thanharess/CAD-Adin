using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class Equalize
    {
        [CommandMethod("EQCIRCLE")]
        public void EqCircle()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var sel = ed.GetSelection(new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "CIRCLE") }));
            if (sel.Status != PromptStatus.OK) return;

            var kwo = new PromptKeywordOptions("\nChế độ [Mẫu/Nhập] <Mẫu>: ") { AllowNone = true };
            kwo.Keywords.Add("Mau"); kwo.Keywords.Add("Nhap");
            var kwr = ed.GetKeywords(kwo);
            string mode = (kwr.Status != PromptStatus.OK || string.IsNullOrEmpty(kwr.StringResult))
                ? "Mau" : kwr.StringResult;

            double radius = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                if (mode == "Mau")
                {
                    // EQCIRCLE
                    var peo = new PromptEntityOptions("\nChọn đường tròn mẫu: ");
                    peo.SetRejectMessage("\nKhông phải đường tròn.");
                    peo.AddAllowedClass(typeof(Circle), false);
                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK) return;
                    var c = (Circle)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                    radius = c.Radius;
                }
                else
                {
                    var pr = ed.GetDouble("\nNhập bán kính: ");
                    if (pr.Status != PromptStatus.OK) return;
                    radius = pr.Value;
                }

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var c = (Circle)tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                    c.Radius = radius;
                }
                tr.Commit();
            }
            ed.Regen();
            Utils.Print($"[OK] Đã đồng nhất bán kính = {radius}");
        }

        [CommandMethod("EQARC")]
        public void EqArc()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var sel = ed.GetSelection(new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "ARC") }));
            if (sel.Status != PromptStatus.OK) return;

            var kwo = new PromptKeywordOptions("\nChế độ [Mẫu/Nhập] <Mẫu>: ") { AllowNone = true };
            kwo.Keywords.Add("Mau"); kwo.Keywords.Add("Nhap");
            var kwr = ed.GetKeywords(kwo);
            string mode = (kwr.Status != PromptStatus.OK || string.IsNullOrEmpty(kwr.StringResult))
                ? "Mau" : kwr.StringResult;

            double rad = 0, start = 0, end = 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                if (mode == "Mau")
                {

                    // EQARC
                    var peo = new PromptEntityOptions("\nChọn cung tròn mẫu: ");
                    peo.SetRejectMessage("\nKhông phải cung tròn.");
                    peo.AddAllowedClass(typeof(Arc), false);
                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK) return;
                    var a = (Arc)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                    rad = a.Radius; start = a.StartAngle; end = a.EndAngle;
                }
                else
                {
                    var pr = ed.GetDouble("\nNhập bán kính: ");
                    if (pr.Status != PromptStatus.OK) return;
                    rad = pr.Value;
                    var pa1 = ed.GetAngle("\nNhập góc bắt đầu: ");
                    if (pa1.Status != PromptStatus.OK) return;
                    start = pa1.Value;
                    var pa2 = ed.GetAngle("\nNhập góc kết thúc: ");
                    if (pa2.Status != PromptStatus.OK) return;
                    end = pa2.Value;
                }

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var a = (Arc)tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                    a.Radius = rad;
                    a.StartAngle = start;
                    a.EndAngle = end;
                }
                tr.Commit();
            }
            ed.Regen();
            Utils.Print("[OK] Đã đồng nhất cung tròn.");
        }

        [CommandMethod("EQRECT")]
        public void EqRect()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var sel = ed.GetSelection(new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "LWPOLYLINE") }));
            if (sel.Status != PromptStatus.OK) return;

            var kwo = new PromptKeywordOptions("\nChế độ [Mau/Nhap] <Mau>: ") { AllowNone = true };
            kwo.Keywords.Add("Mau"); kwo.Keywords.Add("Nhap");
            var kwr = ed.GetKeywords(kwo);
            string mode = (kwr.Status != PromptStatus.OK || string.IsNullOrEmpty(kwr.StringResult))
                ? "Mau" : kwr.StringResult;

            double refW = 0, refH = 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                if (mode == "Mau")
                {
                    // EQRECT
                    // EQRECT
                    var peo = new PromptEntityOptions("\nChọn RECT mẫu: ");
                    peo.SetRejectMessage("\nKhông phải polyline.");
                    peo.AddAllowedClass(typeof(Polyline), false);
                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK) return;
                    var pl = (Polyline)tr.GetObject(per.ObjectId, OpenMode.ForRead);

                    if (pl.NumberOfVertices != 4 || !pl.Closed)
                    {
                        Utils.Print("❌ Mẫu không phải rect (polyline 4 cạnh kín).");
                        return;
                    }
                    var pts = new List<Point3d>();
                    for (int i = 0; i < 4; i++) pts.Add(pl.GetPoint3dAt(i));
                    if (!IsRect(pts)) { Utils.Print("❌ Mẫu không phải hình chữ nhật hợp lệ."); return; }

                    double len1 = pts[0].DistanceTo(pts[1]);
                    double len2 = pts[1].DistanceTo(pts[2]);
                    if (len1 > len2) { refW = len1; refH = len2; }
                    else { refW = len2; refH = len1; }
                    Utils.Print($"Kích thước mẫu: {refW:F2} x {refH:F2}");
                }
                else
                {
                    var pw = ed.GetDouble("\nNhập chiều rộng (>0): ");
                    if (pw.Status != PromptStatus.OK || pw.Value <= 0) return;
                    var ph = ed.GetDouble("\nNhập chiều cao (>0): ");
                    if (ph.Status != PromptStatus.OK || ph.Value <= 0) return;
                    refW = pw.Value; refH = ph.Value;
                }

                // Cập nhật từng rect
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var pl = (Polyline)tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                    if (pl.NumberOfVertices != 4 || !pl.Closed) continue;

                    var pts = new List<Point3d>();
                    for (int i = 0; i < 4; i++) pts.Add(pl.GetPoint3dAt(i));
                    if (!IsRect(pts)) continue;

                    var cen = Centroid(pts);
                    var v1 = pts[1] - pts[0];
                    double ang = Math.Atan2(v1.Y, v1.X);

                    // Xóa các vertex cũ, add vertex mới
                    // Ghi lại các thuộc tính
                    string layer = pl.Layer;
                    int colorIdx = pl.ColorIndex;
                    var lw = pl.LineWeight;

                    // Xây dựng rect mới quanh tâm
                    double hw = refW / 2.0, hh = refH / 2.0;
                    double ca = Math.Cos(ang), sa = Math.Sin(ang);
                    Func<double, double, Point2d> rot = (x, y) =>
                        new Point2d(cen.X + x * ca - y * sa, cen.Y + x * sa + y * ca);

                    var corners = new[]
                    {
                        rot(-hw, -hh), rot(hw, -hh), rot(hw, hh), rot(-hw, hh)
                    };

                    // Xóa vertex cũ
                    while (pl.NumberOfVertices > 0)
                        pl.RemoveVertexAt(0);
                    for (int i = 0; i < 4; i++)
                        pl.AddVertexAt(i, corners[i], 0, 0, 0);
                    pl.Closed = true;
                    pl.Layer = layer;
                    pl.ColorIndex = colorIdx;
                    pl.LineWeight = lw;
                }
                tr.Commit();
            }
            ed.Regen();
            Utils.Print("✅ Hoàn thành EQRECT.");
        }

        private static bool IsRect(List<Point3d> pts)
        {
            const double tol = 1e-4;
            if (pts.Count != 4) return false;
            var v1 = pts[1] - pts[0];
            var v2 = pts[2] - pts[1];
            var v3 = pts[3] - pts[2];
            var v4 = pts[0] - pts[3];

            double l1 = v1.Length, l2 = v2.Length, l3 = v3.Length, l4 = v4.Length;
            if (l1 < tol || l2 < tol) return false;

            double dot1 = v1.X * v2.X + v1.Y * v2.Y;
            double dot2 = v2.X * v3.X + v2.Y * v3.Y;
            return Math.Abs(l1 - l3) < tol * l1 &&
                   Math.Abs(l2 - l4) < tol * l2 &&
                   Math.Abs(dot1) < tol * l1 * l2 &&
                   Math.Abs(dot2) < tol * l2 * l3;
        }

        private static Point3d Centroid(List<Point3d> pts)
        {
            double sx = 0, sy = 0, sz = 0;
            foreach (var p in pts) { sx += p.X; sy += p.Y; sz += p.Z; }
            int n = pts.Count;
            return new Point3d(sx / n, sy / n, sz / n);
        }
    }
}