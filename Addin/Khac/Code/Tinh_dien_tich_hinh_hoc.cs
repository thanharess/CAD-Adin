using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;



namespace AreaTools
{
    public class AreaLabel
    {
        // ===== Cài đặt =====
        private static string Heading = "Area Table";
        private static string ColNumber = "Number";
        private static string ColArea = "Area";
        private static string NumberPrefix = "";
        private static string NumberSuffix = "";
        private static string AreaPrefix = "";
        private static string AreaSuffix = " m²";
        private static double ConversionFactor = 1e-6;   // mm² → m²
        private static int StartNumber = 1;

        // =====================================================
        // Lệnh AT - Areas to Table
        // =====================================================
        [CommandMethod("AT")]
        public void AreasToTable()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Hỏi số bắt đầu
            PromptIntegerOptions pio = new PromptIntegerOptions($"\nSpecify Starting Number <{StartNumber}>: ");
            pio.AllowNone = true;
            pio.AllowNegative = false;
            pio.AllowZero = false;

            PromptIntegerResult pir = ed.GetInteger(pio);
            if (pir.Status == PromptStatus.OK)
                StartNumber = pir.Value;
            else if (pir.Status != PromptStatus.None)
                return;

            int currentNum = StartNumber;

            // Chọn điểm đặt bảng
            PromptPointOptions ppo = new PromptPointOptions("\nPick Point for Table: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d tablePoint = ppr.Value;

            using (DocumentLock docLock = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                // Tạo bảng
                Table table = new Table();
                table.TableStyle = db.Tablestyle;
                table.SetSize(2, 2);              // header + 1 row tiêu đề
                table.Rows[0].Height = 8;         // ← SỬA
                table.Rows[1].Height = 6;         // ← SỬA
                table.Columns[0].Width = 25;      // ← SỬA
                table.Columns[1].Width = 35;      // ← SỬA

                table.Position = tablePoint;

                // Tiêu đề
                table.Cells[0, 0].TextString = Heading;
                table.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;
                table.MergeCells(CellRange.Create(table, 0, 0, 0, 1));

                // Header cột
                table.Cells[1, 0].TextString = ColNumber;
                table.Cells[1, 1].TextString = ColArea;
                table.Cells[1, 0].Alignment = CellAlignment.MiddleCenter;
                table.Cells[1, 1].Alignment = CellAlignment.MiddleCenter;

                btr.AppendEntity(table);
                tr.AddNewlyCreatedDBObject(table, true);

                // ===== Vòng lặp chọn đối tượng =====
                while (true)
                {
                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect closed object (or press Enter to finish): ");
                    peo.SetRejectMessage("\nChỉ chọn đối tượng có Area.");
                    peo.AddAllowedClass(typeof(Polyline), true);
                    peo.AddAllowedClass(typeof(Polyline2d), true);
                    peo.AddAllowedClass(typeof(Circle), true);
                    peo.AddAllowedClass(typeof(Ellipse), true);
                    peo.AddAllowedClass(typeof(Region), true);
                    peo.AddAllowedClass(typeof(Spline), true);

                    PromptEntityResult per = ed.GetEntity(peo);

                    if (per.Status != PromptStatus.OK)
                        break;

                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    double area = 0;
                    Point3d centroid = Point3d.Origin;

                    try
                    {
                        // Lấy Area
                        if (ent is Curve curve && curve.Closed)
                        {
                            area = curve.Area;
                            // Tính centroid đơn giản (dùng BoundingBox center tạm)
                            Extents3d ext = ent.GeometricExtents;
                            centroid = new Point3d(
                                (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                                (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                                0);
                        }
                        else if (ent is Region region)
                        {
                            area = region.Area;
                            Extents3d ext = ent.GeometricExtents;
                            centroid = new Point3d(
                                (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                                (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                                0);
                        }
                        else
                        {
                            ed.WriteMessage("\nĐối tượng không có Area hợp lệ.");
                            continue;
                        }
                    }
                    catch
                    {
                        ed.WriteMessage("\nKhông lấy được Area.");
                        continue;
                    }

                    // Thêm dòng vào bảng
                    table.InsertRows(table.Rows.Count, 6, 1);
                    int row = table.Rows.Count - 1;

                    string numText = $"{NumberPrefix}{currentNum}{NumberSuffix}";
                    string areaText = $"{AreaPrefix}{(area * ConversionFactor):F2}{AreaSuffix}";

                    table.Cells[row, 0].TextString = numText;
                    table.Cells[row, 1].TextString = areaText;
                    table.Cells[row, 0].Alignment = CellAlignment.MiddleCenter;
                    table.Cells[row, 1].Alignment = CellAlignment.MiddleCenter;

                    // Tạo text số ngay tại centroid
                    DBText label = new DBText();
                    label.Position = centroid;
                    label.Height = 2.5;
                    label.TextString = numText;
                    label.HorizontalMode = TextHorizontalMode.TextCenter;
                    label.VerticalMode = TextVerticalMode.TextVerticalMid;
                    label.AlignmentPoint = centroid;

                    btr.AppendEntity(label);
                    tr.AddNewlyCreatedDBObject(label, true);

                    currentNum++;
                    ed.WriteMessage($"\nĐã thêm Area: {areaText}");
                }

                // Cập nhật số bắt đầu cho lần sau
                StartNumber = currentNum;

                tr.Commit();
            }

            ed.WriteMessage("\n✔ Hoàn thành Area Table.");
        }
    }
}