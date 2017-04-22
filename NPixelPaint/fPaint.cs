﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Keyboard = System.Windows.Input.Keyboard;
using Key = System.Windows.Input.Key;
using KeyStates = System.Windows.Input.KeyStates;

namespace NPixelPaint
{
    public partial class fPaint : Form
    {
        string path;
        int width;
        int height;

        int scale = 21;

        Color[,] pixels;
        Bitmap png;
        bool saved = true;
        bool mouseDown = false;

        Random rnd;
        Point startPoint;

        List<Color[,]> UndoBuffer = new List<Color[,]>();
        List<Color[,]> RedoBuffer = new List<Color[,]>();


        public fPaint(string path)
        {
            InitializeComponent();
            panel.MouseWheel += DrawPanel_MouseWheel;
            rnd = new Random();
            this.path = path;
            try
            {
                png = new Bitmap(path);
                width = png.Width;
                height = png.Height;
                AdjustDrawPanel();
                pixels = new Color[width, height];
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        pixels[i, j] = png.GetPixel(i, j);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                png?.Dispose();
                png = new Bitmap(width, height);
            }
        }

        private void AdjustDrawPanel()
        {
            DrawPanel.Width = scale * width - 1;
            DrawPanel.Height = scale * height - 1;
        }

        private void DrawPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) > 0)
            {
                scale += e.Delta / 100;
                if (scale < 2) scale = 2;
                if (scale > 40) scale = 40;
                AdjustDrawPanel();
                DrawPanel.Refresh();
            }
        }

        private void fPngEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            png.Dispose();
        }


        //Tools
        private void tsbDraw_Click(object sender, EventArgs e)
        {
            UncheckAll();
            tsbDraw.Checked = true;
        }

        private void tsbClear_Click(object sender, EventArgs e)
        {
            UncheckAll();
            tsbClear.Checked = true;
        }

        private void tsbPicker_Click(object sender, EventArgs e)
        {
            UncheckAll();
            tsbPicker.Checked = true;
        }

        private void tsbFill_Click(object sender, EventArgs e)
        {
            UncheckAll();
            tsbFill.Checked = true;
        }

        private void tsbTexturize_Click(object sender, EventArgs e)
        {
            UncheckAll();
            tsbTexturize.Checked = true;
        }

        private void tsbRectangle_Click(object sender, EventArgs e)
        {
            UncheckAll();
            tsbRectangle.Checked = true;
        }

        private void UncheckAll()
        {
            tsbDraw.Checked = false;
            tsbPicker.Checked = false;
            tsbClear.Checked = false;
            tsbFill.Checked = false;
            tsbTexturize.Checked = false;
            tsbRectangle.Checked = false;
        }


        //Fileworking
        private void fPngEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!saved)
            {
                var result = MessageBox.Show("Do you want to save changes?", "Confirmation", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes)
                {
                    save();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true; ;
                }
            }
        }

        private void tsbSave_Click(object sender, EventArgs e)
        {
            save();
        }

        private void save()
        {
            png.Dispose();
            png = new Bitmap(width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    png.SetPixel(i, j, pixels[i, j]);
                }
            }
            try
            {
                png.Save(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                png.Dispose();
            }
            saved = true;
        }

        private void tsbOpen_Click(object sender, EventArgs e)
        {
            dlgOpen.ShowDialog();
        }

        private void dlgOpen_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Bitmap bmp = new Bitmap(dlgOpen.FileName);
            if (bmp.Width == width && bmp.Height == height)
            {
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        pixels[i, j] = bmp.GetPixel(i, j);
                    }
                }
            }
        }


        //Painting
        private void DrawPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen coordsPen = new Pen(Color.LightGray);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    g.FillRectangle(new SolidBrush(pixels[i, j]), i * scale + 1, j * scale + 1, scale, scale);
                }
            }

            for (var i = 1; i < width; i++)
            {
                int pos = i * scale;
                g.DrawLine(coordsPen, pos, 0, pos, DrawPanel.Width);
            }
            for (var i = 1; i < height; i++)
            {
                int pos = i * scale;
                g.DrawLine(coordsPen, 0, pos, DrawPanel.Height, pos);
            }
        }

        private void DrawPanel_Click(object sender, EventArgs e)
        {
            saved = false;
            int x = GetCursorX();
            int y = GetCursorY();
            if (tsbPicker.Checked)
            {
                tsbColorPicker.Color = pixels[x, y];
                tsbPicker.Checked = false;
                tsbDraw.Checked = true;
            }
            else if (tsbFill.Checked || tsbTexturize.Checked)
            {
                BackupForUndo();
                if (tsbFill.Checked)
                    FillRecursive(x, y);
                else if (tsbTexturize.Checked)
                    TexturizeRecursive(x, y);
                DrawPanel.Refresh();
            }
        }

        private void DrawPanel_MouseDown(object sender, MouseEventArgs e)
        {
            BackupForUndo();
            mouseDown = true;
            startPoint = new Point(GetCursorX(), GetCursorY());
            DrawPanel_MouseMove(sender, e);
        }

        private void DrawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                saved = false;
                int x = GetCursorX();
                int y = GetCursorY();
                if (tsbDraw.Checked)
                    pixels[x, y] = tsbColorPicker.Color;
                else if (tsbClear.Checked)
                    pixels[x, y] = Color.Transparent;
                else if (tsbRectangle.Checked)
                {
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            pixels[i, j] = UndoBuffer[UndoBuffer.Count - 1][i, j];
                        }
                    }
                    int sx = Math.Min(startPoint.X, x);
                    int fx = Math.Max(startPoint.X, x);
                    int sy = Math.Min(startPoint.Y, y);
                    int fy = Math.Max(startPoint.Y, y);
                    for (int i = sx; i <= fx; i++)
                    {
                        for (int j = sy; j <= fy; j++)
                        {
                            pixels[i, j] = tsbColorPicker.Color;
                        }
                    }
                }
                DrawPanel.Refresh();
            }
        }

        private void DrawPanel_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }


        private void TexturizeRecursive(int x, int y)
        {
            Color prevColor = pixels[x, y];
            int darkness = rnd.Next(-30, 30);
            int A = prevColor.A + darkness;
            if (A > 255) A = 255;
            if (A < 0) A = 0;
            int R = prevColor.R + darkness;
            if (R > 255) R = 255;
            if (R < 0) R = 0;
            int G = prevColor.G + darkness;
            if (G > 255) G = 255;
            if (G < 0) G = 0;
            int B = prevColor.B + darkness;
            if (B > 255) B = 255;
            if (B < 0) B = 0;

            pixels[x, y] = Color.FromArgb(A, R, G, B);
            if (x > 0 && pixels[x - 1, y] == prevColor)
                TexturizeRecursive(x - 1, y);
            if (x < width - 1 && pixels[x + 1, y] == prevColor)
                TexturizeRecursive(x + 1, y);
            if (y > 0 && pixels[x, y - 1] == prevColor)
                TexturizeRecursive(x, y - 1);
            if (y < height - 1 && pixels[x, y + 1] == prevColor)
                TexturizeRecursive(x, y + 1);
        }

        private void FillRecursive(int x, int y)
        {
            Color prevColor = pixels[x, y];
            if (tsbColorPicker.Color == prevColor)
                return;
            pixels[x, y] = tsbColorPicker.Color;
            if (x > 0 && pixels[x - 1, y] == prevColor)
                FillRecursive(x - 1, y);
            if (x < width - 1 && pixels[x + 1, y] == prevColor)
                FillRecursive(x + 1, y);
            if (y > 0 && pixels[x, y - 1] == prevColor)
                FillRecursive(x, y - 1);
            if (y < height - 1 && pixels[x, y + 1] == prevColor)
                FillRecursive(x, y + 1);
        }



        private void tsbUndo_Click(object sender, EventArgs e)
        {
            Color[,] last = UndoBuffer[UndoBuffer.Count - 1];
            RedoBuffer.Add(pixels);
            tsbRedo.Enabled = true;
            pixels = last;
            UndoBuffer.Remove(last);
            DrawPanel.Refresh();
            if (UndoBuffer.Count <= 0)
                tsbUndo.Enabled = false;
        }

        private void tsbRedo_Click(object sender, EventArgs e)
        {
            Color[,] last = RedoBuffer[RedoBuffer.Count - 1];
            UndoBuffer.Add(pixels);
            tsbUndo.Enabled = true;
            pixels = last;
            RedoBuffer.Remove(last);
            DrawPanel.Refresh();
            if (RedoBuffer.Count <= 0)
                tsbRedo.Enabled = false;
        }

        private void BackupForUndo()
        {
            var n = new Color[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    n[i, j] = pixels[i, j];
                }
            }
            UndoBuffer.Add(n);
            RedoBuffer.Clear();
            tsbRedo.Enabled = false;
            tsbUndo.Enabled = true;
        }


        int GetCursorX()
        {
            int cursorX = Cursor.Position.X - DrawPanel.PointToScreen(Point.Empty).X;
            int x = cursorX / scale;
            if (x < 0) x = 0;
            if (x > width - 1) x = width - 1;
            return x;
        }
        int GetCursorY()
        {
            int cursorY = Cursor.Position.Y - DrawPanel.PointToScreen(Point.Empty).Y;
            int y = cursorY / scale;
            if (y < 0) y = 0;
            if (y > height - 1) y = height - 1;
            return y;
        }
    }
}