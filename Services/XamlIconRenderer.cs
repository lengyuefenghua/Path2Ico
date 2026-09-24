using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Path2Ico.Services
{
    /// <summary>
    /// Renders a complete WPF/XAML vector fragment (Viewbox / Canvas / multiple
    /// Path elements with their own fills, strokes, opacities and effects) into a
    /// bitmap. Global style settings other than the background are ignored, since
    /// the fragment carries its own styling.
    /// </summary>
    public static class XamlIconRenderer
    {
        private const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        public static bool LooksLikeXaml(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string trimmed = text.TrimStart();
            return trimmed.Length > 0 && trimmed[0] == '<';
        }

        public static BitmapSource Render(string xaml, int size, RenderSettings settings)
        {
            if (string.IsNullOrWhiteSpace(xaml)) throw new ArgumentException("xaml");
            if (size <= 0) throw new ArgumentOutOfRangeException("size");
            if (settings == null) throw new ArgumentNullException("settings");

            var content = XamlReader.Parse(Normalize(xaml)) as FrameworkElement;
            if (content == null)
            {
                throw new InvalidOperationException("XAML 片段的根元素不是可视元素。");
            }

            var root = new Grid();
            if (settings.Background.A > 0)
            {
                root.Background = new SolidColorBrush(settings.Background);
            }

            root.Children.Add(new Viewbox
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both,
                Width = size,
                Height = size,
                Child = content
            });

            root.Measure(new Size(size, size));
            root.Arrange(new Rect(0, 0, size, size));
            root.UpdateLayout();

            var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(root);
            bitmap.Freeze();
            return bitmap;
        }

        private static string Normalize(string xaml)
        {
            int rootTagStart = FindRootTagStart(xaml);
            if (rootTagStart < 0) return xaml;

            int rootTagEnd = FindTagEnd(xaml, rootTagStart);
            if (rootTagEnd < 0) return xaml;

            string tag = xaml.Substring(rootTagStart, rootTagEnd - rootTagStart);
            string injection = string.Empty;

            if (tag.IndexOf("xmlns=", StringComparison.Ordinal) < 0)
            {
                injection += " xmlns=\"" + PresentationNamespace + "\"";
            }
            if (tag.IndexOf("xmlns:x=", StringComparison.Ordinal) < 0)
            {
                injection += " xmlns:x=\"" + XamlNamespace + "\"";
            }
            if (injection.Length == 0) return xaml;

            return xaml.Substring(0, rootTagEnd) + injection + xaml.Substring(rootTagEnd);
        }

        private static int FindRootTagStart(string xaml)
        {
            int i = 0;
            while (i < xaml.Length)
            {
                int lt = xaml.IndexOf('<', i);
                if (lt < 0) return -1;

                if (StartsWith(xaml, lt, "<!--"))
                {
                    int end = xaml.IndexOf("-->", lt + 4, StringComparison.Ordinal);
                    if (end < 0) return -1;
                    i = end + 3;
                    continue;
                }
                if (StartsWith(xaml, lt, "<?"))
                {
                    int end = xaml.IndexOf("?>", lt + 2, StringComparison.Ordinal);
                    if (end < 0) return -1;
                    i = end + 2;
                    continue;
                }
                return lt;
            }
            return -1;
        }

        private static int FindTagEnd(string xaml, int tagStart)
        {
            char quote = '\0';
            for (int i = tagStart + 1; i < xaml.Length; i++)
            {
                char c = xaml[i];
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    continue;
                }
                if (c == '"' || c == '\'')
                {
                    quote = c;
                    continue;
                }
                if (c == '>') return i;
            }
            return -1;
        }

        private static bool StartsWith(string text, int index, string value)
        {
            return index + value.Length <= text.Length
                && string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
        }
    }
}
