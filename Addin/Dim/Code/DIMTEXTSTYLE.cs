using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CADAddin.Common;                    // ← THÊM để gọi Utils

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;


namespace CADAddin.Dim
{
    public class DimTextStyleCommands
    {
        private const string REG_KEY = @"Software\YourCompany\DimTextStyle";

        [CommandMethod("DimTextStyle")]
        public void DimTextStyle()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<string> dimStyles = GetDimStyles(db);
            List<string> layers = GetAllLayers(db);

            // Đọc giá trị nhớ lần trước
            string lastSource = GetReg("DIMTXT_SRC");
            string lastTarget = GetReg("DIMTXT_TGT");
            string lastLayer = GetReg("DIMTXT_LYR");

            using (DimTextStyleForm form = new DimTextStyleForm(dimStyles, layers, lastSource, lastTarget, lastLayer))
            {
                if (AcadApp.ShowModalDialog(form) != DialogResult.OK)
                {
                    ed.WriteMessage("\nĐã hủy lệnh.");
                    return;
                }

                string mode = form.SelectedMode;
                string layerOpt = form.LayerOption;
                string sourceStyle = form.SourceStyle;
                string targetStyle = form.TargetStyle;
                string sourceLayer = form.SourceLayer;

                // Lưu lại lựa chọn
                if (!string.IsNullOrEmpty(sourceStyle)) SetReg("DIMTXT_SRC", sourceStyle);
                if (!string.IsNullOrEmpty(targetStyle)) SetReg("DIMTXT_TGT", targetStyle);
                if (!string.IsNullOrEmpty(sourceLayer)) SetReg("DIMTXT_LYR", sourceLayer);

                switch (mode)
                {
                    case "mode1":
                        ChangeAllDims(ed, targetStyle);
                        break;

                    case "mode2":
                        ChangeSelectedDims(ed, targetStyle, layerOpt, sourceLayer);
                        break;

                    case "mode4":
                        SetBaseDimStyle(ed, sourceStyle);
                        break;
                }
            }
        }

        // ========== Chức năng chính ==========

        private void ChangeAllDims(Editor ed, string targetStyle)
        {
            ed.WriteMessage("\n[Chế độ 1] Thay tất cả DIM sang style đích...");
            TypedValue[] filter = { new TypedValue((int)DxfCode.Start, "DIMENSION") };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.SelectAll(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                ed.WriteMessage("\nKhông có DIM nào trong bản vẽ.");
                return;
            }

            int count = UpdateDimStyles(ed.Document.Database, psr.Value.GetObjectIds(), targetStyle, null);
            ed.WriteMessage($"\nĐã đổi {count} DIM sang style: {targetStyle}");
        }

        private void ChangeSelectedDims(Editor ed, string targetStyle, string layerOpt, string sourceLayer)
        {
            ed.WriteMessage("\n[Chế độ 2] Chọn vùng các DIM cần đổi...");
            TypedValue[] filter = { new TypedValue((int)DxfCode.Start, "DIMENSION") };
            SelectionFilter sf = new SelectionFilter(filter);
            PromptSelectionResult psr = ed.GetSelection(sf);

            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
            {
                ed.WriteMessage("\nKhông chọn được DIM nào.");
                return;
            }

            string filterLayer = (layerOpt == "sel2") ? sourceLayer : null;
            int count = UpdateDimStyles(ed.Document.Database, psr.Value.GetObjectIds(), targetStyle, filterLayer);
            ed.WriteMessage($"\nĐã đổi {count} DIM theo tùy chọn.");
        }

        private void SetBaseDimStyle(Editor ed, string sourceStyle)
        {
            ed.WriteMessage("\n[Chế độ 3] Cập nhật Base DIM Text Style theo style nguồn...");
            if (string.IsNullOrEmpty(sourceStyle))
            {
                MessageBox.Show("Không có style nguồn hợp lệ!");
                return;
            }

            // Kiểm tra tồn tại
            using (Transaction tr = ed.Document.Database.TransactionManager.StartTransaction())
            {
                DimStyleTable dst = tr.GetObject(ed.Document.Database.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;
                if (!dst.Has(sourceStyle))
                {
                    MessageBox.Show($"Không tìm thấy Dimension Style: {sourceStyle}");
                    return;
                }
                tr.Commit();
            }

            // Thực hiện -DIMSTYLE Restore
            ed.Command("_.-DIMSTYLE", "_R", sourceStyle, "");
            ed.WriteMessage($"\nBase DIM Text Style đã đặt theo: {sourceStyle}");
        }

        private int UpdateDimStyles(Database db, ObjectId[] ids, string targetStyle, string onlyLayer)
        {
            int count = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent == null) continue;

                    if (!string.IsNullOrEmpty(onlyLayer) && ent.Layer != onlyLayer)
                        continue;

                    // Dimension có property StyleName
                    if (ent is Dimension dim)
                    {
                        try
                        {
                            dim.DimensionStyleName = targetStyle;
                            count++;
                        }
                        catch { }
                    }
                }
                tr.Commit();
            }
            return count;
        }

        // ========== Helper ==========

        private List<string> GetDimStyles(Database db)
        {
            List<string> list = new List<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DimStyleTable dst = tr.GetObject(db.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;
                foreach (ObjectId id in dst)
                {
                    DimStyleTableRecord dstr = tr.GetObject(id, OpenMode.ForRead) as DimStyleTableRecord;
                    list.Add(dstr.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x).ToList();
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
                    list.Add(ltr.Name);
                }
                tr.Commit();
            }
            return list.OrderBy(x => x).ToList();
        }

        private string GetReg(string name)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REG_KEY))
                {
                    return key?.GetValue(name)?.ToString() ?? "";
                }
            }
            catch { return ""; }
        }

        private void SetReg(string name, string value)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(REG_KEY))
                {
                    key.SetValue(name, value);
                }
            }
            catch { }
        }

        // ========== Windows Form ==========
        public class DimTextStyleForm : Form
        {
            private RadioButton rbMode1, rbMode2, rbMode4;
            private RadioButton rbSel1, rbSel2;
            private ComboBox cboSource, cboTarget, cboLayer;
            private Button btnOK, btnCancel;

            public string SelectedMode
            {
                get
                {
                    if (rbMode1.Checked) return "mode1";
                    if (rbMode2.Checked) return "mode2";
                    if (rbMode4.Checked) return "mode4";
                    return "mode2";
                }
            }

            public string LayerOption => rbSel1.Checked ? "sel1" : "sel2";
            public string SourceStyle => cboSource.SelectedItem?.ToString() ?? "";
            public string TargetStyle => cboTarget.SelectedItem?.ToString() ?? "";
            public string SourceLayer => cboLayer.SelectedItem?.ToString() ?? "";

            public DimTextStyleForm(List<string> dimStyles, List<string> layers,
                                    string lastSource, string lastTarget, string lastLayer)
            {
                this.Text = "DIMTEXTSTYLE v5 (Memory)";
                this.Width = 520;
                this.Height = 420;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterScreen;
                this.MaximizeBox = false;
                this.MinimizeBox = false;

                Label lblMode = new Label { Text = "Chọn chế độ thao tác:", Left = 20, Top = 15, Width = 460, Font = new System.Drawing.Font(this.Font, System.Drawing.FontStyle.Bold) };

                rbMode1 = new RadioButton { Text = "1. Thay đổi TẤT CẢ DIM trong bản vẽ", Left = 20, Top = 40, Width = 460 };
                rbMode2 = new RadioButton { Text = "2. Thay đổi DIM theo vùng chọn", Left = 20, Top = 65, Width = 460, Checked = true };
                rbMode4 = new RadioButton { Text = "3. Thay Base DIM Text Style theo style nguồn", Left = 20, Top = 90, Width = 460 };

                GroupBox gbOpt = new GroupBox { Text = "Tùy chọn chế độ 2", Left = 20, Top = 120, Width = 460, Height = 80 };
                rbSel1 = new RadioButton { Text = "1. Đổi tất cả DIM trong vùng chọn sang style đích", Left = 15, Top = 25, Width = 430, Checked = true };
                rbSel2 = new RadioButton { Text = "2. Chỉ đổi DIM có layer trùng với layer nguồn", Left = 15, Top = 50, Width = 430 };
                gbOpt.Controls.AddRange(new Control[] { rbSel1, rbSel2 });

                Label lblSrc = new Label { Text = "Style nguồn:", Left = 20, Top = 220, Width = 100 };
                cboSource = new ComboBox { Left = 130, Top = 217, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
                cboSource.Items.AddRange(dimStyles.ToArray());
                SetCombo(cboSource, lastSource);

                Label lblTgt = new Label { Text = "Style đích:", Left = 20, Top = 255, Width = 100 };
                cboTarget = new ComboBox { Left = 130, Top = 252, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
                cboTarget.Items.AddRange(dimStyles.ToArray());
                SetCombo(cboTarget, lastTarget);

                Label lblLay = new Label { Text = "Layer nguồn:", Left = 20, Top = 290, Width = 100 };
                cboLayer = new ComboBox { Left = 130, Top = 287, Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
                cboLayer.Items.AddRange(layers.ToArray());
                SetCombo(cboLayer, lastLayer);

                btnOK = new Button { Text = "OK", Left = 300, Top = 340, Width = 80, DialogResult = DialogResult.OK };
                btnCancel = new Button { Text = "Cancel", Left = 390, Top = 340, Width = 80, DialogResult = DialogResult.Cancel };

                this.Controls.AddRange(new Control[] {
                lblMode, rbMode1, rbMode2, rbMode4, gbOpt,
                lblSrc, cboSource, lblTgt, cboTarget, lblLay, cboLayer,
                btnOK, btnCancel
            });

                this.AcceptButton = btnOK;
                this.CancelButton = btnCancel;
            }

            private void SetCombo(ComboBox cbo, string value)
            {
                if (string.IsNullOrEmpty(value)) { if (cbo.Items.Count > 0) cbo.SelectedIndex = 0; return; }
                int idx = cbo.Items.IndexOf(value);
                cbo.SelectedIndex = (idx >= 0) ? idx : 0;
            }
        }
    }
}