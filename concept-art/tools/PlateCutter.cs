// Finds the thin uniform divider lines in a concept plate and crops the panels between them.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class PlateCutter
{
    static byte[] Pixels(Bitmap b, out int stride)
    {
        var d = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        stride = d.Stride; var px = new byte[stride * b.Height];
        Marshal.Copy(d.Scan0, px, 0, px.Length); b.UnlockBits(d); return px;
    }

    // Spans of rows (horizontal=true) or columns between x0..x1 / y0..y1 whose brightness is near-uniform: divider lines.
    public static string Uniform(string path, bool rows, int a0, int a1, double maxStd)
    {
        using (var src = new Bitmap(path)) using (var b = src.Clone(new Rectangle(0, 0, src.Width, src.Height), PixelFormat.Format24bppRgb))
        {
            int stride; var px = Pixels(b, out stride);
            int n = rows ? b.Height : b.Width;
            var hits = new List<string>(); int start = -1;
            for (int i = 0; i < n; i++)
            {
                double s = 0, s2 = 0; int c = 0;
                for (int j = a0; j < a1; j += 2)
                {
                    int x = rows ? j : i, y = rows ? i : j;
                    int o = y * stride + x * 3;
                    double l = 0.114 * px[o] + 0.587 * px[o + 1] + 0.299 * px[o + 2];
                    s += l; s2 += l * l; c++;
                }
                double mean = s / c, std = Math.Sqrt(Math.Max(0, s2 / c - mean * mean));
                bool u = std < maxStd;
                if (u && start < 0) start = i;
                if ((!u || i == n - 1) && start >= 0) { int end = u ? i : i - 1; hits.Add(start + "-" + end); start = -1; }
            }
            return string.Join(",", hits);
        }
    }

    public static void Crop(string path, int x, int y, int w, int h, string outPath)
    {
        using (var src = new Bitmap(path)) using (var part = src.Clone(new Rectangle(x, y, w, h), PixelFormat.Format24bppRgb))
            part.Save(outPath, ImageFormat.Png);
    }
}
