using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class ChangeLayerCommand
    {
        [CommandMethod("CHANGELAYER")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // ── Bước 1: Liệt kê layer ──
            var layers = GetLayerNames(db);
            if (layers.Count == 0)
            {
                Utils.Print("❌ Không có layer nào.");
                return;
            }

            // ── Bước 2: Mở dialog ──
            ChangeLayerDialog dlg = null;

            // Vòng lặp — cho phép "Chọn Entity" rồi quay lại dialog
            string presetSource = null;
            while (true)
            {
                dlg = new ChangeLayerDialog(layers.ToArray(), presetSource);

                // Dùng ShowModalDialog của AutoCAD để tích hợp với CAD
                var result = AcApp.ShowModalDialog(dlg);

                if (result == DialogResult.Cancel || result == DialogResult.Abort)
                {
                    Utils.Print("Đã hủy lệnh.");
                    return;
                }

                // Nếu user bấm "Chọn Entity" → pick entity → quay lại dialog
                if (result == DialogResult.Retry)
                {
                    var peo = new PromptEntityOptions("\nChọn 1 đối tượng để lấy layer nguồn: ");
                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                    {
                        Utils.Print("Không chọn được đối tượng.");
                        presetSource = null;
                        continue;
                    }

                    string pickedLayer;
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var ent = (Entity)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                        pickedLayer = ent.Layer;
                        tr.Commit();
                    }

                    if (string.IsNullOrEmpty(pickedLayer))
                    {
                        Utils.Print("Không lấy được layer từ entity.");
                        presetSource = null;
                    }
                    else
                    {
                        Utils.Print($"Đã chọn layer nguồn: {pickedLayer}");
                        presetSource = pickedLayer;
                    }

                    continue;   // Mở lại dialog với layer đã pick
                }

                // OK → thoát loop, xử lý tiếp
                if (result == DialogResult.OK) break;
            }

            // ── Bước 3: Kiểm tra layer nguồn / đích ──
            string sourceLayer = dlg.SourceLayer;
            string targetLayer = dlg.TargetLayer;
            bool isAllMode = dlg.IsAllMode;

            if (string.IsNullOrEmpty(sourceLayer) || string.IsNullOrEmpty(targetLayer))
            {
                Utils.Print("❌ Chưa chọn layer nguồn hoặc đích.");
                return;
            }

            if (string.Equals(sourceLayer, targetLayer, StringComparison.OrdinalIgnoreCase))
            {
                Utils.Print("❌ Layer nguồn và đích giống nhau, không cần chuyển.");
                return;
            }

            // ── Bước 4: Thực thi ──
            if (isAllMode)
                ChangeAllEntities(ed, db, sourceLayer, targetLayer);
            else
                ChangeSelectedEntities(ed, db, sourceLayer, targetLayer);
        }

        // ═══════════════════════════════════════════════════════════
        // Lấy danh sách layer (sắp xếp A-Z)
        // ═══════════════════════════════════════════════════════════
        private static List<string> GetLayerNames(Database db)
        {
            var result = new List<string>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var lay = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    result.Add(lay.Name);
                }
                tr.Commit();
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        // ═══════════════════════════════════════════════════════════
        // Chuyển TẤT CẢ entity trên layer nguồn
        // ═══════════════════════════════════════════════════════════
        private static void ChangeAllEntities(Editor ed, Database db, string src, string tgt)
        {
            int count = 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                // Duyệt toàn bộ block table để bao gồm cả trong block definition
                foreach (ObjectId btrId in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                    // Snapshot id để tránh modify collection
                    var ids = new List<ObjectId>();
                    foreach (ObjectId entId in btr) ids.Add(entId);

                    foreach (ObjectId entId in ids)
                    {
                        var ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        if (string.Equals(ent.Layer, src, StringComparison.OrdinalIgnoreCase))
                        {
                            ent.UpgradeOpen();
                            ent.Layer = tgt;
                            count++;
                        }
                    }
                }
                tr.Commit();
            }

            ed.Regen();

            if (count > 0)
            {
                Utils.Print($"✅ Đã chuyển {count} entity từ layer '{src}' sang '{tgt}'.");
                AcApp.ShowAlertDialog($"Đã chuyển {count} entity từ layer {src} sang {tgt}.");
            }
            else
            {
                Utils.Print($"❌ Không có entity nào trên layer '{src}'.");
                AcApp.ShowAlertDialog($"Không có entity nào trên layer {src}.");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Chuyển entity được chọn trong vùng
        // ═══════════════════════════════════════════════════════════
        private static void ChangeSelectedEntities(Editor ed, Database db, string src, string tgt)
        {
            ed.WriteMessage($"\nChọn vùng entity trên layer '{src}' (Enter để kết thúc): ");

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.LayerName, src) });
            var sel = ed.GetSelection(filter);

            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("❌ Không có entity nào được chọn trên layer nguồn.");
                AcApp.ShowAlertDialog("Không có entity nào được chọn trên layer nguồn.");
                return;
            }

            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                    if (ent == null) continue;
                    ent.Layer = tgt;
                    count++;
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"✅ Đã chuyển {count} entity từ layer '{src}' sang '{tgt}'.");
            AcApp.ShowAlertDialog($"Đã chuyển {count} entity từ layer {src} sang {tgt} trong vùng chọn.");
        }
    }
}