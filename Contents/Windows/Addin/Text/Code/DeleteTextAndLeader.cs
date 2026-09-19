using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.TextTools
{
    public class DeleteTextAndLeader
    {
        private static string _lastLayerFilter = "";

        [CommandMethod("DeleteTextAndLeader")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("LỆNH DELETETEXTANDLEADER - Xoá Text / Leader / Leader Note.");

            var allLayers = GetAllLayers(db);

            while (true)
            {
                string typeStr, mode, layerFilter;
                bool pickRequested;

                using (var form = new DeleteTextForm(allLayers, _lastLayerFilter))
                {
                    var dr = AcApp.ShowModalDialog(form);

                    if (form.PickRequested)
                    {
                        pickRequested = true;
                    }
                    else if (dr != DialogResult.OK)
                    {
                        Utils.Print("✖ Đã hủy lệnh.");
                        return;
                    }
                    else
                    {
                        pickRequested = false;
                    }

                    typeStr = form.SelectedTypeString;
                    mode = form.SelectAll ? "1" : "2";
                    layerFilter = form.LayerFilter;
                }

                if (pickRequested)
                {
                    ed.WriteMessage("\nChọn 1 đối tượng để lấy Layer: ");
                    var per = ed.GetEntity("");

                    if (per.Status == PromptStatus.OK)
                    {
                        using (var tr = db.TransactionManager.StartTransaction())
                        {
                            var ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent != null)
                                _lastLayerFilter = ent.Layer;
                            tr.Commit();
                        }
                    }

                    // Quay lại vòng lặp — form sẽ mở lại với CÙNG lựa chọn cũ
                    continue;
                }

                _lastLayerFilter = layerFilter;

                var filterList = new List<TypedValue>();
                filterList.Add(new TypedValue((int)DxfCode.Start, typeStr));
                if (!string.IsNullOrEmpty(layerFilter))
                    filterList.Add(new TypedValue((int)DxfCode.LayerName, layerFilter));

                var filter = new SelectionFilter(filterList.ToArray());

                PromptSelectionResult sel;
                if (mode == "1")
                {
                    ed.WriteMessage("\n→ Đang quét toàn bộ Model...");
                    sel = ed.SelectAll(filter);
                }
                else
                {
                    ed.WriteMessage("\nChọn vùng cần xoá:");
                    sel = ed.GetSelection(filter);
                }

                if (sel.Status != PromptStatus.OK)
                {
                    Utils.Print("Không tìm thấy đối tượng cần xoá.");
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
                        ent.Erase();
                        count++;
                    }
                    tr.Commit();
                }

                ed.Regen();
                Utils.Print($"→ Đã xoá {count} đối tượng.");
                return;
            }
        }

        private static List<string> GetAllLayers(Database db)
        {
            var list = new List<string>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var lay = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (lay.Name != "Defpoints")
                        list.Add(lay.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x).ToList();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Form — có nhớ trạng thái giữa các lần mở
    // ═══════════════════════════════════════════════════════════
    public class DeleteTextForm : Form
    {
        // ═══ STATIC: nhớ lựa chọn qua các lần mở form ═══
        private static int _savedTypeIndex = 0;    // 0..3
        private static int _savedScopeIndex = 0;    // 0 = All, 1 = Select area

        private RadioButton rbText;
        private RadioButton rbLeader;
        private RadioButton rbLeaderNote;
        private RadioButton rbAll;

        private RadioButton rbAllModel;
        private RadioButton rbSelectArea;

        private TextBox txtLayerFilter;
        private ComboBox cboLayer;
        private Button btnPickLayer;

        private Button btnOK;
        private Button btnCancel;

        private readonly List<string> _allLayers;

        public bool PickRequested { get; private set; } = false;

        public string SelectedTypeString
        {
            get
            {
                if (rbText.Checked) return "TEXT,MTEXT";
                if (rbLeader.Checked) return "LEADER";
                if (rbLeaderNote.Checked) return "MULTILEADER";
                if (rbAll.Checked) return "TEXT,MTEXT,LEADER,MULTILEADER";
                return "TEXT,MTEXT";
            }
        }

        public bool SelectAll => rbAllModel.Checked;
        public string LayerFilter => cboLayer.Text?.Trim() ?? "";

        public DeleteTextForm(List<string> allLayers, string preLayer)
        {
            _allLayers = allLayers ?? new List<string>();

            this.Text = "Xoá Text / Leader";
            this.Width = 460;
            this.Height = 420;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // ═══════════════════════════════════════════════════
            //  NHÓM 1: Loại đối tượng
            // ═══════════════════════════════════════════════════
            Label lblType = new Label
            {
                Text = "Loại đối tượng:",
                Left = 20,
                Top = 15,
                Width = 400,
                Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
            };

            Panel pnlType = new Panel
            {
                Left = 20,
                Top = 38,
                Width = 420,
                Height = 105
            };

            rbText = new RadioButton { Text = "1. Text / MText", Left = 10, Top = 3, Width = 400 };
            rbLeader = new RadioButton { Text = "2. Leader", Left = 10, Top = 27, Width = 400 };
            rbLeaderNote = new RadioButton { Text = "3. Leader Note (MLeader)", Left = 10, Top = 51, Width = 400 };
            rbAll = new RadioButton { Text = "4. Tất cả (Text + Leader + MLeader)", Left = 10, Top = 75, Width = 400 };

            pnlType.Controls.AddRange(new Control[] { rbText, rbLeader, rbLeaderNote, rbAll });

            // ── Khôi phục lựa chọn cũ ──
            switch (_savedTypeIndex)
            {
                case 0: rbText.Checked = true; break;
                case 1: rbLeader.Checked = true; break;
                case 2: rbLeaderNote.Checked = true; break;
                case 3: rbAll.Checked = true; break;
                default: rbText.Checked = true; break;
            }

            // ═══════════════════════════════════════════════════
            //  NHÓM 2: Phạm vi
            // ═══════════════════════════════════════════════════
            Label lblScope = new Label
            {
                Text = "Phạm vi:",
                Left = 20,
                Top = 152,
                Width = 400,
                Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
            };

            Panel pnlScope = new Panel
            {
                Left = 20,
                Top = 175,
                Width = 420,
                Height = 55
            };

            rbAllModel = new RadioButton { Text = "Toàn bộ Model", Left = 10, Top = 3, Width = 400 };
            rbSelectArea = new RadioButton { Text = "Chọn vùng", Left = 10, Top = 27, Width = 400 };

            pnlScope.Controls.AddRange(new Control[] { rbAllModel, rbSelectArea });

            // ── Khôi phục lựa chọn cũ ──
            if (_savedScopeIndex == 1) rbSelectArea.Checked = true;
            else rbAllModel.Checked = true;

            // ═══════════════════════════════════════════════════
            //  NHÓM 3: Layer
            // ═══════════════════════════════════════════════════
            Label lblLayerTitle = new Label
            {
                Text = "Layer (bỏ trống = tất cả):",
                Left = 20,
                Top = 240,
                Width = 400,
                Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
            };

            Label lblFilter = new Label
            {
                Text = "Lọc:",
                Left = 30,
                Top = 270,
                Width = 40
            };
            txtLayerFilter = new TextBox
            {
                Left = 75,
                Top = 267,
                Width = 250
            };
            txtLayerFilter.TextChanged += (s, e) => ApplyLayerFilter();

            btnPickLayer = new Button
            {
                Text = "Pick Layer",
                Left = 335,
                Top = 265,
                Width = 80,
                Height = 26
            };
            btnPickLayer.Click += (s, e) =>
            {
                // ── LƯU trạng thái hiện tại trước khi đóng ──
                SaveCurrentSelections();

                PickRequested = true;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            cboLayer = new ComboBox
            {
                Left = 75,
                Top = 300,
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDown
            };

            ReloadLayerCombo(_allLayers);

            if (!string.IsNullOrEmpty(preLayer))
                cboLayer.Text = preLayer;
            else if (cboLayer.Items.Count > 0)
                cboLayer.SelectedIndex = 0;

            // ═══════════════════════════════════════════════════
            //  Buttons
            // ═══════════════════════════════════════════════════
            btnOK = new Button
            {
                Text = "OK",
                Left = 245,
                Top = 345,
                Width = 80,
                Height = 28,
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 340,
                Top = 345,
                Width = 80,
                Height = 28,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[]
            {
                lblType, pnlType,
                lblScope, pnlScope,
                lblLayerTitle,
                lblFilter, txtLayerFilter, btnPickLayer,
                cboLayer,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // ── Lưu trạng thái khi user thay đổi (để các lần sau vẫn nhớ) ──
            rbText.CheckedChanged += (s, e) => SaveCurrentSelections();
            rbLeader.CheckedChanged += (s, e) => SaveCurrentSelections();
            rbLeaderNote.CheckedChanged += (s, e) => SaveCurrentSelections();
            rbAll.CheckedChanged += (s, e) => SaveCurrentSelections();
            rbAllModel.CheckedChanged += (s, e) => SaveCurrentSelections();
            rbSelectArea.CheckedChanged += (s, e) => SaveCurrentSelections();

            this.Shown += (s, e) => txtLayerFilter.Focus();
        }

        // ── Lưu lựa chọn hiện tại vào static ──
        private void SaveCurrentSelections()
        {
            if (rbText.Checked) _savedTypeIndex = 0;
            else if (rbLeader.Checked) _savedTypeIndex = 1;
            else if (rbLeaderNote.Checked) _savedTypeIndex = 2;
            else if (rbAll.Checked) _savedTypeIndex = 3;

            _savedScopeIndex = rbSelectArea.Checked ? 1 : 0;
        }

        private void ApplyLayerFilter()
        {
            string filter = txtLayerFilter.Text?.Trim() ?? "";
            string current = cboLayer.Text?.Trim();

            List<string> src = string.IsNullOrEmpty(filter)
                ? _allLayers
                : _allLayers
                    .Where(x => x.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            ReloadLayerCombo(src);

            if (!string.IsNullOrEmpty(current))
                cboLayer.Text = current;
            else if (cboLayer.Items.Count > 0)
                cboLayer.SelectedIndex = 0;
        }

        private void ReloadLayerCombo(List<string> items)
        {
            cboLayer.BeginUpdate();
            cboLayer.Items.Clear();
            cboLayer.Items.Add("");
            cboLayer.Items.AddRange(items.ToArray());
            cboLayer.EndUpdate();
        }
    }
}