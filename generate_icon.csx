using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

class IconGenerator
{
    static void Main()
    {
        var sizes = new[] { 16, 32, 48, 256 };
        var bitmaps = new List<Bitmap>();

        foreach (var size in sizes)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float scale = (float)size / 256f;
                float cx = size / 2f;
                float cy = size / 2f;

                // Background circle (dark)
                using (var bgBrush = new SolidBrush(Color.FromArgb(255, 30, 30, 30)))
                using (var bgPen = new Pen(Color.FromArgb(255, 80, 80, 80), 2 * scale))
                {
                    float radius = 110 * scale;
                    g.FillEllipse(bgBrush, cx - radius, cy - radius, radius * 2, radius * 2);
                    g.DrawEllipse(bgPen, cx - radius, cy - radius, radius * 2, radius * 2);
                }

                // Camera body
                using (var bodyBrush = new SolidBrush(Color.FromArgb(255, 100, 100, 100)))
                using (var bodyPen = new Pen(Color.FromArgb(255, 200, 200, 200), 2 * scale))
                {
                    float bw = 140 * scale, bh = 90 * scale;
                    float bx = cx - bw / 2, by = cy - bh / 2 + 5 * scale;
                    var rect = new RectangleF(bx, by, bw, bh);
                    using (var path = GetRoundedRect(rect, 8 * scale))
                    {
                        g.FillPath(bodyBrush, path);
                        g.DrawPath(bodyPen, path);
                    }
                }

                // Camera top bump (viewfinder)
                using (var bumpBrush = new SolidBrush(Color.FromArgb(255, 80, 80, 80)))
                {
                    float bumpW = 50 * scale, bumpH = 20 * scale;
                    float bumpX = cx - bumpW / 2, bumpY = cy - bh(90 * scale) / 2 + 5 * scale - bumpH + 2 * scale;
                    g.FillRectangle(bumpBrush, bumpX, bumpY, bumpW, bumpH);
                }

                // Lens outer ring
                float lensR = 40 * scale;
                using (var lensRingBrush = new SolidBrush(Color.FromArgb(255, 50, 50, 50)))
                using (var lensRingPen = new Pen(Color.FromArgb(255, 180, 180, 180), 3 * scale))
                {
                    g.FillEllipse(lensRingBrush, cx - lensR, cy - lensR + 5 * scale, lensR * 2, lensR * 2);
                    g.DrawEllipse(lensRingPen, cx - lensR, cy - lensR + 5 * scale, lensR * 2, lensR * 2);
                }

                // Lens glass gradient
                using (var lensBrush = new RadialGradientBrush(
                    new PointF(cx, cy + 5 * scale),
                    new PointF(cx, cy + 5 * scale + lensR),
                    Color.FromArgb(255, 60, 160, 240),
                    Color.FromArgb(255, 20, 60, 120)))
                {
                    g.FillEllipse(lensBrush, cx - lensR + 3 * scale, cy - lensR + 8 * scale, (lensR - 3 * scale) * 2, (lensR - 3 * scale) * 2);
                }

                // Lens highlight
                using (var highlightBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255)))
                {
                    float hlR = 14 * scale;
                    g.FillEllipse(highlightBrush, cx - hlR - 8 * scale, cy - hlR - 2 * scale, hlR * 2, hlR * 2);
                }

                // Record dot
                using (var dotBrush = new SolidBrush(Color.FromArgb(255, 220, 50, 50)))
                {
                    float dotR = 6 * scale;
                    float dotX = cx + 60 * scale;
                    float dotY = cy - 20 * scale;
                    g.FillEllipse(dotBrush, dotX - dotR, dotY - dotR, dotR * 2, dotR * 2);
                }
            }
            bitmaps.Add(bmp);
        }

        // Save as ICO
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "CameraStreaming", "Icons");
        Directory.CreateDirectory(dir);
        string icoPath = Path.Combine(dir, "app.ico");

        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            // ICO header
            writer.Write((short)0);     // Reserved
            writer.Write((short)1);     // Type: icon
            writer.Write((short)sizes.Length); // Count

            // Calculate offsets
            int headerSize = 6 + sizes.Length * 16;
            int dataOffset = headerSize;
            var imageData = new List<byte[]>();

            foreach (var bmp in bitmaps)
            {
                var pngData = BmpToPngBytes(bmp);
                imageData.Add(pngData);

                // Directory entry
                writer.Write((byte)(bmp.Width >= 256 ? 0 : bmp.Width));
                writer.Write((byte)(bmp.Height >= 256 ? 0 : bmp.Height));
                writer.Write((byte)0);   // Color palette
                writer.Write((byte)0);   // Reserved
                writer.Write((short)1);  // Color planes
                writer.Write((short)32); // Bits per pixel
                writer.Write(pngData.Length); // Size of data
                writer.Write(dataOffset);     // Offset
                dataOffset += pngData.Length;
            }

            // Write image data
            foreach (var data in imageData)
            {
                writer.Write(data);
            }

            File.WriteAllBytes(icoPath, ms.ToArray());
        }

        foreach (var bmp in bitmaps) bmp.Dispose();
        Console.WriteLine($"Icon saved to: {icoPath}");
    }

    static float bh(float v) => v;

    static GraphicsPath GetRoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float r = Math.Min(radius, rect.Width / 2, rect.Height / 2);
        path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
        path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
        path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        return path;
    }

    static byte[] BmpToPngBytes(Bitmap bmp)
    {
        using (var ms = new MemoryStream())
        {
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
    }

    class RadialGradientBrush : Brush
    {
        private readonly PointF _center1, _center2;
        private readonly Color _color1, _color2;

        public RadialGradientBrush(PointF center1, PointF center2, Color color1, Color color2)
        {
            _center1 = center1;
            _center2 = center2;
            _color1 = color1;
            _color2 = color2;
        }

        protected override void Dispose(bool disposing) { }
    }
}
