using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Dim
{
    public class DimChangeStyleWrite
    {
        // Nhớ style đã chọn lần trước
        private static string _lastStyle = null;

        [CommandMethod("DIMCHANGESTYLEWRITE")]
        public void ChangeDimStyle()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("Lệnh DIMCHANGESTYLEWRITE - Đổi Dim Style.");

            // ═══════════════════════════════════════════════════════
            //  Bước 1: Lấy danh sách Dimstyle
            // ═══════════════════════════════════════════════════════
            var styleNames = GetAllDimStyles(db);
            if (styleNames.Count == 0)
            {
                Utils.Print("❌ Bản vẽ chưa có Dimstyle nào.");
                return;
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 2: Vòng lặp mở form — có thể pick lại nhiều lần
            // ═══════════════════════════════════════════════════════
            string dimStyleName = _lastStyle;

            while (true)
            {
                bool pickRequested;

                using (var form = new DimStyleSelectForm(styleNames, dimStyleName))
                {
                    var dr = AcApp.ShowModalDialog(form);

                    if (form.PickRequested)
                    {
                        pickRequested = true;
                    }
                    else if (dr != DialogResult.OK)
                    {
                        Utils.Print("⏹️ Hủy lệnh.");
                        return;
                    }
                    else
                    {
                        pickRequested = false;
                    }

                    dimStyleName = form.SelectedStyle;
                }

                if (!pickRequested)
                    break;   // OK → thoát vòng lặp

                // ── Pick DIM mẫu để lấy style ──
                var peo = new PromptEntityOptions(
                    "\nChọn 1 DIMENSION mẫu để lấy Dimstyle: ");
                peo.SetRejectMessage("\nChỉ được chọn DIMENSION!");
                peo.AddAllowedClass(typeof(Dimension), false);

                var per = ed.GetEntity(peo);
                if (per.Status == PromptStatus.OK)
                {
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var dim = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Dimension;
                        if (dim != null)
                        {
                            string fullName = dim.DimensionStyleName;
                            string styleFromDim = fullName;

                            // Cắt phần sau $ (nếu có)
                            int idx = fullName.IndexOf('$');
                            if (idx > 0)
                            {
                                string shortName = fullName.Substring(0, idx);
                                var dst = (DimStyleTable)tr.GetObject(
                                    db.DimStyleTableId, OpenMode.ForRead);
                                if (dst.Has(shortName))
                                    styleFromDim = shortName;
                            }

                            dimStyleName = styleFromDim;

                            // Nếu style này chưa có trong danh sách → thêm vào
                            if (!styleNames.Contains(styleFromDim,
                                    StringComparer.OrdinalIgnoreCase))
                            {
                                styleNames.Add(styleFromDim);
                                styleNames.Sort(StringComparer.OrdinalIgnoreCase);
                            }

                            Utils.Print($"✔ Đã lấy Dimstyle: {styleFromDim}");
                        }
                        tr.Commit();
                    }
                }
                else
                {
                    Utils.Print("⏹️ Không chọn được DIM.");
                }

                // Quay lại vòng lặp → mở lại form với style mới
            }

            if (string.IsNullOrEmpty(dimStyleName))
            {
                Utils.Print("❌ Chưa chọn Dim Style.");
                return;
            }

            _lastStyle = dimStyleName;   // nhớ lần sau

            // ═══════════════════════════════════════════════════════
            //  Bước 3: Lấy ObjectId của style
            // ═══════════════════════════════════════════════════════
            ObjectId dimStyleId;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                if (!dst.Has(dimStyleName))
                {
                    Utils.Print($"❌ Dim Style '{dimStyleName}' không tồn tại!");
                    return;
                }
                dimStyleId = dst[dimStyleName];
                tr.Commit();
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 4: Chọn các DIM cần đổi style
            // ═══════════════════════════════════════════════════════
            ed.WriteMessage($"\n→ Đang đổi sang Dim Style: {dimStyleName}");
            ed.WriteMessage("\nChọn các DIM cần đổi style:");

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "DIMENSION") });

            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK || sel.Value.Count == 0)
            {
                Utils.Print("⏹️ Không chọn DIM nào.");
                return;
            }

            // ═══════════════════════════════════════════════════════
            //  Bước 5: Gán dimstyle mới cho từng DIM
            // ═══════════════════════════════════════════════════════
            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var dim = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Dimension;
                    if (dim == null) continue;

                    dim.DimensionStyle = dimStyleId;
                    dim.RecordGraphicsModified(true);
                    count++;
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"✓ Đã đổi Dim Style '{dimStyleName}' cho {count} DIM được chọn.");
        }

        // ═══════════════════════════════════════════════════════════
        //  Lấy danh sách tất cả Dimstyle
        // ═══════════════════════════════════════════════════════════
        private static List<string> GetAllDimStyles(Database db)
        {
            var list = new List<string>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                foreach (ObjectId id in dst)
                {
                    var rec = (DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    list.Add(rec.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM chọn Dimstyle
    // ═══════════════════════════════════════════════════════════
    public class DimStyleSelectForm : Form
    {
        private TextBox txtFilter;
        private ComboBox cboStyle;
        private Button btnPickFromDrawing;      // ← MỚI
        private Button btnOK;
        private Button btnCancel;

        private readonly List<string> _allStyles;

        public string SelectedStyle => cboStyle.Text?.Trim() ?? "";
        public bool PickRequested { get; private set; } = false;   // ← MỚI

        public DimStyleSelectForm(List<string> styles, string preSelected)
        {
            _allStyles = styles ?? new List<string>();

            this.Text = "Chọn Dim Style";
            this.ClientSize = new Size(460, 210);      // rộng hơn để chứa nút mới
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // ── Ô lọc ──
            Label lblFilter = new Label
            {
                Text = "Lọc:",
                Left = 20,
                Top = 25,
                Width = 40
            };
            txtFilter = new TextBox
            {
                Left = 65,
                Top = 22,
                Width = 375
            };
            txtFilter.TextChanged += delegate { ApplyFilter(); };

            // ── ComboBox style ──
            Label lblStyle = new Label
            {
                Text = "Dim Style:",
                Left = 20,
                Top = 68,
                Width = 60
            };
            cboStyle = new ComboBox
            {
                Left = 85,
                Top = 65,
                Width = 265,
                DropDownStyle = ComboBoxStyle.DropDown
            };

            ReloadCombo(_allStyles);

            // Preselect
            if (!string.IsNullOrEmpty(preSelected))
            {
                int idx = cboStyle.Items.IndexOf(preSelected);
                if (idx >= 0) cboStyle.SelectedIndex = idx;
                else cboStyle.Text = preSelected;
            }
            else if (cboStyle.Items.Count > 0)
            {
                cboStyle.SelectedIndex = 0;
            }

            // ── Nút "Pick từ bản vẽ" ──
            btnPickFromDrawing = new Button
            {
                Text = "Pick từ\nbản vẽ",
                Left = 360,
                Top = 60,
                Width = 80,
                Height = 46,
                BackColor = Color.FromArgb(220, 240, 220),
                FlatStyle = FlatStyle.System
            };
            btnPickFromDrawing.Click += delegate
            {
                PickRequested = true;
                this.DialogResult = DialogResult.Cancel;   // đóng form để pick
                this.Close();
            };

            // ── Buttons ──
            btnOK = new Button
            {
                Text = "OK",
                Left = 260,
                Top = 135,
                Width = 85,
                Height = 30
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 355,
                Top = 135,
                Width = 85,
                Height = 30
            };

            btnOK.Click += delegate
            {
                if (string.IsNullOrEmpty(SelectedStyle))
                {
                    MessageBox.Show(
                        "Vui lòng chọn 1 Dim Style.",
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                this.DialogResult = DialogResult.OK;
            };

            btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
            };

            this.Controls.AddRange(new Control[]
            {
                lblFilter, txtFilter,
                lblStyle, cboStyle, btnPickFromDrawing,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            this.Shown += delegate
            {
                txtFilter.Focus();
            };
        }

        // ── Lọc danh sách style theo chuỗi gõ vào ──
        private void ApplyFilter()
        {
            string filter = txtFilter.Text?.Trim() ?? "";
            string current = cboStyle.Text?.Trim();

            List<string> src = string.IsNullOrEmpty(filter)
                ? _allStyles
                : _allStyles
                    .Where(x => x.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            ReloadCombo(src);

            if (!string.IsNullOrEmpty(current))
                cboStyle.Text = current;
            else if (cboStyle.Items.Count > 0)
                cboStyle.SelectedIndex = 0;
        }

        private void ReloadCombo(List<string> items)
        {
            cboStyle.BeginUpdate();
            cboStyle.Items.Clear();
            cboStyle.Items.AddRange(items.ToArray());
            cboStyle.EndUpdate();
        }
    }
}