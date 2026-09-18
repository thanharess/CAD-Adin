using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class ScaleLine
    {
        [CommandMethod("ScaleLine")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            ed.WriteMessage("\nLệnh SCALELINE - Scale Linetype Scale.");

            // ── Bước 1: Chọn đối tượng (cho phép cả locked layers) ──
            ed.WriteMessage("\nChọn LINE/POLYLINE/ARC/CIRCLE/ELLIPSE/SPLINE hoặc BLOCK cần scale:");
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start,
                    "LINE,LWPOLYLINE,POLYLINE,ARC,CIRCLE,ELLIPSE,SPLINE,INSERT")
            });
            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có đối tượng nào được chọn.");
                return;
            }

            // ── Bước 2: Chọn mode ──
            var kwo = new PromptKeywordOptions("\nChọn chế độ [1:Tăng / 2:Giảm / 3:1:1] <1>: ");
            kwo.Keywords.Add("1");
            kwo.Keywords.Add("2");
            kwo.Keywords.Add("3");
            kwo.AllowNone = true;

            var kwr = ed.GetKeywords(kwo);
            string mode = (kwr.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr.StringResult))
                ? kwr.StringResult : "1";

            // ── Bước 3: Nhập hệ số ──
            double factor = 1.0;
            if (mode == "1")
            {
                var pr = ed.GetDouble("\nNhập hệ số tăng (VD: 2 = nhân 2:1): ");
                factor = (pr.Status == PromptStatus.OK) ? pr.Value : 1.0;
            }
            else if (mode == "2")
            {
                var pr = ed.GetDouble("\nNhập hệ số giảm (VD: 5 = chia 1:5): ");
                factor = (pr.Status == PromptStatus.OK && pr.Value != 0)
                    ? 1.0 / pr.Value : 1.0;
            }
            // mode 3 → factor = 1.0 (không dùng)

            int count = 0;

            // ── Bước 4: Duyệt và scale ──
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var processedBlocks = new HashSet<string>();   // tránh xử lý 1 block def nhiều lần

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // ── Trường hợp BLOCK REFERENCE ──
                    if (ent is BlockReference br)
                    {
                        // 4.1: Scale LinetypeScale của block reference
                        br.UpgradeOpen();
                        ApplyFactorToEntity(br, mode, factor);
                        count++;

                        // 4.2: Scale entities trong block definition
                        string effName = br.Name;   // EffectiveName trong .NET là .Name
                        if (!string.IsNullOrEmpty(effName) &&
                            !processedBlocks.Contains(effName) &&
                            bt.Has(effName))
                        {
                            processedBlocks.Add(effName);
                            var btr = (BlockTableRecord)tr.GetObject(bt[effName], OpenMode.ForRead);

                            foreach (ObjectId entId in btr)
                            {
                                var sub = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                                if (sub == null) continue;

                                sub.UpgradeOpen();
                                ApplyFactorToEntity(sub, mode, factor);
                                count++;
                            }
                        }
                    }
                    // ── Trường hợp entity khác (LINE, POLYLINE, ARC, ...) ──
                    else
                    {
                        ent.UpgradeOpen();
                        ApplyFactorToEntity(ent, mode, factor);
                        count++;
                    }
                }

                tr.Commit();
            }

            // ── Bước 5: Regen để cập nhật hiển thị ──
            ed.Regen();

            // ── Bước 6: Thông báo kết quả ──
            if (mode == "3")
                Utils.Print($"Đã đặt {count} đối tượng về Linetype Scale 1.0");
            else
                Utils.Print($"Đã scale {count} đối tượng, hệ số nhân: {factor:F4}");
        }

        // ═══════════════════════════════════════════════════════════
        // ÁP DỤNG HỆ SỐ LÊN LinetypeScale
        // ═══════════════════════════════════════════════════════════
        private static void ApplyFactorToEntity(Entity ent, string mode, double factor)
        {
            try
            {
                if (mode == "3")
                {
                    ent.LinetypeScale = 1.0;
                }
                else
                {
                    double old = ent.LinetypeScale;
                    if (old <= 0) old = 1.0;   // tránh nhân với 0
                    ent.LinetypeScale = old * factor;
                }
            }
            catch
            {
                // Bỏ qua nếu entity không hỗ trợ LinetypeScale
            }
        }
    }
}