using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Layer
{
    public class LineTypeToLayer
    {
        // Nhớ layer đã chọn lần trước (giữa các lần chạy lệnh)
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

            // ── Bước 2: Form chọn PHẠM VI + LAYER ĐÍCH ──
            var allLayers = GetAllLayers(db);
            if (allLayers.Count == 0)
            {
                Utils.Print("✖ Bản vẽ chưa có layer nào.");
                return;
            }

            bool selectAllModel;
            string targetLayer;
            using (var form = new LayerPickForm(allLayers, _lastLayer))
            {
                if (AcApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    Utils.Print("✖ Đã hủy lệnh.");
                    return;
                }
                selectAllModel = form.SelectAllModel;
                targetLayer = form.SelectedLayer;
            }

            if (string.IsNullOrEmpty(targetLayer))
            {
                Utils.Print("✖ Chưa chọn layer đích.");
                return;
            }
            _lastLayer = targetLayer;

            // ── Bước 3: Lấy selection theo phạm vi ──
            PromptSelectionResult sel;
            if (selectAllModel)
            {
                ed.WriteMessage("\n→ Đang quét toàn bộ Model...");
                sel = ed.SelectAll();
            }
            else
            {
                ed.WriteMessage("\nChọn vùng (Window / Crossing / Fence / CP...):");
                sel = ed.GetSelection();
            }

            if (sel.Status != PromptStatus.OK || sel.Value.Count == 0)
            {
                Utils.Print("✖ Không chọn được đối tượng nào.");
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

        // ─────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────
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

        private static List<string> GetAllLayers(Database db)
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
            return layers;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Form: chọn PHẠM VI + LAYER ĐÍCH
    // ═══════════════════════════════════════════════════════════
    public class LayerPickForm : Form
    {
        private RadioButton rbAllModel;
        private RadioButton rbSelectArea;
        private ComboBox cboLayer;
        private TextBox txtFilter;
        private Button btnOK;
        private Button btnCancel;

        private readonly List<string> _allLayers;

        public bool SelectAllModel => rbAllModel.Checked;
        public string SelectedLayer => cboLayer.SelectedItem?.ToString() ?? "";

        public LayerPickForm(List<string> layers, string preSelected)
        {
            _allLayers = layers ?? new List<string>();

            this.Text = "Chọn phạm vi & Layer đích";
            this.Width = 440;
            this.Height = 260;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // ── Nhóm chọn phạm vi ──
            Label lblScope = new Label
            {
                Text = "Phạm vi:",
                Left = 20,
                Top = 15,
                Width = 80,
                Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
            };

            rbSelectArea = new RadioButton
            {
                Text = "Chọn vùng (Window / Crossing / Fence / CP...)",
                Left = 20,
                Top = 42,
                Width = 400,
                Checked = true
            };
            rbAllModel = new RadioButton
            {
                Text = "Toàn bộ Model",
                Left = 20,
                Top = 66,
                Width = 400
            };

            // ── Ô lọc layer ──
            Label lblFilter = new Label
            {
                Text = "Lọc layer theo chữ cái:",
                Left = 20,
                Top = 108,
                Width = 100
            };
            txtFilter = new TextBox
            {
                Left = 125,
                Top = 105,
                Width = 285
            };
            txtFilter.TextChanged += (s, e) => ApplyFilter();

            // ── ComboBox layer ──
            Label lblLayer = new Label
            {
                Text = "Layer đích:",
                Left = 20,
                Top = 148,
                Width = 80
            };
            cboLayer = new ComboBox
            {
                Left = 105,
                Top = 145,
                Width = 310,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            ReloadCombo(_allLayers);

            // Preselect layer
            if (!string.IsNullOrEmpty(preSelected))
            {
                int idx = cboLayer.Items.IndexOf(preSelected);
                cboLayer.SelectedIndex = (idx >= 0) ? idx : 0;
            }
            else if (cboLayer.Items.Count > 0)
            {
                cboLayer.SelectedIndex = 0;
            }

            // ── Buttons ──
            btnOK = new Button
            {
                Text = "OK",
                Left = 230,
                Top = 190,
                Width = 80,
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 335,
                Top = 190,
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[]
            {
                lblScope, rbSelectArea, rbAllModel,
                lblFilter, txtFilter,
                lblLayer, cboLayer,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            this.Shown += (s, e) => txtFilter.Focus();
        }

        private void ApplyFilter()
        {
            string filter = txtFilter.Text?.Trim() ?? "";
            string current = cboLayer.SelectedItem?.ToString();

            IEnumerable<string> src = string.IsNullOrEmpty(filter)
                ? _allLayers
                : _allLayers.Where(x =>
                    x.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

            ReloadCombo(src.ToList());

            if (!string.IsNullOrEmpty(current))
            {
                int idx = cboLayer.Items.IndexOf(current);
                if (idx >= 0) cboLayer.SelectedIndex = idx;
            }
            if (cboLayer.SelectedIndex < 0 && cboLayer.Items.Count > 0)
                cboLayer.SelectedIndex = 0;
        }

        private void ReloadCombo(List<string> items)
        {
            cboLayer.BeginUpdate();
            cboLayer.Items.Clear();
            cboLayer.Items.AddRange(items.ToArray());
            cboLayer.EndUpdate();
        }
    }
}