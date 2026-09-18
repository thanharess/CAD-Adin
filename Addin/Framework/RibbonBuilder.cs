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

                // ===== 4. Duyệt Panel =====
                foreach (var panelKv in tabKv.Value)
                {
                    var src = new RibbonPanelSource { Title = panelKv.Key };

                    // Sort theo Order
                    var list = panelKv.Value;
                    list.Sort((a, b) => GetOrder(a).CompareTo(GetOrder(b)));

                    // Chia cột - tối đa 3 hàng/cột
                    const int MaxRows = 3;
                    RibbonRowPanel currentCol = new RibbonRowPanel();
                    int countInCol = 0;

                    foreach (var obj in list)
                    {
                        if (countInCol >= MaxRows)
                        {
                            src.Items.Add(currentCol);
                            currentCol = new RibbonRowPanel();
                            countInCol = 0;
                        }
                        if (countInCol > 0)
                            currentCol.Items.Add(new RibbonRowBreak());

                        // Tạo nút/ dropdown
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
            // ============================================================
            // 1. TẠO NÚT CHÍNH (SplitButton)
            // ============================================================
            var split = new RibbonSplitButton
            {
                Text = a.Text,
                ShowText = true,
                ShowImage = true,
                ToolTip = a.ToolTip,
                Size = a.Size,
                IsSplit = false,
                // ⚠️ KHÔNG set ListStyle (gây lỗi IconText/ListItem)
                // ⚠️ KHÔNG set ListImageSize (enum không tồn tại)
                CommandHandler = new RibbonCommandHandler(),
                CommandParameter = m.Name + " "
            };

            // ============================================================
            // 2. NÚT CHA "Box" — chỉ set Current, KHÔNG add vào Items
            // ============================================================
            var parentBtn = new RibbonButton
            {
                Text = a.Text,
                ShowText = true,
                ShowImage = true,
                ToolTip = a.ToolTip,
                Size = a.Size,
                CommandHandler = new RibbonCommandHandler(),
                CommandParameter = m.Name + " "
            };

            // ⬇️ CHỈ set Height cho nút Standard
            if (a.Size == RibbonItemSize.Standard)
                parentBtn.Height = 24;

            var pImg = LoadImage(a.Icon, 16);
            if (pImg != null) parentBtn.Image = pImg;

            var pLargeImg = LoadImage(a.LargeIcon ?? a.Icon, 32);
            if (pLargeImg != null) parentBtn.LargeImage = pLargeImg;

            split.Current = parentBtn;
            // ============================================================
            // 3. LỌC CÁC ITEM CON CÓ Parent KHỚP
            // ============================================================
            var children = new List<KeyValuePair<MethodInfo, RibbonDropItemAttribute>>();
            foreach (var it in allItems)
            {
                if (it.Value.Parent == a.Text)
                    children.Add(it);
            }
            children.Sort((x, y) => x.Value.Order.CompareTo(y.Value.Order));

            // ============================================================
            // 4. TẠO TỪNG ITEM CON + GÁN ẢNH
            // ============================================================
            foreach (var c in children)
            {
                var childBtn = new RibbonButton
                {
                    Text = c.Value.Text,
                    ShowText = true,
                    ShowImage = true,
                    ToolTip = c.Value.ToolTip,
                    CommandHandler = new RibbonCommandHandler(),
                    CommandParameter = c.Key.Name + " ",
                    Size = RibbonItemSize.Standard
                };

                // Ảnh nhỏ (16×16) cho item trong dropdown
                // Item con trong dropdown = 16×16
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

        
        // Load ảnh với kích thước mục tiêu (16 hoặc 32)
        private System.Windows.Media.ImageSource LoadImage(string iconName, int targetSize = 16)
        {
            if (string.IsNullOrEmpty(iconName)) return null;
            try
            {
                string path = Path.Combine(_imageFolder, iconName);
                if (!File.Exists(path)) return null;

                // ===== 1. Load ảnh gốc =====
                var src = new System.Windows.Media.Imaging.BitmapImage();
                src.BeginInit();
                src.UriSource = new Uri(path, UriKind.Absolute);
                src.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                src.EndInit();

                // ===== 2. Tính toán tỉ lệ để FIT (không crop, không méo) =====
                double srcW = src.PixelWidth;
                double srcH = src.PixelHeight;

                // Scale nhỏ hơn → Fit vào khung targetSize × targetSize
                double scale = Math.Min(targetSize / srcW, targetSize / srcH);
                double drawW = srcW * scale;
                double drawH = srcH * scale;

                // Căn giữa
                double offsetX = (targetSize - drawW) / 2;
                double offsetY = (targetSize - drawH) / 2;

                // ===== 3. Vẽ ảnh đã scale vào bitmap mới =====
                var visual = new System.Windows.Media.DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    var destRect = new System.Windows.Rect(offsetX, offsetY, drawW, drawH);
                    dc.DrawImage(src, destRect);
                }

                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    targetSize, targetSize, 96, 96,
                    System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(visual);
                rtb.Freeze();

                return rtb;
            }
            catch (System.Exception ex)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                    .MdiActiveDocument?.Editor.WriteMessage($"\n[Plugin] Lỗi ảnh: {ex.Message}");
                return null;
            }
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