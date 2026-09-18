using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class ScaleDimValueBlock
    {
        [CommandMethod("SCALEDIMVALUEBLOCK")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            ed.WriteMessage("\nLệnh SCALEDIMVALUEBLOCK - Scale Dim Linear factor.");

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "DIMENSION,INSERT") });
            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có đối tượng nào được chọn.");
                return;
            }

            // ── Chọn mode ──
            var kwo = new PromptKeywordOptions("\nChọn chế độ [1:Tăng / 2:Giảm / 3:1:1] <1>: ");
            kwo.Keywords.Add("1"); kwo.Keywords.Add("2"); kwo.Keywords.Add("3");
            kwo.AllowNone = true;
            var kwr = ed.GetKeywords(kwo);
            string mode = (kwr.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr.StringResult))
                ? kwr.StringResult : "1";

            // ── Nhập hệ số ──
            double factor = 1.0;
            if (mode == "1")
            {
                var pr = ed.GetDouble("\nNhập hệ số tăng (VD: 2 = nhân 2:1): ");
                factor = (pr.Status == PromptStatus.OK) ? pr.Value : 1.0;
            }
            else if (mode == "2")
            {
                var pr = ed.GetDouble("\nNhập hệ số giảm (VD: 5 = chia 1:5): ");
                factor = (pr.Status == PromptStatus.OK && pr.Value != 0) ? 1.0 / pr.Value : 1.0;
            }
            // mode 3 → factor = 1.0

            int count = 0;
            var updatedBlocks = new HashSet<string>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // ── Trường hợp DIMENSION ──
                    if (ent is Dimension dim)
                    {
                        dim.UpgradeOpen();
                        ApplyFactor(dim, mode, factor);
                        count++;
                    }
                    // ── Trường hợp BLOCK REFERENCE ──
                    else if (ent is BlockReference br)
                    {
                        string blkName = br.Name;
                        if (!updatedBlocks.Contains(blkName) && bt.Has(blkName))
                        {
                            updatedBlocks.Add(blkName);
                            var btr = (BlockTableRecord)tr.GetObject(bt[blkName], OpenMode.ForRead);

                            bool modified = false;
                            foreach (ObjectId id in btr)
                            {
                                var sub = tr.GetObject(id, OpenMode.ForRead) as Entity;
                                if (sub is Dimension subDim)
                                {
                                    subDim.UpgradeOpen();
                                    ApplyFactor(subDim, mode, factor);
                                    count++;
                                    modified = true;
                                }
                            }

                            // Update reference để hiển thị lại
                            if (modified)
                            {
                                br.UpgradeOpen();
                                br.RecordGraphicsModified(true);
                            }
                        }
                    }
                }

                tr.Commit();
            }

            // Regen nếu có block được cập nhật
            if (updatedBlocks.Count > 0)
                ed.Regen();

            if (mode == "3")
                Utils.Print($"Đã đặt {count} DIM về giá trị 1.0");
            else
                Utils.Print($"Đã scale {count} DIM, hệ số nhân: {factor:F4}");
        }

        private static void ApplyFactor(Dimension dim, string mode, double factor)
        {
            if (mode == "3")
            {
                dim.Dimlfac = 1.0;
            }
            else
            {
                double old = dim.Dimlfac;
                if (old == 0) old = 1.0;   // tránh nhân với 0
                dim.Dimlfac = old * factor;
            }
        }
    }
}