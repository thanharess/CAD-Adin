using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Layer
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
        /// Đặt layer hiện hành theo tên có dạng "AM x".
        /// Tự động xử lý 2 biến thể:
        ///   - Có dấu cách:  "AM 0"
        ///   - Không dấu cách: "AM0"
        ///
        /// Quy tắc chọn:
        ///   • Cả 2 tồn tại  → ưu tiên bản KHÔNG dấu cách ("AM0").
        ///   • Chỉ 1 tồn tại → dùng bản đang có.
        ///   • Không có cái nào → tạo mới bản KHÔNG dấu cách ("AM0").
        /// </summary>
        private static void SetCurrentLayer(string layerNameWithSpace)
        {
            // Sinh tên không dấu cách từ tên có dấu cách: "AM 0" -> "AM0"
            string layerNameNoSpace = layerNameWithSpace.Replace(" ", "");

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            string targetLayer;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                bool hasWithSpace = lt.Has(layerNameWithSpace);
                bool hasNoSpace = lt.Has(layerNameNoSpace);

                if (hasNoSpace)
                {
                    // Ưu tiên bản không dấu cách (kể cả khi cả 2 đều có)
                    targetLayer = layerNameNoSpace;
                }
                else if (hasWithSpace)
                {
                    // Chỉ có bản có dấu cách
                    targetLayer = layerNameWithSpace;
                }
                else
                {
                    // Không có cả 2 → tạo mới bản không dấu cách
                    lt.UpgradeOpen();
                    var newLay = new LayerTableRecord { Name = layerNameNoSpace };
                    lt.Add(newLay);
                    tr.AddNewlyCreatedDBObject(newLay, true);
                    targetLayer = layerNameNoSpace;
                }

                tr.Commit();
            }

            // Đặt layer hiện hành
            AcApp.SetSystemVariable("CLAYER", targetLayer);
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