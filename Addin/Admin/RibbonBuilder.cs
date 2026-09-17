using Autocad_addin.Framework.Autocad_addin.Framework;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Autocad_addin.Framework
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

            // ===== 1. Quét method có [RibbonButton] =====
            var found = new List<KeyValuePair<MethodInfo, RibbonButtonAttribute>>();
            foreach (var type in _asm.GetTypes())
            {
                foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var attr = m.GetCustomAttribute<RibbonButtonAttribute>();
                    if (attr != null)
                        found.Add(new KeyValuePair<MethodInfo, RibbonButtonAttribute>(m, attr));
                }
            }

            // ===== 2. Load LISP =====
            LoadAllLisp();

            // ===== 3. Group theo Tab =====
            var tabDict = new Dictionary<string,
                Dictionary<string, List<KeyValuePair<MethodInfo, RibbonButtonAttribute>>>>();

            foreach (var kv in found)
            {
                var a = kv.Value;
                if (!tabDict.ContainsKey(a.Tab))
                    tabDict[a.Tab] = new Dictionary<string, List<KeyValuePair<MethodInfo, RibbonButtonAttribute>>>();
                if (!tabDict[a.Tab].ContainsKey(a.Panel))
                    tabDict[a.Tab][a.Panel] = new List<KeyValuePair<MethodInfo, RibbonButtonAttribute>>();
                tabDict[a.Tab][a.Panel].Add(kv);
            }

            // ===== 4. Duyệt Tab =====
            foreach (var tabKv in tabDict)
            {
                string tabId = "TAB_" + tabKv.Key.Replace(" ", "_");
                var tab = ribbon.FindTab(tabId);
                if (tab == null)
                {
                    tab = new RibbonTab { Title = tabKv.Key, Id = tabId };
                    ribbon.Tabs.Add(tab);
                }

                // ===== 5. Duyệt Panel =====
                foreach (var panelKv in tabKv.Value)
                {
                    var src = new RibbonPanelSource { Title = panelKv.Key };

                    var list = panelKv.Value;
                    list.Sort((x, y) => x.Value.Order.CompareTo(y.Value.Order));

                    // ⬇️ DÙNG 1 ROWPANEL, CHÈN ROWBREAK GIỮA CÁC NÚT
                    var row = new RibbonRowPanel();

                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];

                        // Chèn RowBreak TRƯỚC nút thứ 2 trở đi
                        if (i > 0)
                            row.Items.Add(new RibbonRowBreak());

                        var btn = new RibbonButton
                        {
                            Text = item.Value.Text,
                            ShowText = true,
                            ShowImage = true,
                            ToolTip = item.Value.ToolTip,
                            Size = item.Value.Size,
                            CommandHandler = new RibbonCommandHandler(),
                            CommandParameter = item.Key.Name + " "
                        };

                        var img = LoadImage(item.Value.Icon);
                        if (img != null) btn.Image = img;

                        var largeImg = LoadImage(item.Value.LargeIcon ?? item.Value.Icon);
                        if (largeImg != null) btn.LargeImage = largeImg;

                        row.Items.Add(btn);
                    }

                    src.Items.Add(row);
                    var panel = new RibbonPanel { Source = src };
                    tab.Panels.Add(panel);
                }
                tab.IsActive = true;
            }

            Application.DocumentManager.MdiActiveDocument?
                .Editor.WriteMessage($"\n[Plugin] Đã tạo {tabDict.Count} tab, {found.Count} nút.");
        }

        private System.Windows.Media.ImageSource LoadImage(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return null;
            try
            {
                string path = Path.Combine(_imageFolder, iconName);
                if (!File.Exists(path)) return null;

                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        private void LoadAllLisp()
        {
            if (!Directory.Exists(_lispFolder)) return;
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            foreach (var f in Directory.GetFiles(_lispFolder, "*.lsp"))
                doc.SendStringToExecute($"(load \"{f.Replace("\\", "/")}\") ", true, false, false);
        }
    }
}