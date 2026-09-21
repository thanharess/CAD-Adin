using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

// Alias tránh xung đột Font
using WinFont = System.Drawing.Font;

namespace BlockTools
{
    public enum EqualizeFunction { Circle, Arc, Rect }

    public class Equalize
    {
        // Nhớ lựa chọn lần trước
        private static EqualizeFunction _lastFunc = EqualizeFunction.Circle;

        // ═══════════════════════════════════════════════════════════
        //  LỆNH CHUNG — Mở form chọn chức năng
        // ═══════════════════════════════════════════════════════════
        [CommandMethod("EQUALIZE")]
        public void EqualizeCommand()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            using (var form = new EqualizeForm(_lastFunc))
            {
                if (AcApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    Utils.Print("⏹️ Hủy lệnh.");
                    return;
                }
                _lastFunc = form.SelectedFunction;
            }

            switch (_lastFunc)
            {
                case EqualizeFunction.Circle: EqCircle(); break;
                case EqualizeFunction.Arc: EqArc(); break;
                case EqualizeFunction.Rect: EqRect(); break;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  EQCIRCLE — Đồng nhất bán kính đường tròn
        // ═══════════════════════════════════════════════════════════
        [CommandMethod("Equalize")]
        public void EqCircle()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var sel = ed.GetSelection(new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "CIRCLE") }));
            if (sel.Status != PromptStatus.OK || sel.Value.Count == 0) return;

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

                int cnt = 0;
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var c = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Circle;
                    if (c == null) continue;
                    c.Radius = radius;
                    c.RecordGraphicsModified(true);
                    cnt++;
                }
                tr.Commit();
                Utils.Print($"✔ Đã đồng nhất {cnt} đường tròn, R = {radius:F4}");
            }
            ed.Regen();
        }

        // ═══════════════════════════════════════════════════════════
        //  EQARC — Đồng nhất cung tròn
        // ═══════════════════════════════════════════════════════════
        [CommandMethod("EQARC")]
        public void EqArc()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var sel = ed.GetSelection(new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "ARC") }));
            if (sel.Status != PromptStatus.OK || sel.Value.Count == 0) return;

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
                    var peo = new PromptEntityOptions("\nChọn cung tròn mẫu: ");
                    peo.SetRejectMessage("\nKhông phải cung tròn.");
                    peo.AddAllowedClass(typeof(Arc), false);
                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK) return;
                    var a = (Arc)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                    rad = a.Radius;
                    start = a.StartAngle;
                    end = a.EndAngle;
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

                int cnt = 0;
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var a = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Arc;
                    if (a == null) continue;
                    a.Radius = rad;
                    a.StartAngle = start;
                    a.EndAngle = end;
                    a.RecordGraphicsModified(true);
                    cnt++;
                }
                tr.Commit();
                Utils.Print($"✔ Đã đồng nhất {cnt} cung tròn.");
            }
            ed.Regen();
        }

        // ═══════════════════════════════════════════════════════════
        //  EQRECT — Đồng nhất kích thước hình chữ nhật
        //  ★ Fix: Giữ nguyên hướng cạnh AB gốc, không đảo, reset bulge
        // ═══════════════════════════════════════════════════════════
        [CommandMethod("EQRECT")]
        public void EqRect()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var sel = ed.GetSelection(new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "LWPOLYLINE") }));
            if (sel.Status != PromptStatus.OK || sel.Value.Count == 0)
            {
                Utils.Print("❌ Không có đối tượng nào được chọn.");
                return;
            }

            var kwo = new PromptKeywordOptions("\nChế độ [Mau/Nhap] <Mau>: ") { AllowNone = true };
            kwo.Keywords.Add("Mau");
            kwo.Keywords.Add("Nhap");
            var kwr = ed.GetKeywords(kwo);
            string mode = (kwr.Status != PromptStatus.OK || string.IsNullOrEmpty(kwr.StringResult))
                ? "Mau" : kwr.StringResult;

            double refW = 0, refH = 0;
            bool sampleABIsLong = true;   // Cạnh AB của mẫu có phải cạnh DÀI không?

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // ═══════════════════════════════════════════════════
                //  Lấy kích thước mẫu
                // ═══════════════════════════════════════════════════
                if (mode == "Mau")
                {
                    var peo = new PromptEntityOptions("\nChọn RECT mẫu (Polyline 4 cạnh kép kín): ");
                    peo.SetRejectMessage("\nKhông phải polyline.");
                    peo.AddAllowedClass(typeof(Polyline), false);
                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK) return;

                    var pl = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                    if (pl == null || pl.NumberOfVertices != 4 || !pl.Closed)
                    {
                        Utils.Print("❌ Mẫu không phải RECT hợp lệ (4 cạnh kép kín).");
                        return;
                    }

                    var pts = GetPolylinePoints(pl);
                    if (!IsRect(pts, 1e-3))
                    {
                        Utils.Print("❌ Mẫu không phải RECT hợp lệ (cạnh không vuông góc).");
                        return;
                    }

                    // ★ Xác định cạnh AB mẫu là DÀI hay NGẮN
                    double lenABmau = pts[0].DistanceTo(pts[1]);
                    double lenBCmau = pts[1].DistanceTo(pts[2]);

                    if (lenABmau >= lenBCmau)
                    {
                        refW = lenABmau;
                        refH = lenBCmau;
                        sampleABIsLong = true;
                    }
                    else
                    {
                        refW = lenBCmau;
                        refH = lenABmau;
                        sampleABIsLong = false;
                    }

                    Utils.Print($"Kích thước mẫu: W={refW:F2} × H={refH:F2} | " +
                                $"Cạnh AB mẫu: {(sampleABIsLong ? "DÀI" : "NGẮN")}");
                }
                else
                {
                    var pw = ed.GetDouble("\nNhập chiều rộng W (>0): ");
                    if (pw.Status != PromptStatus.OK || pw.Value <= 0) return;
                    var ph = ed.GetDouble("\nNhập chiều cao H (>0): ");
                    if (ph.Status != PromptStatus.OK || ph.Value <= 0) return;
                    refW = pw.Value;
                    refH = ph.Value;
                    sampleABIsLong = true;   // mặc định: AB = cạnh dài
                }

                // ═══════════════════════════════════════════════════
                //  Hỏi IgnoreCheck
                // ═══════════════════════════════════════════════════
                var kwoChk = new PromptKeywordOptions(
                    "\nCho phép sửa RECT hơi lệch (không vuông góc hoàn hảo)? [Yes/No] <No>: ")
                { AllowNone = true };
                kwoChk.Keywords.Add("Yes");
                kwoChk.Keywords.Add("No");
                var kwrChk = ed.GetKeywords(kwoChk);
                bool ignoreCheck = (kwrChk.Status == PromptStatus.OK && kwrChk.StringResult == "Yes");
                double tol = ignoreCheck ? 1e-2 : 1e-4;

                // ═══════════════════════════════════════════════════
                //  Duyệt từng RECT
                // ═══════════════════════════════════════════════════
                int success = 0;
                int failed = 0;
                var failedReasons = new List<string>();

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;

                    var pl = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Polyline;
                    if (pl == null) continue;

                    if (pl.NumberOfVertices != 4 || !pl.Closed)
                    {
                        failed++;
                        failedReasons.Add("Không phải polyline kép kín 4 cạnh");
                        continue;
                    }

                    var pts = GetPolylinePoints(pl);

                    if (!IsRect(pts, tol))
                    {
                        failed++;
                        failedReasons.Add("Cạnh không vuông góc");
                        continue;
                    }

                    try
                    {
                        RebuildRect(pl, pts, refW, refH, sampleABIsLong);
                        success++;

                        // Debug
                        var newPts = GetPolylinePoints(pl);
                        double newAB = newPts[0].DistanceTo(newPts[1]);
                        double newBC = newPts[1].DistanceTo(newPts[2]);
                        Utils.Print($"  ▸ Rect {success}: AB={newAB:F2}, BC={newBC:F2}");
                    }
                    catch (System.Exception ex)
                    {
                        failed++;
                        failedReasons.Add(ex.Message);
                    }
                }

                tr.Commit();

                Utils.Print($"✅ Hoàn thành EQRECT — Thành công: {success}, Lỗi/Bỏ qua: {failed}");
                if (failed > 0 && failedReasons.Count > 0)
                {
                    foreach (var r in failedReasons.Distinct().Take(5))
                        Utils.Print($"   • {r}");
                }
            }

            ed.Regen();
        }

        // ═══════════════════════════════════════════════════════════
        //  ★ REBUILD RECT — Fix toàn bộ
        //    - Giữ NGUYÊN hướng cạnh AB gốc (không đảo thứ tự)
        //    - Áp kích thước W/H vào đúng vị trí cạnh AB/BC
        //    - Reset bulge để cạnh thẳng
        // ═══════════════════════════════════════════════════════════
        private static void RebuildRect(Polyline pl, List<Point3d> pts,
            double refW, double refH, bool sampleABIsLong)
        {
            // ── 1. Tính tâm ──
            var cen = Centroid(pts);

            // ── 2. Hướng cạnh AB gốc (giữ nguyên hướng) ──
            var vAB = pts[1] - pts[0];
            var dirAB = vAB.GetNormal();

            // ── 3. Hướng cạnh BC gốc — ép vuông góc với dirAB ──
            var vBC = pts[2] - pts[1];
            var dirBCraw = vBC.GetNormal();

            var perpCCW = new Vector3d(-dirAB.Y, dirAB.X, 0);
            var perpCW = new Vector3d(dirAB.Y, -dirAB.X, 0);

            var dirBC = (dirBCraw.DotProduct(perpCCW) >= dirBCraw.DotProduct(perpCW))
                ? perpCCW
                : perpCW;

            // ── 4. Kích thước mới cho cạnh AB và BC ──
            //   - Mẫu AB dài → AB mới = W (dài), BC mới = H (ngắn)
            //   - Mẫu AB ngắn → AB mới = H (ngắn), BC mới = W (dài)
            double newABLen = sampleABIsLong ? refW : refH;
            double newBCLen = sampleABIsLong ? refH : refW;

            double hw = newABLen / 2.0;
            double hh = newBCLen / 2.0;

            // ── 5. Tính 4 đỉnh mới (KHÔNG đảo thứ tự) ──
            var n0 = cen - dirAB * hw - dirBC * hh;
            var n1 = cen + dirAB * hw - dirBC * hh;
            var n2 = cen + dirAB * hw + dirBC * hh;
            var n3 = cen - dirAB * hw + dirBC * hh;

            // ── 6. Ghi đỉnh vào polyline ──
            pl.SetPointAt(0, new Point2d(n0.X, n0.Y));
            pl.SetPointAt(1, new Point2d(n1.X, n1.Y));
            pl.SetPointAt(2, new Point2d(n2.X, n2.Y));
            pl.SetPointAt(3, new Point2d(n3.X, n3.Y));

            // ── 7. ★ Reset bulge = 0 cho 4 cạnh (tránh cạnh cong) ──
            for (int i = 0; i < 4; i++)
                pl.SetBulgeAt(i, 0);

            pl.Closed = true;
            pl.RecordGraphicsModified(true);
        }

        // ═══════════════════════════════════════════════════════════
        //  Lấy 4 điểm polyline
        // ═══════════════════════════════════════════════════════════
        private static List<Point3d> GetPolylinePoints(Polyline pl)
        {
            var pts = new List<Point3d>(4);
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                var p2d = pl.GetPoint2dAt(i);
                pts.Add(new Point3d(p2d.X, p2d.Y, 0.0));
            }
            return pts;
        }

        // ═══════════════════════════════════════════════════════════
        //  Kiểm tra hình chữ nhật
        // ═══════════════════════════════════════════════════════════
        private static bool IsRect(List<Point3d> pts, double tol)
        {
            if (pts.Count != 4) return false;

            var v1 = pts[1] - pts[0];
            var v2 = pts[2] - pts[1];
            var v3 = pts[3] - pts[2];
            var v4 = pts[0] - pts[3];

            double l1 = v1.Length, l2 = v2.Length, l3 = v3.Length, l4 = v4.Length;
            if (l1 < tol || l2 < tol || l3 < tol || l4 < tol) return false;

            if (Math.Abs(l1 - l3) > tol * l1) return false;
            if (Math.Abs(l2 - l4) > tol * l2) return false;

            double dot1 = v1.X * v2.X + v1.Y * v2.Y;
            double dot2 = v2.X * v3.X + v2.Y * v3.Y;
            double dot3 = v3.X * v4.X + v3.Y * v4.Y;
            double dot4 = v4.X * v1.X + v4.Y * v1.Y;

            if (Math.Abs(dot1) > tol * l1 * l2) return false;
            if (Math.Abs(dot2) > tol * l2 * l3) return false;
            if (Math.Abs(dot3) > tol * l3 * l4) return false;
            if (Math.Abs(dot4) > tol * l4 * l1) return false;

            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  Tính tâm 4 điểm
        // ═══════════════════════════════════════════════════════════
        private static Point3d Centroid(List<Point3d> pts)
        {
            double sx = 0, sy = 0, sz = 0;
            foreach (var p in pts) { sx += p.X; sy += p.Y; sz += p.Z; }
            int n = pts.Count;
            return new Point3d(sx / n, sy / n, sz / n);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM
    // ═══════════════════════════════════════════════════════════
    public class EqualizeForm : Form
    {
        private RadioButton rbCircle;
        private RadioButton rbArc;
        private RadioButton rbRect;
        private Button btnOK;
        private Button btnCancel;

        public EqualizeFunction SelectedFunction { get; private set; } = EqualizeFunction.Circle;

        public EqualizeForm(EqualizeFunction preFunc)
        {
            this.Text = "Đồng nhất hình học";
            this.ClientSize = new Size(410, 235);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            Label lblTitle = new Label
            {
                Text = "Chọn chức năng đồng nhất:",
                Left = 20,
                Top = 15,
                Width = 370,
                Font = new WinFont(this.Font, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            Panel pnlFunc = new Panel
            {
                Left = 20,
                Top = 45,
                Width = 370,
                Height = 110
            };

            rbCircle = new RadioButton
            {
                Text = "1. Đồng nhất Đường tròn (CIRCLE) — cùng bán kính",
                Left = 5,
                Top = 8,
                Width = 360,
                Height = 24
            };
            rbArc = new RadioButton
            {
                Text = "2. Đồng nhất Cung tròn (ARC) — cùng R + góc",
                Left = 5,
                Top = 42,
                Width = 360,
                Height = 24
            };
            rbRect = new RadioButton
            {
                Text = "3. Đồng nhất Hình chữ nhật (RECT) — cùng W × H",
                Left = 5,
                Top = 76,
                Width = 360,
                Height = 24
            };

            pnlFunc.Controls.AddRange(new Control[] { rbCircle, rbArc, rbRect });

            switch (preFunc)
            {
                case EqualizeFunction.Arc: rbArc.Checked = true; break;
                case EqualizeFunction.Rect: rbRect.Checked = true; break;
                default: rbCircle.Checked = true; break;
            }

            Label lblHint = new Label
            {
                Text = "💡 Sau khi chọn chức năng → chọn đối tượng trên CAD → chọn mẫu/nhập.",
                Left = 20,
                Top = 160,
                Width = 370,
                Height = 30,
                ForeColor = Color.DimGray,
                Font = new WinFont(this.Font, FontStyle.Italic)
            };

            btnOK = new Button
            {
                Text = "OK",
                Left = 215,
                Top = 195,
                Width = 85,
                Height = 28
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 310,
                Top = 195,
                Width = 85,
                Height = 28
            };

            btnOK.Click += delegate
            {
                if (rbCircle.Checked) SelectedFunction = EqualizeFunction.Circle;
                else if (rbArc.Checked) SelectedFunction = EqualizeFunction.Arc;
                else if (rbRect.Checked) SelectedFunction = EqualizeFunction.Rect;
                this.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
            };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, pnlFunc, lblHint, btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}