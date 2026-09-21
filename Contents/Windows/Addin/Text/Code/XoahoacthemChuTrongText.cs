using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.TextTools
{
    public class XoahoacthemChuTrongText
    {
        [CommandMethod("XoahoacthemChuTrongText")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // ── 1. Chọn đối tượng ──
            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "TEXT,MTEXT,MULTILEADER") });
            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có đối tượng nào được chọn.");
                return;
            }

            // ── 2. Mở form nhập cặp TÌM/THAY ──
            List<ReplacePair> pairs;
            using (var form = new ReplaceForm())
            {
                if (AcApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    Utils.Print("✖ Đã hủy lệnh.");
                    return;
                }
                pairs = form.Pairs;
            }

            if (pairs == null || pairs.Count == 0)
            {
                Utils.Print("Không có cặp tìm/thay nào.");
                return;
            }

            // ── 3. Áp dụng ──
            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // Lấy văn bản gốc
                    string oldTxt = null;
                    if (ent is DBText t) oldTxt = t.TextString;
                    else if (ent is MText m) oldTxt = m.Contents;
                    else if (ent is MLeader ml)
                    {
                        if (ml.ContentType == ContentType.MTextContent)
                        {
                            MText innerMt = ml.MText;
                            if (innerMt != null) oldTxt = innerMt.Contents;
                        }
                    }
                    if (string.IsNullOrEmpty(oldTxt)) continue;

                    // Áp dụng lần lượt các cặp
                    string newTxt = oldTxt;
                    bool anyErase = false;
                    foreach (var p in pairs)
                    {
                        if (string.IsNullOrEmpty(p.Find)) continue;
                        newTxt = ReplaceIgnoreCase(newTxt, p.Find, p.Replace ?? "");
                        if (string.IsNullOrEmpty(p.Replace)) anyErase = true;
                    }

                    // Chỉ dọn khoảng trắng kép nếu có cặp "thay = rỗng" (hành vi xóa)
                    if (anyErase)
                    {
                        while (newTxt.Contains("  "))
                            newTxt = newTxt.Replace("  ", " ");
                    }
                    newTxt = newTxt.Trim();

                    if (newTxt == oldTxt) continue;

                    // Ghi lại
                    if (ent is DBText txt)
                    {
                        // TEXT → thay bằng MTEXT để giữ nội dung đa dạng
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
            Utils.Print($"Đã xử lý {count} đối tượng.");
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

    // ═══════════════════════════════════════════════════════════
    // Model: cặp tìm/thay
    // ═══════════════════════════════════════════════════════════
    public class ReplacePair
    {
        public string Find { get; set; }
        public string Replace { get; set; }
        public ReplacePair(string f, string r) { Find = f; Replace = r; }
    }

    // ═══════════════════════════════════════════════════════════
    // Form nhập cặp TÌM / THAY THẾ
    // ═══════════════════════════════════════════════════════════
    public class ReplaceForm : Form
    {
        private DataGridView dgv;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnOK;
        private Button btnCancel;

        public List<ReplacePair> Pairs { get; private set; }

        public ReplaceForm()
        {
            Text = "Thay chữ trong Text / MText / MLeader";
            Width = 600;
            Height = 460;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new System.Drawing.Font("Segoe UI", 9F);

            // ── Tiêu đề ──
            var lblTitle = new Label
            {
                Text = "Nhập các cặp TÌM / THAY THẾ (không phân biệt hoa thường):",
                Left = 12,
                Top = 10,
                Width = 570,
                Font = new System.Drawing.Font(Font, FontStyle.Bold)
            };

            var lblHint = new Label
            {
                Text = "• Cột 'Thay bằng' để trống = XÓA cụm từ đó\r\n" +
                       "• Có thể nhập nhiều cặp — áp dụng lần lượt từ trên xuống",
                Left = 12,
                Top = 32,
                Width = 570,
                Height = 34,
                ForeColor = Color.DimGray
            };

            // ── Grid ──
            dgv = new DataGridView
            {
                Left = 12,
                Top = 72,
                Width = 570,
                Height = 300,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditOnEnter,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFind",
                HeaderText = "Tìm",
                FillWeight = 50
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colReplace",
                HeaderText = "Thay bằng  (để trống = xóa)",
                FillWeight = 50
            });

            // Dòng trống ban đầu
            dgv.Rows.Add("", "");

            // ── Buttons ──
            btnAdd = new Button
            {
                Text = "Thêm dòng",
                Left = 12,
                Top = 382,
                Width = 100,
                Height = 28
            };
            btnRemove = new Button
            {
                Text = "Xóa dòng",
                Left = 120,
                Top = 382,
                Width = 100,
                Height = 28
            };

            btnOK = new Button
            {
                Text = "OK",
                Left = 396,
                Top = 382,
                Width = 90,
                Height = 28,
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 492,
                Top = 382,
                Width = 90,
                Height = 28,
                DialogResult = DialogResult.Cancel
            };

            // ── Sự kiện ──
            btnAdd.Click += (s, e) =>
            {
                int idx = dgv.Rows.Add("", "");
                dgv.CurrentCell = dgv.Rows[idx].Cells[0];
                dgv.BeginEdit(true);
            };

            btnRemove.Click += (s, e) =>
            {
                if (dgv.CurrentRow != null)
                    dgv.Rows.Remove(dgv.CurrentRow);

                // Luôn giữ ít nhất 1 dòng
                if (dgv.Rows.Count == 0)
                    dgv.Rows.Add("", "");
            };

            btnOK.Click += (s, e) =>
            {
                // Chốt giá trị ô đang edit
                dgv.EndEdit();

                Pairs = new List<ReplacePair>();
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    var f = row.Cells[0].Value?.ToString() ?? "";
                    var r = row.Cells[1].Value?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(f))
                        Pairs.Add(new ReplacePair(f, r));
                }

                if (Pairs.Count == 0)
                {
                    MessageBox.Show(
                        "Vui lòng nhập ít nhất 1 cặp TÌM/THAY.",
                        "Chưa có dữ liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;   // Ngăn form đóng
                }
            };

            // ── Add controls ──
            Controls.Add(lblTitle);
            Controls.Add(lblHint);
            Controls.Add(dgv);
            Controls.Add(btnAdd);
            Controls.Add(btnRemove);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }
    }
}