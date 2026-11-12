using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Path2Ico.Services
{
    /// <summary>
    /// Writes a same-name SVG sidecar next to an exported image. Path mode is
    /// rebuilt from the geometry and current render settings so the SVG matches
    /// the preview; SVG input is stored as-is.
    /// </summary>
    public static class SvgExporter
    {
        private const int CanvasSize = 256;

        public static void SaveFromPath(Geometry geometry, RenderSettings settings, string path)
        {
            if (geometry == null) throw new ArgumentNullException("geometry");
            if (settings == null) throw new ArgumentNullException("settings");

            Rect bounds = geometry.Bounds;
            double width = bounds.Width;
            double height = bounds.Height;
            if (width <= 0) width = height;
            if (height <= 0) height = width;

            double padding = CanvasSize * Clamp(settings.PaddingRatio, 0, 0.45);
            double available = CanvasSize - padding * 2;
            double scale = width > 0 && height > 0 ? Math.Min(available / width, available / height) : 1;
            if (scale <= 0) scale = 1;
            double offsetX = padding + (available - width * scale) / 2 - bounds.X * scale;
            double offsetY = padding + (available - height * scale) / 2 - bounds.Y * scale;

            var svg = new StringBuilder();
            svg.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"256\" height=\"256\" viewBox=\"0 0 256 256\">\n");
            svg.Append("  <g transform=\"translate(").Append(Fmt(offsetX)).Append(' ').Append(Fmt(offsetY))
               .Append(") scale(").Append(Fmt(scale)).Append(")\">\n");
            svg.Append("    <path d=\"").Append(ToSvgPathData(geometry)).Append('"');
            AppendFill(svg, geometry, settings);
            AppendStroke(svg, settings, scale);
            svg.Append(" />\n");
            svg.Append("  </g>\n");
            svg.Append("</svg>\n");

            File.WriteAllText(path, svg.ToString(), new UTF8Encoding(false));
        }

        public static void SaveOriginal(string svg, string path)
        {
            if (string.IsNullOrWhiteSpace(svg)) throw new ArgumentException("svg");
            File.WriteAllText(path, svg, new UTF8Encoding(false));
        }

        private static void AppendFill(StringBuilder svg, Geometry geometry, RenderSettings settings)
        {
            string fill = settings.Fill is SolidColorBrush brush ? ToSvgColor(brush.Color) : "none";
            svg.Append(" fill=\"").Append(fill).Append('"');
            if (PathGeometry.CreateFromGeometry(geometry).FillRule == FillRule.EvenOdd)
            {
                svg.Append(" fill-rule=\"evenodd\"");
            }
        }

        private static void AppendStroke(StringBuilder svg, RenderSettings settings, double scale)
        {
            if (!settings.HasStroke || settings.StrokeThickness <= 0)
            {
                return;
            }

            svg.Append(" stroke=\"").Append(ToSvgColor(settings.StrokeColor)).Append('"');
            svg.Append(" stroke-width=\"").Append(Fmt(settings.StrokeThickness / scale)).Append('"');
            svg.Append(" stroke-linecap=\"round\" stroke-linejoin=\"round\"");
        }

        private static string ToSvgPathData(Geometry geometry)
        {
            PathGeometry path = PathGeometry.CreateFromGeometry(geometry);
            var data = new StringBuilder();

            foreach (PathFigure figure in path.Figures)
            {
                data.Append('M');
                AppendPoint(data, figure.StartPoint);

                foreach (PathSegment segment in figure.Segments)
                {
                    if (segment is LineSegment line)
                    {
                        data.Append(" L");
                        AppendPoint(data, line.Point);
                    }
                    else if (segment is PolyLineSegment polyLine)
                    {
                        foreach (Point point in polyLine.Points)
                        {
                            data.Append(" L");
                            AppendPoint(data, point);
                        }
                    }
                    else if (segment is BezierSegment bezier)
                    {
                        data.Append(" C");
                        AppendPoint(data, bezier.Point1);
                        AppendPoint(data, bezier.Point2);
                        AppendPoint(data, bezier.Point3);
                    }
                    else if (segment is PolyBezierSegment polyBezier)
                    {
                        data.Append(" C");
                        foreach (Point point in polyBezier.Points)
                        {
                            AppendPoint(data, point);
                        }
                    }
                    else if (segment is QuadraticBezierSegment quadratic)
                    {
                        data.Append(" Q");
                        AppendPoint(data, quadratic.Point1);
                        AppendPoint(data, quadratic.Point2);
                    }
                    else if (segment is PolyQuadraticBezierSegment polyQuadratic)
                    {
                        data.Append(" Q");
                        foreach (Point point in polyQuadratic.Points)
                        {
                            AppendPoint(data, point);
                        }
                    }
                    else if (segment is ArcSegment arc)
                    {
                        data.Append(" A")
                            .Append(Fmt(arc.Size.Width)).Append(' ')
                            .Append(Fmt(arc.Size.Height)).Append(' ')
                            .Append(Fmt(arc.RotationAngle)).Append(' ')
                            .Append(arc.IsLargeArc ? '1' : '0').Append(' ')
                            .Append(arc.SweepDirection == SweepDirection.Clockwise ? '1' : '0');
                        AppendPoint(data, arc.Point);
                    }
                }

                if (figure.IsClosed)
                {
                    data.Append(" Z");
                }
            }

            return data.ToString();
        }

        private static void AppendPoint(StringBuilder data, Point point)
        {
            data.Append(' ').Append(Fmt(point.X)).Append(' ').Append(Fmt(point.Y));
        }

        private static string ToSvgColor(Color color)
        {
            if (color.A == 255)
            {
                return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "rgba({0},{1},{2},{3})",
                color.R,
                color.G,
                color.B,
                Fmt(color.A / 255.0));
        }

        private static string Fmt(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
