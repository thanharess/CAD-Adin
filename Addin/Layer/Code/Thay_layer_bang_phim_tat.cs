using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class QuickShortcuts
    {
        // ═══════════════════════════════════════════════════════════
        // NHÓM PHÍM TẮT ĐỔI LAYER HIỆN HÀNH (A1 - A6)
        // ═══════════════════════════════════════════════════════════
        [CommandMethod("A1")]
        public void SetLayerA1() => SetCurrentLayer("AM 0");

        [CommandMethod("A2")]
        public void SetLayerA2() => SetCurrentLayer("AM 3");

        [CommandMethod("A3")]
        public void SetLayerA3() => SetCurrentLayer("AM 7");

        [CommandMethod("A4")]
        public void SetLayerA4() => SetCurrentLayer("AM 8");

        [CommandMethod("A5")]
        public void SetLayerA5() => SetCurrentLayer("AM 5");

        [CommandMethod("A6")]
        public void SetLayerA6() => SetCurrentLayer("AM 6");

        /// <summary>
        /// Đặt layer hiện hành = layerName.
        /// Nếu layer chưa tồn tại → tạo mới (để tránh lỗi).
        /// </summary>
        private static void SetCurrentLayer(string layerName)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                if (!lt.Has(layerName))
                {
                    // Tự tạo layer nếu chưa có (giống hành vi thân thiện)
                    lt.UpgradeOpen();
                    var newLay = new LayerTableRecord { Name = layerName };
                    lt.Add(newLay);
                    tr.AddNewlyCreatedDBObject(newLay, true);
                }

                tr.Commit();
            }

            // Đặt layer hiện hành
            AcApp.SetSystemVariable("CLAYER", layerName);
        }

        // ═══════════════════════════════════════════════════════════
        // NHÓM PHÍM TẮT TẠO CIRCLE
        // ═══════════════════════════════════════════════════════════
        /// <summary>C2 — Tạo CIRCLE bằng 2 điểm (đường kính).</summary>
        [CommandMethod("C2")]
        public void Circle2Points()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("_.CIRCLE _2P ", true, false, false);
        }

        /// <summary>C3 — Tạo CIRCLE bằng đường kính (Diameter).</summary>
        [CommandMethod("C3")]
        public void CircleDiameter()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("_.CIRCLE _D ", true, false, false);
        }

        // ═══════════════════════════════════════════════════════════
        // PHÍM TẮT AMPOWERDIM (P)
        // ═══════════════════════════════════════════════════════════
        /// <summary>P — Gọi lệnh AMPOWERDIM (AutoCAD Mechanical).</summary>
        [CommandMethod("P")]
        public void PowerDim()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("_.ampowerdim ", true, false, false);
        }
    }
}