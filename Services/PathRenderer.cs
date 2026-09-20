using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Path2Ico.Services
{
    public sealed class RenderSettings
    {
        public Color Background { get; set; }
        public Brush Fill { get; set; }
        public bool HasStroke { get; set; }
        public Color StrokeColor { get; set; }
        public double StrokeThickness { get; set; }
        public double PaddingRatio { get; set; }
    }

    public static class PathRenderer
    {
        public static BitmapSource Render(Geometry geometry, int size, RenderSettings settings)
        {
            if (geometry == null) throw new ArgumentNullException("geometry");
            if (size <= 0) throw new ArgumentOutOfRangeException("size");
            if (settings == null) throw new ArgumentNullException("settings");

            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                if (settings.Background.A > 0)
                {
                    dc.DrawRectangle(new SolidColorBrush(settings.Background), null, new Rect(0, 0, size, size));
                }

                Rect bounds = geometry.Bounds;
                double width = bounds.Width;
                double height = bounds.Height;
                if (width <= 0) width = height;
                if (height <= 0) height = width;

                if (width > 0 && height > 0)
                {
                    double padding = size * Clamp(settings.PaddingRatio, 0, 0.45);
                    double available = size - padding * 2;
                    double scale = Math.Min(available / width, available / height);
                    double offsetX = padding + (available - width * scale) / 2 - bounds.X * scale;
                    double offsetY = padding + (available - height * scale) / 2 - bounds.Y * scale;

                    dc.PushTransform(new MatrixTransform(new Matrix(scale, 0, 0, scale, offsetX, offsetY)));

                    Pen pen = null;
                    if (settings.HasStroke && settings.StrokeThickness > 0)
                    {
                        pen = new Pen(new SolidColorBrush(settings.StrokeColor), settings.StrokeThickness / scale)
                        {
                            LineJoin = PenLineJoin.Round,
                            StartLineCap = PenLineCap.Round,
                            EndLineCap = PenLineCap.Round
                        };
                    }

                    if (settings.Fill != null)
                    {
                        dc.DrawGeometry(settings.Fill, pen, geometry);
                    }
                    else if (pen != null)
                    {
                        dc.DrawGeometry(null, pen, geometry);
                    }

                    dc.Pop();
                }
            }

            var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
