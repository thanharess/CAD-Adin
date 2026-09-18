using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class LineTypeToLayer
    {
        private static string _lastLayer = null;

        [CommandMethod("LAYERCHANGEAM")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // ── Cache linetype của tất cả layer ──
            var layerLTCache = BuildLayerLinetypeCache(db);

            // ── Bước 1: Chọn object mẫu ──
            var peo = new PromptEntityOptions("\nChọn đối tượng mẫu (lấy Linetype): ");
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            string sampleLT;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = (Entity)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                sampleLT = GetRealLinetype(ent, layerLTCache);
                tr.Commit();
            }

            if (string.IsNullOrEmpty(sampleLT))
            {
                Utils.Print("✖ Không xác định được Linetype của đối tượng mẫu.");
                return;
            }
            Utils.Print($"Linetype mẫu: {sampleLT}");

            // ── Bước 2: Quét chọn vùng ──
            ed.WriteMessage("\nQuét chọn vùng (Window / Crossing / Fence / CP...)");
            var sel = ed.GetSelection();
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("✖ Không chọn đối tượng nào.");
                return;
            }

            // ── Bước 3: Chọn layer đích ──
            string targetLayer = GetLayerByNumber(ed, db);
            if (string.IsNullOrEmpty(targetLayer))
            {
                Utils.Print("✖ Chưa chọn layer đích.");
                return;
            }

            // ── Bước 4: Đổi layer ──
            int changed = 0, skipped = 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    if (string.Equals(GetRealLinetype(ent, layerLTCache),
                                      sampleLT, StringComparison.OrdinalIgnoreCase))
                    {
                        ent.UpgradeOpen();
                        ent.Layer = targetLayer;
                        changed++;
                    }
                    else skipped++;
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"✔ Hoàn thành – Layer hiện tại: {targetLayer}");
            Utils.Print($"   • Đã đổi: {changed} đối tượng");
            Utils.Print($"   • Bỏ qua: {skipped} đối tượng");
        }

        private static Dictionary<string, string> BuildLayerLinetypeCache(Database db)
        {
            var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var lay = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    string ltName = lay.LinetypeObjectId.IsValid
                        ? ((LinetypeTableRecord)tr.GetObject(lay.LinetypeObjectId, OpenMode.ForRead)).Name
                        : "Continuous";
                    cache[lay.Name] = ltName;
                }
                tr.Commit();
            }
            return cache;
        }

        private static string GetRealLinetype(Entity ent, Dictionary<string, string> layerLTCache)
        {
            string lt = ent.Linetype;
            if (string.IsNullOrEmpty(lt) ||
                string.Equals(lt, "BYLAYER", StringComparison.OrdinalIgnoreCase))
            {
                return layerLTCache.TryGetValue(ent.Layer, out string cached)
                    ? cached : "Continuous";
            }
            return lt;
        }

        private static string GetLayerByNumber(Editor ed, Database db)
        {
            var layers = new List<string>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var lay = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    layers.Add(lay.Name);
                }
                tr.Commit();
            }

            layers.Sort(StringComparer.OrdinalIgnoreCase);
            if (layers.Count == 0)
            {
                Utils.Print("✖ Bản vẽ chưa có layer nào.");
                return null;
            }

            ed.WriteMessage("\n================ LAYER LIST ================");
            for (int i = 0; i < layers.Count; i++)
                ed.WriteMessage($"\n[{i}]  {layers[i]}");
            ed.WriteMessage("\n============================================");

            if (!string.IsNullOrEmpty(_lastLayer))
                ed.WriteMessage($"\n<Enter = dùng lại layer: {_lastLayer}>");

            var pio = new PromptIntegerOptions("\nNhập SỐ Layer đích: ")
            {
                LowerLimit = 0,
                UpperLimit = layers.Count - 1,
                AllowNone = true
            };
            var pir = ed.GetInteger(pio);

            if (pir.Status != PromptStatus.OK)
            {
                if (!string.IsNullOrEmpty(_lastLayer))
                {
                    Utils.Print($"↩ Dùng lại layer: {_lastLayer}");
                    return _lastLayer;
                }
                Utils.Print("❌ Chưa có layer nào được chọn trước đó.");
                return null;
            }

            string chosen = layers[pir.Value];
            _lastLayer = chosen;
            return chosen;
        }
    }
}