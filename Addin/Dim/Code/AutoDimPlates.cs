using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Dimtools
{
    public class AutoDimPlates
    {
        [CommandMethod("DIMAUTOPLATES")]
        public void AutoDim()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // Chỉ chọn LWPOLYLINE (không trên layer khóa)
            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "LWPOLYLINE") });
            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không chọn được polyline.");
                return;
            }

            int totalDims = 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Lấy layer DIM hiện hành để tạo dim trên đó
                string curDimLayer = (string)AcApp.GetSystemVariable("CLAYER");

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var pl = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Polyline;
                    if (pl == null) continue;

                    // Lấy danh sách điểm vertex
                    var pts = new List<Point3d>();
                    for (int i = 0; i < pl.NumberOfVertices; i++)
                        pts.Add(pl.GetPoint3dAt(i));

                    if (pts.Count < 2) continue;

                    // DIM từng cạnh
                    for (int i = 0; i < pts.Count - 1; i++)
                    {
                        CreateLinearDim(ed, db, pts[i], pts[i + 1], tr);
                        totalDims++;
                    }

                    // Nếu polyline đóng → dim cạnh cuối về đầu
                    if (pl.Closed && pts.Count >= 3)
                    {
                        CreateLinearDim(ed, db, pts[pts.Count - 1], pts[0], tr);
                        totalDims++;
                    }
                }

                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"✓ Đã tạo {totalDims} DIM cho các cạnh polyline.");
        }

        /// <summary>
        /// Tạo 1 DIMLINEAR cho 2 điểm, cho người dùng pick vị trí dim line.
        /// </summary>
        private static void CreateLinearDim(Editor ed, Database db, Point3d p1, Point3d p2, Transaction tr)
        {
            // Hỏi vị trí đặt đường dim
            var ppo = new PromptPointOptions($"\nChọn vị trí đường dim cho cạnh ({p1.X:F1},{p1.Y:F1})-({p2.X:F1},{p2.Y:F1}): ");
            var ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            var dimLinePt = ppr.Value;

            // Tạo RotatedDimension (tương đương DIMLINEAR)
            var rotDim = new RotatedDimension
            {
                XLine1Point = p1,
                XLine2Point = p2,
                DimLinePoint = dimLinePt,
                Rotation = 0.0,
                DimensionStyle = db.Dimstyle
            };

            var ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
            ms.AppendEntity(rotDim);
            tr.AddNewlyCreatedDBObject(rotDim, true);
        }
    }
}