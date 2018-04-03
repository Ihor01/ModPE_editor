﻿using System.Drawing;
using System.Windows.Forms;

namespace NIDE.components
{
    public partial class FileTabControl : TabControl
    {
        public FileTabControl()
        {
            InitializeComponent();
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(120, 16);
            DrawMode = TabDrawMode.OwnerDrawFixed;
            DrawItem += FileTabControl_DrawItem;
            MouseDown += FileTabControl_MouseDown;
        }



        private void FileTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (TabCount > 1)
                e.Graphics.DrawString("x", e.Font, Brushes.Black, e.Bounds.Right - 15, e.Bounds.Top + 2);
            e.Graphics.DrawString(TabPages[e.Index].Text, e.Font, Brushes.Black, e.Bounds.Left + 12, e.Bounds.Top + 3);
            e.DrawFocusRectangle();
        }

        private void FileTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (TabCount <= 1) return;
            for (int i = 0; i < TabPages.Count; i++)
            {
                Rectangle r = GetTabRect(i);
                Rectangle closeButton = new Rectangle(r.Right - 15, r.Top + 4, 15, 9);
                if (closeButton.Contains(e.Location))
                {
                    if (((EditorTab)TabPages[i]).CanClose())
                    {
                        TabPages.RemoveAt(i);
                    }
                }
            }
        }

        public EditorTab Load(string file)
        {
            foreach (EditorTab tab in TabPages)
            {
                if (tab.File == file)
                {
                    SelectedTab = tab;
                    tab.Reload();
                    Refresh();
                    return tab;
                }
            }
            EditorTab newTab = new EditorTab(file);
            TabPages.Add(newTab);
            SelectedTab = newTab;
            return newTab;
        }

        public EditorTab LoadBlank(string file)
        {
            EditorTab newTab = new EditorTab(file);
            TabPages.Add(newTab);
            foreach(TabPage page in TabPages)
            {
                if (page != newTab)
                    TabPages.Remove(page);
            }
            return newTab;
        }
    }
}