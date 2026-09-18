using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class LayerChangeReplateLine
    {
        [CommandMethod("LAYERCHANGEREPLATELINE")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // ═══════════════════════════════════════════════════════
            // BƯỚC 1: Liệt kê danh sách layer theo STT gốc
            // ═══════════════════════════════════════════════════════
            var layList = new List<string>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                int i = 1;
                ed.WriteMessage("\nDanh sách layer:");
                foreach (ObjectId id in lt)
                {
                    var lay = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    layList.Add(lay.Name);
                    ed.WriteMessage($"\n {i++}. {lay.Name}");
                }
                tr.Commit();
            }

            if (layList.Count == 0)
            {
                Utils.Print("✖ Không có layer nào.");
                return;
            }

            // ═══════════════════════════════════════════════════════
            // BƯỚC 2: Nhập STT layer NGUỒN
            // ═══════════════════════════════════════════════════════
            var pioSrc = new PromptIntegerOptions("\nNhập STT layer NGUỒN: ")
            { LowerLimit = 1, UpperLimit = layList.Count };
            var pirSrc = ed.GetInteger(pioSrc);
            if (pirSrc.Status != PromptStatus.OK)
            {
                Utils.Print("✖ STT layer nguồn không hợp lệ.");
                return;
            }
            string srcLayer = layList[pirSrc.Value - 1];

            // ═══════════════════════════════════════════════════════
            // BƯỚC 3: Nhập STT layer ĐÍCH
            // ═══════════════════════════════════════════════════════
            var pioDst = new PromptIntegerOptions("\nNhập STT layer ĐÍCH: ")
            { LowerLimit = 1, UpperLimit = layList.Count };
            var pirDst = ed.GetInteger(pioDst);
            if (pirDst.Status != PromptStatus.OK)
            {
                Utils.Print("✖ STT layer đích không hợp lệ.");
                return;
            }
            string dstLayer = layList[pirDst.Value - 1];

            // ═══════════════════════════════════════════════════════
            // BƯỚC 4: Đổi layer cho tất cả entity trên layer nguồn
            // ═══════════════════════════════════════════════════════
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Kiểm tra layer đích có tồn tại
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(dstLayer))
                {
                    Utils.Print($"✖ Layer đích '{dstLayer}' không tồn tại.");
                    return;
                }

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                int count = 0;

                // Duyệt qua tất cả block (ModelSpace + PaperSpace + Block Definitions)
                foreach (ObjectId btrId in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                    if (btr.IsLayout == false && btr.Name.StartsWith("*"))
                        continue;   // bỏ qua block hệ thống

                    foreach (ObjectId entId in btr)
                    {
                        var ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        // Chỉ xử lý LINE, LWPOLYLINE, POLYLINE, ARC, CIRCLE
                        string typeName = ent.GetType().Name;
                        bool isTargetType = typeName == "Line"
                            || typeName == "Polyline"
                            || typeName == "Polyline2d"
                            || typeName == "Polyline3d"
                            || typeName == "Arc"
                            || typeName == "Circle";
                        if (!isTargetType) continue;

                        // Chỉ xử lý entity thuộc layer nguồn
                        if (ent.Layer != srcLayer) continue;

                        ent.UpgradeOpen();
                        ent.Layer = dstLayer;
                        count++;
                    }
                }

                tr.Commit();

                if (count == 0)
                {
                    Utils.Print($"✖ Không có LINE nào trên layer '{srcLayer}'.");
                    return;
                }

                ed.Regen();
                Utils.Print($"✔ Đã chuyển {count} entity từ layer '{srcLayer}' sang '{dstLayer}'.");
            }
        }
    }
}