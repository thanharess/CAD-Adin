using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class XoaChuTrongText
    {
        [CommandMethod("XoaChuTrongText")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "TEXT,MTEXT,MULTILEADER") });
            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có đối tượng nào được chọn.");
                return;
            }

            ed.WriteMessage("\n=== Nhập CHỮ / CỤM TỪ cần XÓA (nhiều dòng) ===");
            ed.WriteMessage("\n- Gõ từng cụm từ cần xóa rồi Enter");
            ed.WriteMessage("\n- Nhập dòng trống (chỉ Enter) để KẾT THÚC\n");

            var removeList = new System.Collections.Generic.List<string>();
            while (true)
            {
                var pso = new PromptStringOptions("\nCụm từ cần xóa: ") { AllowSpaces = true };
                var psr = ed.GetString(pso);
                if (psr.Status != PromptStatus.OK) break;
                string line = psr.StringResult;
                if (string.IsNullOrEmpty(line)) break;
                removeList.Add(line);
            }

            Utils.Print("Đã kết thúc nhập nội dung cần xóa.");
            if (removeList.Count == 0)
            {
                Utils.Print("Không có nội dung cần xóa.");
                return;
            }

            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // ── Lấy văn bản gốc ──
                    string oldTxt = null;
                    if (ent is DBText t) oldTxt = t.TextString;
                    else if (ent is MText m) oldTxt = m.Contents;
                    else if (ent is MLeader ml)
                    {
                        // ✅ Sửa: dùng MText.Contents thay vì TextString
                        if (ml.ContentType == ContentType.MTextContent)
                        {
                            MText innerMt = ml.MText;
                            if (innerMt != null) oldTxt = innerMt.Contents;
                        }
                    }
                    if (string.IsNullOrEmpty(oldTxt)) continue;

                    // ── Xóa cụm từ (không phân biệt hoa thường) ──
                    string newTxt = oldTxt;
                    foreach (var r in removeList)
                    {
                        if (!string.IsNullOrEmpty(r))
                            newTxt = ReplaceIgnoreCase(newTxt, r, "");
                    }

                    // Dọn khoảng trắng thừa
                    while (newTxt.Contains("  "))
                        newTxt = newTxt.Replace("  ", " ");
                    newTxt = newTxt.Trim();

                    if (newTxt == oldTxt) continue;

                    // ── Ghi lại ──
                    if (ent is DBText txt)
                    {
                        // TEXT → chuyển thành MTEXT
                        var insPt = txt.Position;
                        double h = txt.Height;
                        string layer = txt.Layer;
                        ObjectId styleId = txt.TextStyleId;

                        if (!string.IsNullOrEmpty(newTxt))
                        {
                            var mt = new MText
                            {
                                Location = insPt,
                                TextHeight = h,
                                Layer = layer,
                                TextStyleId = styleId,
                                Contents = newTxt
                            };
                            ms.AppendEntity(mt);
                            tr.AddNewlyCreatedDBObject(mt, true);
                        }

                        txt.UpgradeOpen();
                        txt.Erase();
                        count++;
                    }
                    else if (ent is MText mt2)
                    {
                        mt2.UpgradeOpen();
                        mt2.Contents = newTxt;
                        count++;
                    }
                    else if (ent is MLeader ml2)
                    {
                        // ✅ Sửa: dùng MText.Contents thay vì TextString
                        if (ml2.ContentType == ContentType.MTextContent)
                        {
                            ml2.UpgradeOpen();
                            MText innerMt = ml2.MText;
                            if (innerMt != null)
                            {
                                innerMt.Contents = newTxt;
                                ml2.MText = innerMt;
                                count++;
                            }
                        }
                    }
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"Đã xử lý xóa cụm từ trong {count} đối tượng.");
        }

        // ═══════════════════════════════════════════════════════════
        // Replace không phân biệt hoa thường
        // ═══════════════════════════════════════════════════════════
        private static string ReplaceIgnoreCase(string str, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(oldValue)) return str;

            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < str.Length)
            {
                if (i + oldValue.Length <= str.Length &&
                    string.Compare(str, i, oldValue, 0, oldValue.Length,
                        StringComparison.OrdinalIgnoreCase) == 0)
                {
                    sb.Append(newValue);
                    i += oldValue.Length;
                }
                else
                {
                    sb.Append(str[i]);
                    i++;
                }
            }
            return sb.ToString();
        }
    }
}