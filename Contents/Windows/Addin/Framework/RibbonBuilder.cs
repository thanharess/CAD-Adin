using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CADAddin.Framework

{
    public class RibbonBuilder
    {
        private readonly Assembly _asm;
        private readonly string _lispFolder;
        private readonly string _imageFolder;

        public RibbonBuilder(Assembly asm, string lispFolder, string imageFolder)
        {
            _asm = asm;
            _lispFolder = lispFolder;
            _imageFolder = imageFolder;
        }

        public void Build()
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null) return;

            // ===== 1. Quét tất cả =====
            var normalButtons = new List<KeyValuePair<MethodInfo, RibbonButtonAttribute>>();
            var dropDowns = new List<KeyValuePair<MethodInfo, RibbonDropDownAttribute>>();
            var dropItems = new List<KeyValuePair<MethodInfo, RibbonDropItemAttribute>>();

            foreach (var type in _asm.GetTypes())
            {
                foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var a1 = m.GetCustomAttribute<RibbonButtonAttribute>();
                    if (a1 != null) normalButtons.Add(new KeyValuePair<MethodInfo, RibbonButtonAttribute>(m, a1));

                    var a2 = m.GetCustomAttribute<RibbonDropDownAttribute>();
                    if (a2 != null) dropDowns.Add(new KeyValuePair<MethodInfo, RibbonDropDownAttribute>(m, a2));

                    var a3 = m.GetCustomAttribute<RibbonDropItemAttribute>();
                    if (a3 != null) dropItems.Add(new KeyValuePair<MethodInfo, RibbonDropItemAttribute>(m, a3));
                }
            }

            LoadAllLisp();

            // ===== 2. Gom tất cả vào 1 dict theo Tab → Panel =====
            // Mỗi phần tử là 1 "item" (nút thường HOẶC dropdown)
            var tabDict = new Dictionary<string,
                Dictionary<string, List<object>>>();

            // Nút thường
            foreach (var kv in normalButtons)
            {
                var a = kv.Value;
                AddToDict(tabDict, a.Tab, a.Panel, kv);
            }

            // Dropdown
            foreach (var kv in dropDowns)
            {
                var a = kv.Value;
                AddToDict(tabDict, a.Tab, a.Panel, kv);
            }

            // ===== 3. Duyệt Tab =====
            foreach (var tabKv in tabDict)
            {
                string tabId = "TAB_" + tabKv.Key.Replace(" ", "_");
                var tab = ribbon.FindTab(tabId);
                if (tab == null)
                {
                    tab = new RibbonTab { Title = tabKv.Key, Id = tabId };
                    ribbon.Tabs.Add(tab);
                }

                // ─── 4. Duyệt Panel ───
                foreach (var panelKv in tabKv.Value)
                {
                    var src = new RibbonPanelSource { Title = panelKv.Key };

                    var list = panelKv.Value;
                    list.Sort((a, b) => GetOrder(a).CompareTo(GetOrder(b)));

                    // ✅ Lấy RowsPerColumn từ item đầu panel
                    int maxRows = GetRowsPerColumn(list[0]);

                    RibbonRowPanel currentCol = new RibbonRowPanel();
                    int countInCol = 0;

                    foreach (var obj in list)
                    {
                        if (countInCol >= maxRows)
                        {
                            src.Items.Add(currentCol);
                            currentCol = new RibbonRowPanel();
                            countInCol = 0;
                        }
                        if (countInCol > 0)
                            currentCol.Items.Add(new RibbonRowBreak());

                        if (obj is KeyValuePair<MethodInfo, RibbonButtonAttribute> nb)
                            currentCol.Items.Add(CreateButton(nb.Key, nb.Value));
                        else if (obj is KeyValuePair<MethodInfo, RibbonDropDownAttribute> dd)
                            currentCol.Items.Add(CreateDropDown(dd.Key, dd.Value, dropItems));

                        countInCol++;
                    }

                    if (countInCol > 0) src.Items.Add(currentCol);

                    var panel = new RibbonPanel { Source = src };
                    tab.Panels.Add(panel);
                }
                tab.IsActive = true;
            }

            Application.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage($"\n[Plugin] Đã tạo {tabDict.Count} tab.");
        }

        // Hàm hỗ trợ
        private void AddToDict(Dictionary<string, Dictionary<string, List<object>>> dict,
                               string tab, string panel, object item)
        {
            if (!dict.ContainsKey(tab))
                dict[tab] = new Dictionary<string, List<object>>();
            if (!dict[tab].ContainsKey(panel))
                dict[tab][panel] = new List<object>();
            dict[tab][panel].Add(item);
        }

        private int GetOrder(object obj)
        {
            if (obj is KeyValuePair<MethodInfo, RibbonButtonAttribute> nb) return nb.Value.Order;
            if (obj is KeyValuePair<MethodInfo, RibbonDropDownAttribute> dd) return dd.Value.Order;
            return 0;
        }

        private RibbonButton CreateButton(MethodInfo m, RibbonButtonAttribute a)
        {
            var btn = new RibbonButton
            {
                Text = a.Text,
                ShowText = true,
                ShowImage = true,
                ToolTip = a.ToolTip,
                Size = a.Size,
                CommandParameter = m.Name,                 // ← quan trọng
                CommandHandler = new RibbonCommandHandler()
            };

            // Height chỉ cho Standard
            if (a.Size == RibbonItemSize.Standard)
                btn.Height = 24;

            // Ảnh
            var img = LoadImage(a.Icon, 16);
            if (img != null) btn.Image = img;

            var largeImg = LoadImage(a.LargeIcon ?? a.Icon, 32);
            if (largeImg != null) btn.LargeImage = largeImg;

            return btn;
        }

        private RibbonSplitButton CreateDropDown(MethodInfo m,
    RibbonDropDownAttribute a,
    List<KeyValuePair<MethodInfo, RibbonDropItemAttribute>> allItems)
        {
            // ═══════════════════════════════════════════════════════════
            // SPLITBUTTON — Nút cha + mũi tên
            // ═══════════════════════════════════════════════════════════
            var split = new RibbonSplitButton
            {
                Text = a.Text,
                ShowText = true,
                ShowImage = true,
                ToolTip = a.ToolTip,
                Size = a.Size,
                IsSplit = true,
                CommandHandler = new RibbonCommandHandler(),
                CommandParameter = m.Name
            };

            // ✅ Set Orientation Vertical cho Large
            if (a.Size == RibbonItemSize.Large)
            {
                split.Orientation = System.Windows.Controls.Orientation.Vertical;
                // KHÔNG set Height
            }
            else
            {
                split.Height = 24;
            }

            // ✅ Load ảnh đúng kích thước
            var smallImg = LoadImage(a.Icon, 16);
            var largeImg = LoadImage(a.LargeIcon ?? a.Icon, 32);

            if (smallImg != null) split.Image = smallImg;
            if (largeImg != null) split.LargeImage = largeImg;

            // ═══════════════════════════════════════════════════════════
            // NÚT CHA — Phần chính của SplitButton (bấm để chạy lệnh)
            // ═══════════════════════════════════════════════════════════
            var parentBtn = new RibbonButton
            {
                Text = a.Text,
                ShowText = true,
                ShowImage = true,
                ToolTip = a.ToolTip,
                Size = a.Size,
                CommandHandler = new RibbonCommandHandler(),
                CommandParameter = m.Name
            };

            // ✅ Nút cha PHẢI cùng cấu hình như SplitButton
            if (a.Size == RibbonItemSize.Large)
            {
                parentBtn.Orientation = System.Windows.Controls.Orientation.Vertical;
                // KHÔNG set Height
            }
            else
            {
                parentBtn.Height = 24;
            }

            if (smallImg != null) parentBtn.Image = smallImg;
            if (largeImg != null) parentBtn.LargeImage = largeImg;

            split.Current = parentBtn;

            // ═══════════════════════════════════════════════════════════
            // ITEM CON — Danh sách dropdown (luôn Standard)
            // ═══════════════════════════════════════════════════════════
            var children = new List<KeyValuePair<MethodInfo, RibbonDropItemAttribute>>();
            foreach (var it in allItems)
            {
                if (it.Value.Parent == a.Text)
                    children.Add(it);
            }
            children.Sort((x, y) => x.Value.Order.CompareTo(y.Value.Order));

            foreach (var c in children)
            {
                var childBtn = new RibbonButton
                {
                    Text = c.Value.Text,
                    ShowText = true,
                    ShowImage = true,
                    ToolTip = c.Value.ToolTip,
                    CommandHandler = new RibbonCommandHandler(),
                    CommandParameter = c.Key.Name,
                    Size = RibbonItemSize.Standard
                };

                // Ảnh cho item con — 16×16
                var ci = LoadImage(c.Value.Icon, 16);
                if (ci != null)
                {
                    childBtn.Image = ci;
                    childBtn.LargeImage = ci;
                }

                split.Items.Add(childBtn);
            }

            return split;
        }
        private int GetRowsPerColumn(object obj)
        {
            if (obj is KeyValuePair<MethodInfo, RibbonButtonAttribute> nb)
                return nb.Value.RowsPerColumn;

            if (obj is KeyValuePair<MethodInfo, RibbonDropDownAttribute> dd)
                return dd.Value.RowsPerColumn;

            return 3;
        }

        // Load ảnh với kích thước mục tiêu (16 hoặc 32)
        private System.Windows.Media.ImageSource LoadImage(string iconName, int targetSize = 16)
        {
            // ⚠️ Bọc TẤT CẢ trong try/catch — không bao giờ để crash
            try
            {
                if (string.IsNullOrEmpty(iconName))
                    return null;

                string path = Path.Combine(_imageFolder, iconName);
                if (!File.Exists(path))
                {
                    LogDebug($"Ảnh không tồn tại: {path}");
                    return null;
                }

                // ═══════════════════════════════════════════════════════════
                // ✅ ĐỌC ẢNH QUA FileStream — an toàn hơn UriSource
                // ═══════════════════════════════════════════════════════════
                System.Windows.Media.Imaging.BitmapImage src;

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    src = new System.Windows.Media.Imaging.BitmapImage();
                    src.BeginInit();
                    src.StreamSource = fs;
                    src.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    src.EndInit();
                }

                // ✅ Freeze ngay để thread-safe
                src.Freeze();

                // ═══════════════════════════════════════════════════════════
                // ✅ SCALE ẢNH
                // ═══════════════════════════════════════════════════════════
                double srcW = src.PixelWidth;
                double srcH = src.PixelHeight;

                if (srcW <= 0 || srcH <= 0)
                    return src;   // fallback

                double scale = Math.Min(targetSize / srcW, targetSize / srcH);
                double drawW = srcW * scale;
                double drawH = srcH * scale;

                double offsetX = (targetSize - drawW) / 2;
                double offsetY = (targetSize - drawH) / 2;

                // ✅ Vẽ ảnh đã scale
                var visual = new System.Windows.Media.DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    var destRect = new System.Windows.Rect(offsetX, offsetY, drawW, drawH);
                    dc.DrawImage(src, destRect);
                }

                // ⚠️ RenderTargetBitmap CHỈ chạy trên STA thread (UI thread)
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    targetSize, targetSize, 96, 96,
                    System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(visual);
                rtb.Freeze();

                return rtb;
            }
            catch (System.Exception ex)
            {
                // ✅ Ghi log — KHÔNG crash
                LogDebug($"Lỗi ảnh '{iconName}': {ex.Message}");
                return null;   // ← Quan trọng: return null thay vì throw
            }
        }

        // Helper log riêng cho RibbonBuilder
        private static void LogDebug(string msg)
        {
            try
            {
                string logPath = Path.Combine(Path.GetTempPath(), "CADAddin_debug.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] [RibbonBuilder] {msg}\n");
            }
            catch { }
        }
        private void LoadAllLisp()
        {
            if (!Directory.Exists(_lispFolder)) return;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Load tất cả file .lsp (kể cả thư mục con)
            var files = Directory.GetFiles(_lispFolder, "*.lsp", SearchOption.AllDirectories);

            foreach (var f in files)
            {
                // Dùng đường dẫn tuyệt đối + escape đúng
                string path = f.Replace("\\", "/");
                doc.SendStringToExecute($"(load \"{path}\") ", true, false, false);
            }

            doc.Editor.WriteMessage($"\n[Plugin] Đã load {files.Length} file LISP.");
        }
    }
}