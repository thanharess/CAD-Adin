using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CADAddin.Common;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Layer
{
    public class LayerToolCommands
    {
        private static string _pickedLayer = null;

        [CommandMethod("LAYERDELETE")]
        public void LayerTool()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<string> layerList = GetAllLayers(db);

            using (LayerToolForm form = new LayerToolForm(layerList, _pickedLayer))
            {
                DialogResult dr = AcadApp.ShowModalDialog(form);

                if (form.PickRequested)
                {
                    ed.WriteMessage("\nChọn 1 đối tượng để lấy layer: ");
                    PromptEntityResult per = ed.GetEntity("");
                    if (per.Status == PromptStatus.OK)
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent != null)
                            {
                                _pickedLayer = ent.Layer;
                                MessageBox.Show($"Đã chọn layer: {_pickedLayer}");
                            }
                            tr.Commit();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Không chọn được đối tượng.");
                    }
                    LayerTool();
                    return;
                }

                if (dr != DialogResult.OK)
                {
                    ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                string choice = form.SelectedOption;
                string layname = form.SelectedLayer;

                switch (choice)
                {
                    case "delete_select":
                        DeleteEntitiesByLayerSelect(ed, layname);
                        break;

                    case "delete_all":
                        DeleteEntitiesByLayer(ed, layname);
                        break;

                    case "purge_layers":
                        PurgeAllUnusedLayers(ed);
                        break;

                    case "delete_linetype_all":
                        DeleteByPickedLinetype(ed, db, true);
                        break;

                    case "delete_linetype_select":
                        DeleteByPickedLinetype(ed, db, false);
                        break;

                    default:
                        MessageBox.Show("Lựa chọn không hợp lệ.");
                        break;
                }
            }
        }

        // ==================== XÓA THEO LAYER ====================

        private void DeleteEntitiesByLayerSelect(Editor ed, string layname)
        {
            ed.WriteMessage($"\nChọn vùng các line trong model cùng layer {layname}: ");
            TypedValue[] filter = { new TypedValue((int)DxfCode.LayerName, layname) };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.GetSelection(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show("Không có line trong model cùng layer nào được chọn.");
                return;
            }

            int cnt = EraseEntities(ed.Document.Database, psr.Value.GetObjectIds());
            MessageBox.Show($"Đã xóa {cnt} line trên layer {layname} trong vùng chọn.");
        }

        private void DeleteEntitiesByLayer(Editor ed, string layname)
        {
            TypedValue[] filter = { new TypedValue((int)DxfCode.LayerName, layname) };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.SelectAll(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                MessageBox.Show($"Không có line nào trong model trùng layer {layname}.");
                return;
            }

            int cnt = EraseEntities(ed.Document.Database, psr.Value.GetObjectIds());
            MessageBox.Show($"Đã xóa {cnt} line trên layer {layname}.");
        }

        private void PurgeAllUnusedLayers(Editor ed)
        {
            ed.Command("_.PURGE", "_LA", "*", "N");
            MessageBox.Show("Đã purge tất cả layer không dùng.");
        }

        // ==================== XÓA THEO LINETYPE (PICK BẤT KỲ ĐỐI TƯỢNG) ====================

        private void DeleteByPickedLinetype(Editor ed, Database db, bool deleteAll)
        {
            // ===== Bước 1: Chọn đối tượng mẫu (không hỏi lại) =====
            PromptEntityOptions peo = new PromptEntityOptions(
                "\nChọn 1 đối tượng để lấy Linetype: ");
            peo.SetRejectMessage("\nĐối tượng không hợp lệ!");
            peo.AddAllowedClass(typeof(Line), true);
            peo.AddAllowedClass(typeof(Circle), true);
            peo.AddAllowedClass(typeof(Arc), true);
            peo.AddAllowedClass(typeof(Polyline), true);
            peo.AddAllowedClass(typeof(Polyline2d), true);
            peo.AddAllowedClass(typeof(Polyline3d), true);
            peo.AddAllowedClass(typeof(Spline), true);
            peo.AddAllowedClass(typeof(Ellipse), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            string targetLinetype;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                if (ent == null) return;
                targetLinetype = GetEffectiveLinetype(ent, tr);
                tr.Commit();
            }

            if (string.IsNullOrEmpty(targetLinetype)) return;

            // ===== Bước 2: Selection =====
            TypedValue[] filter = {
        new TypedValue((int)DxfCode.Operator, "<OR"),
        new TypedValue((int)DxfCode.Start, "LINE"),
        new TypedValue((int)DxfCode.Start, "CIRCLE"),
        new TypedValue((int)DxfCode.Start, "ARC"),
        new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
        new TypedValue((int)DxfCode.Start, "POLYLINE"),
        new TypedValue((int)DxfCode.Start, "SPLINE"),
        new TypedValue((int)DxfCode.Start, "ELLIPSE"),
        new TypedValue((int)DxfCode.Operator, "OR>")
    };
            SelectionFilter sf = new SelectionFilter(filter);

            PromptSelectionResult psr = deleteAll
                ? ed.SelectAll(sf)
                : ed.GetSelection(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0) return;

            // ===== Bước 3: Lọc + gom layer cần unlock =====
            List<ObjectId> toDelete = new List<ObjectId>();
            HashSet<string> lockedLayers = new HashSet<string>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    string effLt = GetEffectiveLinetype(ent, tr);
                    if (!effLt.Equals(targetLinetype, StringComparison.OrdinalIgnoreCase))
                        continue;

                    toDelete.Add(id);

                    if (lt.Has(ent.Layer))
                    {
                        LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(
                            lt[ent.Layer], OpenMode.ForRead);
                        if (ltr.IsLocked) lockedLayers.Add(ent.Layer);
                    }
                }
                tr.Commit();
            }

            if (toDelete.Count == 0)
            {
                MessageBox.Show($"Không tìm thấy đối tượng nào có Linetype = \"{targetLinetype}\".");
                return;
            }

            // ===== Bước 4: Unlock tạm → xóa → relock (không thông báo) =====
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                foreach (string lay in lockedLayers)
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(
                        lt[lay], OpenMode.ForWrite);
                    ltr.IsLocked = false;
                }

                int cnt = 0;
                foreach (ObjectId id in toDelete)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null) { ent.Erase(); cnt++; }
                }

                foreach (string lay in lockedLayers)
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(
                        lt[lay], OpenMode.ForWrite);
                    ltr.IsLocked = true;
                }

                tr.Commit();

                MessageBox.Show($"Đã xóa {cnt} đối tượng có Linetype = \"{targetLinetype}\".");
            }
        }

        // Lấy linetype hiệu quả (hỗ trợ ByLayer)
        private string GetEffectiveLinetype(Entity ent, Transaction tr)
        {
            string lt = ent.Linetype;

            if (string.IsNullOrEmpty(lt) || lt.Equals("ByLayer", StringComparison.OrdinalIgnoreCase))
            {
                Database db = ent.Database;
                LayerTable ltTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                if (ltTable.Has(ent.Layer))
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(ltTable[ent.Layer], OpenMode.ForRead);

                    if (ltr.LinetypeObjectId.IsValid && !ltr.LinetypeObjectId.IsNull)
                    {
                        LinetypeTableRecord ltRec = tr.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead) as LinetypeTableRecord;
                        lt = (ltRec != null) ? ltRec.Name : "Continuous";
                    }
                    else
                    {
                        lt = "Continuous";
                    }
                }
                else
                {
                    lt = "Continuous";
                }
            }

            return lt;
        }

        // ==================== HÀM HỖ TRỢ ====================

        private int EraseEntities(Database db, ObjectId[] ids)
        {
            int cnt = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null)
                    {
                        ent.Erase();
                        cnt++;
                    }
                }
                tr.Commit();
            }
            return cnt;
        }

        private List<string> GetAllLayers(Database db)
        {
            List<string> list = new List<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId id in lt)
                {
                    LayerTableRecord ltr = tr.GetObject(id, OpenMode.ForRead) as LayerTableRecord;
                    if (ltr.Name != "Defpoints")
                        list.Add(ltr.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x).ToList();
        }
    }

    // ==================== WINDOWS FORM ====================

    public class LayerToolForm : Form
    {
        private RadioButton rbDeleteSelect;
        private RadioButton rbDeleteAll;
        private RadioButton rbPurge;
        private RadioButton rbLtAll;
        private RadioButton rbLtSelect;

        private ComboBox cboLayer;
        private Button btnPick;
        private Button btnOK;
        private Button btnCancel;

        public bool PickRequested { get; private set; } = false;

        public string SelectedOption
        {
            get
            {
                if (rbDeleteSelect.Checked) return "delete_select";
                if (rbDeleteAll.Checked) return "delete_all";
                if (rbPurge.Checked) return "purge_layers";
                if (rbLtAll.Checked) return "delete_linetype_all";
                if (rbLtSelect.Checked) return "delete_linetype_select";
                return "delete_select";
            }
        }

        public string SelectedLayer => cboLayer.SelectedItem?.ToString() ?? "";

        public LayerToolForm(List<string> layers, string preSelectedLayer)
        {
            this.Text = "Layer Tool - Xóa theo Layer / Linetype";
            this.Width = 580;
            this.Height = 400;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Chọn chức năng:",
                Left = 20,
                Top = 15,
                Width = 520,
                Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
            };

            rbDeleteSelect = new RadioButton
            {
                Text = "1. Xóa line theo layer trong vùng chọn",
                Left = 20,
                Top = 45,
                Width = 520,
                Checked = true
            };

            rbDeleteAll = new RadioButton
            {
                Text = "2. Xóa tất cả line trên layer được chọn trong model",
                Left = 20,
                Top = 70,
                Width = 520
            };

            rbPurge = new RadioButton
            {
                Text = "3. Xóa tất cả layer không dùng trong model",
                Left = 20,
                Top = 95,
                Width = 520
            };

            rbLtAll = new RadioButton
            {
                Text = "4. Xóa tất cả đối tượng theo Linetype (chọn 1 đối tượng lấy mẫu trước) trong model",
                Left = 20,
                Top = 130,
                Width = 520
            };

            rbLtSelect = new RadioButton
            {
                Text = "5. Xóa đối tượng theo Linetype trong vùng chọn (chọn 1 đối tượng lấy mẫu trước)",
                Left = 20,
                Top = 155,
                Width = 520
            };

            Label lblLayer = new Label { Text = "Chọn Layer:", Left = 20, Top = 200, Width = 100 };
            cboLayer = new ComboBox
            {
                Left = 120,
                Top = 197,
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboLayer.Items.AddRange(layers.ToArray());

            btnPick = new Button { Text = "Chọn line", Left = 415, Top = 195, Width = 110 };
            btnPick.Click += (s, e) =>
            {
                PickRequested = true;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            if (!string.IsNullOrEmpty(preSelectedLayer))
            {
                int idx = cboLayer.Items.IndexOf(preSelectedLayer);
                cboLayer.SelectedIndex = (idx >= 0) ? idx : 0;
            }
            else if (cboLayer.Items.Count > 0)
            {
                cboLayer.SelectedIndex = 0;
            }

            btnOK = new Button
            {
                Text = "OK",
                Left = 320,
                Top = 280,
                Width = 90,
                DialogResult = DialogResult.OK
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 420,
                Top = 280,
                Width = 90,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                rbDeleteSelect, rbDeleteAll, rbPurge,
                rbLtAll, rbLtSelect,
                lblLayer, cboLayer, btnPick,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}