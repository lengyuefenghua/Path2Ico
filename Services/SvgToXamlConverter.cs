using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace Path2Ico.Services
{
    /// <summary>
    /// Converts a common subset of inline SVG markup into a WPF XAML fragment that
    /// can be rendered by <see cref="XamlIconRenderer"/>. Supported elements are
    /// svg/g/rect/circle/ellipse/line/polyline/polygon/path with basic presentation
    /// attributes (fill, stroke, opacity, stroke-width, line caps/joins) and the
    /// translate/scale/rotate/matrix/skew transforms. Gradients, use, clip paths,
    /// masks and filters are intentionally not supported.
    /// </summary>
    public static class SvgToXamlConverter
    {
        private const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static readonly Regex NumberRegex = new Regex(
            @"[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?",
            RegexOptions.Compiled);

        private static readonly Regex TransformRegex = new Regex(
            @"([a-zA-Z]+)\s*\(([^)]*)\)",
            RegexOptions.Compiled);

        public static bool LooksLikeSvg(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string trimmed = text.TrimStart();
            if (trimmed.Length == 0 || trimmed[0] != '<') return false;

            try
            {
                XDocument doc = XDocument.Parse(text);
                return doc.Root != null
                    && string.Equals(doc.Root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static string Convert(string svg)
        {
            if (string.IsNullOrWhiteSpace(svg)) throw new ArgumentException("svg");

            XDocument doc = XDocument.Parse(svg);
            XElement root = doc.Root;
            if (root == null || !string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("根元素不是 <svg>。");
            }

            double viewX, viewY, viewWidth, viewHeight;
            ResolveViewport(root, out viewX, out viewY, out viewWidth, out viewHeight);

            var rootMatrix = new Matrix(1, 0, 0, 1, -viewX, -viewY);

            var builder = new StringBuilder();
            builder.Append("<Canvas xmlns=\"").Append(PresentationNamespace).Append("\" ");
            builder.Append("xmlns:x=\"").Append(XamlNamespace).Append("\" ");
            builder.Append("Width=\"").Append(Format(viewWidth)).Append("\" ");
            builder.Append("Height=\"").Append(Format(viewHeight)).Append("\">");

            SvgStyle style = SvgStyle.CreateDefault();
            foreach (XElement child in root.Elements())
            {
                AppendElement(builder, child, style, rootMatrix);
            }

            builder.Append("</Canvas>");
            return builder.ToString();
        }

        private static void AppendElement(StringBuilder builder, XElement element, SvgStyle parentStyle, Matrix parentMatrix)
        {
            string name = element.Name.LocalName;
            if (name == null) return;

            if (!IsVisible(element)) return;

            switch (name.ToLowerInvariant())
            {
                case "defs":
                case "title":
                case "desc":
                case "metadata":
                case "style":
                    return;

                case "g":
                case "svg":
                case "a":
                {
                    SvgStyle style = ApplyStyle(parentStyle, element);
                    Matrix matrix = Combine(parentMatrix, GetValue(element, "transform"));
                    foreach (XElement child in element.Elements())
                    {
                        AppendElement(builder, child, style, matrix);
                    }
                    return;
                }

                case "rect":
                case "circle":
                case "ellipse":
                case "line":
                case "polyline":
                case "polygon":
                case "path":
                {
                    SvgStyle style = ApplyStyle(parentStyle, element);
                    Matrix matrix = Combine(parentMatrix, GetValue(element, "transform"));
                    Geometry geometry = BuildGeometry(element, name.ToLowerInvariant());
                    if (geometry == null) return;
                    AppendPath(builder, geometry, style, matrix, element);
                    return;
                }
            }
        }

        private static bool IsVisible(XElement element)
        {
            string display = GetValue(element, "display");
            if (string.Equals(display, "none", StringComparison.OrdinalIgnoreCase)) return false;

            string visibility = GetValue(element, "visibility");
            if (string.Equals(visibility, "hidden", StringComparison.OrdinalIgnoreCase)
                || string.Equals(visibility, "collapse", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            double opacity = ParseDouble(GetValue(element, "opacity"), 1);
            return opacity > 0;
        }

        private static void AppendPath(StringBuilder builder, Geometry geometry, SvgStyle style, Matrix matrix, XElement element)
        {
            string data = GeometryToData(geometry);
            if (string.IsNullOrEmpty(data)) return;

            builder.Append("<Path Data=\"").Append(Escape(data)).Append('"');

            Color fillColor;
            if (TryResolveColor(style.Fill, style.FillOpacity, out fillColor))
            {
                builder.Append(" Fill=\"").Append(ToHex(fillColor)).Append('"');
            }
            else
            {
                builder.Append(" Fill=\"{x:Null}\"");
            }

            Color strokeColor;
            if (TryResolveColor(style.Stroke, style.StrokeOpacity, out strokeColor))
            {
                builder.Append(" Stroke=\"").Append(ToHex(strokeColor)).Append('"');
                if (style.StrokeWidth > 0)
                {
                    builder.Append(" StrokeThickness=\"").Append(Format(style.StrokeWidth)).Append('"');
                }

                string cap = MapLineCap(style.LineCap);
                builder.Append(" StrokeStartLineCap=\"").Append(cap).Append('"');
                builder.Append(" StrokeEndLineCap=\"").Append(cap).Append('"');
                builder.Append(" StrokeLineJoin=\"").Append(MapLineJoin(style.LineJoin)).Append('"');
            }

            string fillRule = GetValue(element, "fill-rule");
            if (string.Equals(fillRule, "evenodd", StringComparison.OrdinalIgnoreCase))
            {
                builder.Append(" FillRule=\"EvenOdd\"");
            }

            if (style.Opacity < 1)
            {
                builder.Append(" Opacity=\"").Append(Format(style.Opacity)).Append('"');
            }

            if (!matrix.IsIdentity)
            {
                builder.Append(" RenderTransform=\"").Append(FormatMatrix(matrix)).Append('"');
            }

            builder.Append("/>");
        }

        private static Geometry BuildGeometry(XElement element, string name)
        {
            switch (name)
            {
                case "rect":
                {
                    double x = ParseDouble(GetValue(element, "x"), 0);
                    double y = ParseDouble(GetValue(element, "y"), 0);
                    double width = ParseDouble(GetValue(element, "width"), 0);
                    double height = ParseDouble(GetValue(element, "height"), 0);
                    if (width <= 0 || height <= 0) return null;

                    double rx = ParseDouble(GetValue(element, "rx"), 0);
                    double ry = ParseDouble(GetValue(element, "ry"), 0);
                    if (rx <= 0 && ry > 0) rx = ry;
                    if (ry <= 0 && rx > 0) ry = rx;

                    var rect = new Rect(x, y, width, height);
                    return (rx > 0 || ry > 0)
                        ? new RectangleGeometry(rect, rx, ry)
                        : new RectangleGeometry(rect);
                }

                case "circle":
                {
                    double cx = ParseDouble(GetValue(element, "cx"), 0);
                    double cy = ParseDouble(GetValue(element, "cy"), 0);
                    double r = ParseDouble(GetValue(element, "r"), 0);
                    if (r <= 0) return null;
                    return new EllipseGeometry(new Point(cx, cy), r, r);
                }

                case "ellipse":
                {
                    double cx = ParseDouble(GetValue(element, "cx"), 0);
                    double cy = ParseDouble(GetValue(element, "cy"), 0);
                    double rx = ParseDouble(GetValue(element, "rx"), 0);
                    double ry = ParseDouble(GetValue(element, "ry"), 0);
                    if (rx <= 0 || ry <= 0) return null;
                    return new EllipseGeometry(new Point(cx, cy), rx, ry);
                }

                case "line":
                {
                    double x1 = ParseDouble(GetValue(element, "x1"), 0);
                    double y1 = ParseDouble(GetValue(element, "y1"), 0);
                    double x2 = ParseDouble(GetValue(element, "x2"), 0);
                    double y2 = ParseDouble(GetValue(element, "y2"), 0);
                    return new LineGeometry(new Point(x1, y1), new Point(x2, y2));
                }

                case "polyline":
                case "polygon":
                    return BuildPolyGeometry(GetValue(element, "points"), name == "polygon");

                case "path":
                {
                    string d = GetValue(element, "d");
                    if (string.IsNullOrWhiteSpace(d)) return null;
                    try
                    {
                        return Geometry.Parse(SvgPathNormalizer.Normalize(d));
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        private static Geometry BuildPolyGeometry(string points, bool close)
        {
            double[] values = ParseNumbers(points);
            if (values.Length < 4) return null;

            var figure = new PathFigure { StartPoint = new Point(values[0], values[1]) };
            var segment = new PolyLineSegment { IsStroked = true };
            for (int i = 2; i + 1 < values.Length; i += 2)
            {
                segment.Points.Add(new Point(values[i], values[i + 1]));
            }

            figure.Segments.Add(segment);
            figure.IsClosed = close;

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

        private static SvgStyle ApplyStyle(SvgStyle parent, XElement element)
        {
            var style = new SvgStyle
            {
                Fill = parent.Fill,
                Stroke = parent.Stroke,
                FillOpacity = parent.FillOpacity,
                StrokeOpacity = parent.StrokeOpacity,
                StrokeWidth = parent.StrokeWidth,
                LineCap = parent.LineCap,
                LineJoin = parent.LineJoin,
                Opacity = parent.Opacity
            };

            string fill = GetValue(element, "fill");
            if (fill != null) style.Fill = fill;

            string stroke = GetValue(element, "stroke");
            if (stroke != null) style.Stroke = stroke;

            style.FillOpacity = Clamp01(ParseDouble(GetValue(element, "fill-opacity"), style.FillOpacity));
            style.StrokeOpacity = Clamp01(ParseDouble(GetValue(element, "stroke-opacity"), style.StrokeOpacity));
            style.StrokeWidth = Math.Max(0, ParseDouble(GetValue(element, "stroke-width"), style.StrokeWidth));

            string lineCap = GetValue(element, "stroke-linecap");
            if (lineCap != null) style.LineCap = lineCap;

            string lineJoin = GetValue(element, "stroke-linejoin");
            if (lineJoin != null) style.LineJoin = lineJoin;

            double opacity = Clamp01(ParseDouble(GetValue(element, "opacity"), 1));
            style.Opacity = Clamp01(parent.Opacity * opacity);

            return style;
        }

        private static void ResolveViewport(XElement root, out double x, out double y, out double width, out double height)
        {
            x = 0;
            y = 0;

            string viewBox = GetValue(root, "viewBox");
            if (!string.IsNullOrWhiteSpace(viewBox))
            {
                double[] values = ParseNumbers(viewBox);
                if (values.Length == 4 && values[2] > 0 && values[3] > 0)
                {
                    x = values[0];
                    y = values[1];
                    width = values[2];
                    height = values[3];
                    return;
                }
            }

            width = ParseDouble(GetValue(root, "width"), 0);
            height = ParseDouble(GetValue(root, "height"), 0);
            if (width <= 0) width = 64;
            if (height <= 0) height = 64;
        }

        private static Matrix Combine(Matrix parent, string transform)
        {
            if (string.IsNullOrWhiteSpace(transform)) return parent;
            Matrix matrix = parent;
            matrix.Append(ParseTransform(transform));
            return matrix;
        }

        private static Matrix ParseTransform(string transform)
        {
            var result = Matrix.Identity;
            foreach (Match match in TransformRegex.Matches(transform))
            {
                string name = match.Groups[1].Value;
                double[] args = ParseNumbers(match.Groups[2].Value);

                switch (name.ToLowerInvariant())
                {
                    case "translate":
                    {
                        double tx = args.Length > 0 ? args[0] : 0;
                        double ty = args.Length > 1 ? args[1] : 0;
                        result.Append(new Matrix(1, 0, 0, 1, tx, ty));
                        break;
                    }
                    case "scale":
                    {
                        double sx = args.Length > 0 ? args[0] : 1;
                        double sy = args.Length > 1 ? args[1] : sx;
                        result.Append(new Matrix(sx, 0, 0, sy, 0, 0));
                        break;
                    }
                    case "rotate":
                    {
                        double angle = args.Length > 0 ? args[0] : 0;
                        double radians = angle * Math.PI / 180.0;
                        double cos = Math.Cos(radians);
                        double sin = Math.Sin(radians);
                        if (args.Length >= 3)
                        {
                            double cx = args[1];
                            double cy = args[2];
                            result.Append(new Matrix(1, 0, 0, 1, cx, cy));
                            result.Append(new Matrix(cos, sin, -sin, cos, 0, 0));
                            result.Append(new Matrix(1, 0, 0, 1, -cx, -cy));
                        }
                        else
                        {
                            result.Append(new Matrix(cos, sin, -sin, cos, 0, 0));
                        }
                        break;
                    }
                    case "matrix":
                        if (args.Length >= 6)
                        {
                            result.Append(new Matrix(args[0], args[1], args[2], args[3], args[4], args[5]));
                        }
                        break;
                    case "skewx":
                        if (args.Length >= 1)
                        {
                            result.Append(new Matrix(1, 0, Math.Tan(args[0] * Math.PI / 180.0), 1, 0, 0));
                        }
                        break;
                    case "skewy":
                        if (args.Length >= 1)
                        {
                            result.Append(new Matrix(1, Math.Tan(args[0] * Math.PI / 180.0), 0, 1, 0, 0));
                        }
                        break;
                }
            }
            return result;
        }

        private static string GetValue(XElement element, string name)
        {
            string style = Attr(element, "style");
            if (!string.IsNullOrWhiteSpace(style))
            {
                foreach (string declaration in style.Split(';'))
                {
                    int colon = declaration.IndexOf(':');
                    if (colon < 0) continue;
                    string key = declaration.Substring(0, colon).Trim();
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return declaration.Substring(colon + 1).Trim();
                    }
                }
            }

            return Attr(element, name);
        }

        private static string Attr(XElement element, string name)
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                if (string.Equals(attribute.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return attribute.Value;
                }
            }
            return null;
        }

        private static bool TryParseColor(string value, out Color color)
        {
            color = Colors.Black;
            if (string.IsNullOrWhiteSpace(value)) return false;

            string text = value.Trim();
            if (text.StartsWith("#", StringComparison.Ordinal))
            {
                string hex = text.Substring(1);
                if (hex.Length == 3)
                {
                    hex = new string(new[] { hex[0], hex[0], hex[1], hex[1], hex[2], hex[2] });
                }
                if (hex.Length == 6)
                {
                    hex = "FF" + hex;
                }
                if (hex.Length != 8) return false;

                try
                {
                    color = (Color)ColorConverter.ConvertFromString("#" + hex);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                double[] values = ParseNumbers(text);
                if (values.Length < 3) return false;
                color = Color.FromRgb(ToByte(values[0]), ToByte(values[1]), ToByte(values[2]));
                return true;
            }

            try
            {
                object parsed = ColorConverter.ConvertFromString(text);
                if (parsed is Color)
                {
                    color = (Color)parsed;
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private static bool TryResolveColor(string value, double opacity, out Color color)
        {
            color = Colors.Black;
            if (string.IsNullOrWhiteSpace(value)) return false;

            string text = value.Trim();
            if (text.Equals("none", StringComparison.OrdinalIgnoreCase)) return false;
            if (text.Equals("transparent", StringComparison.OrdinalIgnoreCase)) return false;
            if (text.StartsWith("url(", StringComparison.OrdinalIgnoreCase)) return false;

            Color baseColor;
            if (!TryParseColor(text, out baseColor)) return false;

            double alpha = baseColor.A / 255.0 * Clamp01(opacity);
            color = Color.FromArgb((byte)Math.Round(alpha * 255), baseColor.R, baseColor.G, baseColor.B);
            return true;
        }

        private static string MapLineCap(string value)
        {
            if (string.Equals(value, "round", StringComparison.OrdinalIgnoreCase)) return "Round";
            if (string.Equals(value, "square", StringComparison.OrdinalIgnoreCase)) return "Square";
            return "Flat";
        }

        private static string MapLineJoin(string value)
        {
            if (string.Equals(value, "round", StringComparison.OrdinalIgnoreCase)) return "Round";
            if (string.Equals(value, "bevel", StringComparison.OrdinalIgnoreCase)) return "Bevel";
            return "Miter";
        }

        private static double[] ParseNumbers(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new double[0];

            MatchCollection matches = NumberRegex.Matches(text);
            var values = new List<double>(matches.Count);
            foreach (Match match in matches)
            {
                double value;
                if (double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    values.Add(value);
                }
            }
            return values.ToArray();
        }

        private static double ParseDouble(string text, double fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;

            double value;
            if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            double[] values = ParseNumbers(text);
            return values.Length > 0 ? values[0] : fallback;
        }

        private static byte ToByte(double value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (byte)Math.Round(value);
        }

        private static double Clamp01(double value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        private static string GeometryToData(Geometry geometry)
        {
            try
            {
                PathGeometry path = PathGeometry.CreateFromGeometry(geometry);
                var formattable = path as IFormattable;
                if (formattable != null)
                {
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                }
                return path.ToString();
            }
            catch
            {
                return geometry.ToString();
            }
        }

        private static string ToHex(Color color)
        {
            return "#" + color.A.ToString("X2") + color.R.ToString("X2")
                 + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private static string FormatMatrix(Matrix matrix)
        {
            return string.Join(",", new[]
            {
                Format(matrix.M11), Format(matrix.M12),
                Format(matrix.M21), Format(matrix.M22),
                Format(matrix.OffsetX), Format(matrix.OffsetY)
            });
        }

        private static string Format(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private sealed class SvgStyle
        {
            public string Fill;
            public string Stroke;
            public double FillOpacity;
            public double StrokeOpacity;
            public double StrokeWidth;
            public string LineCap;
            public string LineJoin;
            public double Opacity;

            public static SvgStyle CreateDefault()
            {
                return new SvgStyle
                {
                    Fill = "black",
                    Stroke = "none",
                    FillOpacity = 1,
                    StrokeOpacity = 1,
                    StrokeWidth = 1,
                    LineCap = "butt",
                    LineJoin = "miter",
                    Opacity = 1
                };
            }
        }
    }
}
