// Floor 01 picture: the grass and the wet stone path under the booth become clouds (Astra, 2026-10-20:
// "Get rid of the grass in the first floor. I just want it to be clouds instead."). The new clouds are the picture's
// own cloud sea, taken from both sides of the booth and enlarged (nearer clouds look bigger), so the style stays Codex's.
// Run (PowerShell): Add-Type -ReferencedAssemblies System.Drawing -Path cloud-floor-20261020.cs;
//   [CloudFloor]::Run("<grassy 01 png>", "<out png>", "01")   (made for 01-fox-pinkscape-20261015.png, 510 x 500 px)
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

public static class CloudFloor
{
    static double Smooth(double a, double b, double x) { double t = Math.Max(0, Math.Min(1, (x - a) / (b - a))); return t * t * (3 - 2 * t); }

    // the source strip: the cloud sea left of the booth (x 4..170) then right of it (x 345..505), cross-faded where they meet
    const int L0 = 4, L1 = 170, R0 = 345, R1 = 505, Fade = 14;
    static double[] Strip(Bitmap s, double u, double v)
    {
        double lw = L1 - L0;
        double[] a = null, b = null; double t = 0;
        if (u < lw - Fade / 2.0) a = Sample(s, L0 + u, v);
        else if (u > lw + Fade / 2.0) a = Sample(s, R0 + (u - lw), v);
        else
        {
            t = (u - (lw - Fade / 2.0)) / Fade;
            a = Sample(s, L0 + u, v); b = Sample(s, R0 + (u - lw), v);
            return new[] { a[0] * (1 - t) + b[0] * t, a[1] * (1 - t) + b[1] * t, a[2] * (1 - t) + b[2] * t };
        }
        return a;
    }

    static double[] Sample(Bitmap s, double x, double y)
    {
        x = Math.Max(0, Math.Min(s.Width - 1.001, x)); y = Math.Max(0, Math.Min(s.Height - 1.001, y));
        int x0 = (int)x, y0 = (int)y; double fx = x - x0, fy = y - y0;
        Color c00 = s.GetPixel(x0, y0), c10 = s.GetPixel(x0 + 1, y0), c01 = s.GetPixel(x0, y0 + 1), c11 = s.GetPixel(x0 + 1, y0 + 1);
        Func<int, int, int, int, double> lerp = (p, q, r, w) => (p * (1 - fx) + q * fx) * (1 - fy) + (r * (1 - fx) + w * fx) * fy;
        return new[] { lerp(c00.R, c10.R, c01.R, c11.R), lerp(c00.G, c10.G, c01.G, c11.G), lerp(c00.B, c10.B, c01.B, c11.B) };
    }

    // where the new clouds start (fully in a few pixels below): a soft, billowy line around y 330, lower in front of the booth
    static double Edge(int x)
    {
        double wave = 7 * Math.Sin(x * 0.045) + 4 * Math.Sin(x * 0.11 + 1.3) + 2.5 * Math.Sin(x * 0.23 + 0.4);
        if (x >= 177 && x <= 340)
        {
            double baseY = x < 258 ? 358 : 365;                      // the base sinks into the cloud: the old grass in front reaches up to ~372
            // round puffs in front of the base, so the booth sits in the clouds instead of on a straight line
            double puff = 0;
            foreach (var p in new double[][] { new double[] { 181, 13 }, new double[] { 204, 9 }, new double[] { 229, 11 }, new double[] { 255, 8 }, new double[] { 279, 10 }, new double[] { 304, 12 }, new double[] { 327, 9 }, new double[] { 341, 14 } })
                puff = Math.Max(puff, Math.Sqrt(Math.Max(0, p[1] * p[1] - (x - p[0]) * (x - p[0]))));
            return baseY + 3 - puff * 0.9 + 0.3 * wave;
        }
        double near = x < 177 ? 177 - x : x - 340;                    // grass tips stood up to ~320 right beside the posts
        return 329 + wave - 16 * Math.Max(0, 1 - near / 48.0);
    }

    public static void Run(string src, string dst, string label)
    {
        using (var s = new Bitmap(src))
        using (var o = new Bitmap(s.Width, s.Height, PixelFormat.Format24bppRgb))
        {
            const double Scale = 1.65, TopV = 206;                   // nearer clouds: the band from y 206 down, enlarged 1.65x
            for (int y = 0; y < s.Height; y++)
                for (int x = 0; x < s.Width; x++)
                {
                    Color c = s.GetPixel(x, y);
                    bool frame = x < 5 || x >= s.Width - 4 || y >= s.Height - 1;   // the picture's thin dark border
                    double e = Edge(x), a = frame ? 0 : Smooth(e - 9, e + 9, y);
                    if (a <= 0) { o.SetPixel(x, y, c); continue; }
                    double u = x / Scale, v = TopV + (y - 318) / Scale;
                    var t = Strip(s, u, v);
                    double lift = 0.10 + 0.10 * Smooth(380, 500, y);    // a little whiter the nearer they are, like the puffs bottom left
                    for (int k = 0; k < 3; k++) t[k] = t[k] + (255 - t[k]) * lift;
                    int r = (int)Math.Round(c.R * (1 - a) + t[0] * a), g = (int)Math.Round(c.G * (1 - a) + t[1] * a), b = (int)Math.Round(c.B * (1 - a) + t[2] * a);
                    o.SetPixel(x, y, Color.FromArgb(Math.Min(255, r), Math.Min(255, g), Math.Min(255, b)));
                }
            using (var gr = Graphics.FromImage(o))
            using (var f = new Font("Segoe UI", 27f, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                gr.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; gr.SmoothingMode = SmoothingMode.AntiAlias;
                using (var sh = new SolidBrush(Color.FromArgb(70, 60, 20, 60))) gr.DrawString(label, f, sh, 16.5f, 454f);
                gr.DrawString(label, f, Brushes.White, 15.5f, 453f);
            }
            o.Save(dst, ImageFormat.Png);
        }
    }
}
