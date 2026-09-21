using System;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.TextTools
{
    public class Doiinhoachu
    {
        // Nhớ lựa chọn lần trước (0..4)
        private static int _lastOption = 0;

        [CommandMethod("Doiinhoachu")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // ═══════════════════════════════════════════════════════
            //  Mở form chọn kiểu
            // ═══════════════════════════════════════════════════════
            using (var form = new CaseConvertForm(_lastOption))
            {
                if (AcApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    Utils.Print("✖ Đã hủy lệnh.");
                    return;
                }
                                _lastOption = form.SelectedIndex;
            }

            int opt = _lastOption;

            // ═══════════════════════════════════════════════════════
            //  Chọn đối tượng Text / MText
            // ═══════════════════════════════════════════════════════
            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "TEXT,MTEXT") });

            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có đối tượng nào được chọn.");
                return;
            }

            // ═══════════════════════════════════════════════════════
            //  Chuyển đổi
            // ═══════════════════════════════════════════════════════
            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                    if (ent == null) continue;

                    string oldTxt = null;
                    if (ent is MText mt) oldTxt = mt.Contents;
                    else if (ent is DBText txt) oldTxt = txt.TextString;
                    if (string.IsNullOrEmpty(oldTxt)) continue;

                    string newTxt;
                    switch (opt)
                    {
                        case 1: newTxt = CaseConvert(oldTxt, true); break;   // Thường
                        case 2: newTxt = SentenceCase(oldTxt); break;   // Hoa đầu dòng
                        case 3: newTxt = TitleCase(oldTxt); break;   // Hoa đầu từ
                        case 4: newTxt = FirstUpperRestLower(oldTxt); break;   // Hoa đầu tiên
                        default: newTxt = CaseConvert(oldTxt, false); break;   // 0 - Hoa
                    }

                    if (newTxt != oldTxt)
                    {
                        if (ent is MText mt2) mt2.Contents = newTxt;
                        else if (ent is DBText txt2) txt2.TextString = newTxt;
                        count++;
                    }
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"Hoàn tất – đã xử lý {count} đối tượng.");
        }

        // ═══════════════════════════════════════════════════════════
        //  1 & 2) UPPER / lower
        // ═══════════════════════════════════════════════════════════
        private static string CaseConvert(string s, bool toLower)
        {
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                char ch = s[i];
                if (ch == '\\' && i + 1 < s.Length)
                {
                    int consumed;
                    AppendEscape(s, i, sb, out consumed);
                    i += consumed;
                    continue;
                }
                sb.Append(toLower ? char.ToLowerInvariant(ch) : char.ToUpperInvariant(ch));
                i++;
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  3) Hoa đầu dòng
        // ═══════════════════════════════════════════════════════════
        private static string SentenceCase(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool startOfLine = true;
            int i = 0;

            while (i < s.Length)
            {
                char ch = s[i];

                if (ch == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];
                    if (next == 'P' || next == 'p')
                    {
                        sb.Append(s, i, 2);
                        i += 2;
                        startOfLine = true;
                        continue;
                    }
                    int consumed;
                    AppendEscape(s, i, sb, out consumed);
                    i += consumed;
                    continue;
                }

                if (char.IsLetter(ch))
                {
                    sb.Append(startOfLine
                        ? char.ToUpperInvariant(ch)
                        : char.ToLowerInvariant(ch));
                    startOfLine = false;
                }
                else
                {
                    if (ch == '\n' || ch == '\r')
                        startOfLine = true;
                    sb.Append(ch);
                }
                i++;
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  4) Hoa đầu từ
        // ═══════════════════════════════════════════════════════════
        private static string TitleCase(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool startOfWord = true;
            int i = 0;

            while (i < s.Length)
            {
                char ch = s[i];

                if (ch == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];
                    if (next == 'P' || next == 'p')
                    {
                        sb.Append(s, i, 2);
                        i += 2;
                        startOfWord = true;
                        continue;
                    }
                    int consumed;
                    AppendEscape(s, i, sb, out consumed);
                    i += consumed;
                    startOfWord = true;
                    continue;
                }

                if (char.IsLetter(ch))
                {
                    sb.Append(startOfWord
                        ? char.ToUpperInvariant(ch)
                        : char.ToLowerInvariant(ch));
                    startOfWord = false;
                }
                else
                {
                    startOfWord = true;
                    sb.Append(ch);
                }
                i++;
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  5) Hoa đầu tiên — lower hết rồi viết hoa 1 chữ cái đầu
        // ═══════════════════════════════════════════════════════════
        private static string FirstUpperRestLower(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            string lower = CaseConvert(s, toLower: true);
            var sb = new StringBuilder(lower);
            int i = 0;

            while (i < sb.Length)
            {
                char ch = sb[i];

                if (ch == '\\' && i + 1 < sb.Length)
                {
                    char next = sb[i + 1];
                    if (next == 'P' || next == 'p' ||
                        next == '\\' || next == '{' || next == '}' || next == '~')
                    {
                        i += 2;
                        continue;
                    }
                    int j = i + 1;
                    while (j < sb.Length && sb[j] != ';') j++;
                    i = (j < sb.Length) ? j + 1 : j;
                    continue;
                }

                if (char.IsLetter(ch))
                {
                    sb[i] = char.ToUpperInvariant(ch);
                    break;
                }
                i++;
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  Escape helper
        // ═══════════════════════════════════════════════════════════
        private static void AppendEscape(string s, int i, StringBuilder sb, out int consumed)
        {
            char next = s[i + 1];

            if (next == 'P' || next == 'p')
            {
                sb.Append(s, i, 2);
                consumed = 2;
                return;
            }
            if (next == '\\' || next == '{' || next == '}' || next == '~')
            {
                sb.Append(s, i, 2);
                consumed = 2;
                return;
            }
            int j = i + 1;
            while (j < s.Length && s[j] != ';') j++;
            if (j < s.Length) j++;
            sb.Append(s, i, j - i);
            consumed = j - i;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Form chọn kiểu chuyển đổi
    // ═══════════════════════════════════════════════════════════
    public class CaseConvertForm : Form
    {
        private RadioButton rbHoa;
        private RadioButton rbThuong;
        private RadioButton rbDauDong;
        private RadioButton rbMoiTu;
        private RadioButton rbChuDau;

        private Button btnOK;
        private Button btnCancel;

        public int SelectedIndex { get; private set; } = 0;

        public CaseConvertForm(int preSelected)
        {
            this.Text = "Chọn kiểu chuyển đổi chữ";
            this.Width = 400;
            this.Height = 260;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Chọn kiểu chuyển đổi:",
                Left = 20,
                Top = 15,
                Width = 350,
                Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold)
            };

            // ── Bọc các RadioButton trong 1 Panel để chúng cùng nhóm ──
            Panel pnlOpts = new Panel
            {
                Left = 20,
                Top = 42,
                Width = 350,
                Height = 150
            };

            rbHoa = new RadioButton
            {
                Text = "1. Hoa           (IN HOA HẾT)",
                Left = 5,
                Top = 3,
                Width = 340
            };
            rbThuong = new RadioButton
            {
                Text = "2. Thường        (in thường hết)",
                Left = 5,
                Top = 27,
                Width = 340
            };
            rbDauDong = new RadioButton
            {
                Text = "3. Hoa đầu dòng  (chữ cái đầu MỖI DÒNG)",
                Left = 5,
                Top = 51,
                Width = 340
            };
            rbMoiTu = new RadioButton
            {
                Text = "4. Hoa đầu từ    (chữ cái đầu MỖI TỪ)",
                Left = 5,
                Top = 75,
                Width = 340
            };
            rbChuDau = new RadioButton
            {
                Text = "5. Hoa đầu tiên  (chỉ chữ cái đầu, còn lại thường)",
                Left = 5,
                Top = 99,
                Width = 340
            };

            pnlOpts.Controls.AddRange(new Control[]
            {
                rbHoa, rbThuong, rbDauDong, rbMoiTu, rbChuDau
            });

            // ── Chọn sẵn ──
            switch (preSelected)
            {
                case 1: rbThuong.Checked = true; break;
                case 2: rbDauDong.Checked = true; break;
                case 3: rbMoiTu.Checked = true; break;
                case 4: rbChuDau.Checked = true; break;
                default: rbHoa.Checked = true; break;
            }

            // ── Buttons ──
            btnOK = new Button
            {
                Text = "OK",
                Left = 195,
                Top = 170,
                Width = 80,
                Height = 28,
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 290,
                Top = 170,
                Width = 80,
                Height = 28,
                DialogResult = DialogResult.Cancel
            };

            btnOK.Click += (s, e) =>
            {
                if (rbHoa.Checked) SelectedIndex = 0;
                else if (rbThuong.Checked) SelectedIndex = 1;
                else if (rbDauDong.Checked) SelectedIndex = 2;
                else if (rbMoiTu.Checked) SelectedIndex = 3;
                else if (rbChuDau.Checked) SelectedIndex = 4;
            };

            this.Controls.AddRange(new Control[]
            {
                lblTitle, pnlOpts, btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}