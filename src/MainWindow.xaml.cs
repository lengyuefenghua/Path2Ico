using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Path2Ico.Services;

namespace Path2Ico
{
    public partial class MainWindow : Window
    {
        private const int PreviewSize = 320;
        private const int JpegQuality = 90;

        private const string SamplePath =
            "M 50,5 L 61,38 L 95,38 L 67,59 L 78,92 L 50,71 L 22,92 L 33,59 L 5,38 L 39,38 Z";

        private bool _ready;
        private CheckBox[] _icoSizeBoxes;

        public MainWindow()
        {
            InitializeComponent();
            FitToWorkArea();

            _icoSizeBoxes = new[] { Ico16, Ico20, Ico24, Ico32, Ico40, Ico48, Ico64, Ico96, Ico128, Ico256 };

            PathTextBox.Text = SamplePath;

            _ready = true;
            RefreshPreview();
        }

        private void FitToWorkArea()
        {
            Rect workArea = SystemParameters.WorkArea;
            Width = Math.Min(Width, workArea.Width);
            Height = Math.Min(Height, workArea.Height);
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshPreview();
        }

        private void OnCheckChanged(object sender, RoutedEventArgs e)
        {
            RefreshPreview();
        }

        private void OnSampleClick(object sender, RoutedEventArgs e)
        {
            PathTextBox.Text = SamplePath;
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            PathTextBox.Clear();
        }

        private void RefreshPreview()
        {
            if (!_ready) return;

            string text = PathTextBox.Text;
            bool isXaml = XamlIconRenderer.LooksLikeXaml(text);

            try
            {
                BitmapSource preview;
                if (isXaml)
                {
                    preview = XamlIconRenderer.Render(text, PreviewSize, BuildSettings(Colors.Transparent));
                }
                else
                {
                    Geometry geometry = BuildGeometry();
                    if (geometry == null)
                    {
                        PreviewImage.Source = null;
                        return;
                    }
                    preview = PathRenderer.Render(geometry, PreviewSize, BuildSettings(Colors.Transparent));
                }

                PreviewImage.Source = preview;
            }
            catch
            {
                // 预览失败时静默清空，避免输入过程中反复弹窗。
                PreviewImage.Source = null;
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;

            string text = PathTextBox.Text;
            bool isXaml = XamlIconRenderer.LooksLikeXaml(text);

            Geometry geometry = null;
            if (!isXaml)
            {
                geometry = BuildGeometry();
                if (geometry == null)
                {
                    ShowError("路径解析失败，无法生成。");
                    return;
                }
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "保存图标",
                Filter = "PNG 图片 (*.png)|*.png|JPEG 图片 (*.jpg)|*.jpg|图标文件 (*.ico)|*.ico",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != true) return;

            string path = dialog.FileName;
            string extension = Path.GetExtension(path).ToLowerInvariant();

            Func<int, Color, BitmapSource> render = (size, background) =>
            {
                RenderSettings settings = BuildSettings(background);
                return isXaml
                    ? XamlIconRenderer.Render(text, size, settings)
                    : PathRenderer.Render(geometry, size, settings);
            };

            try
            {
                if (extension == ".png")
                {
                    int size = GetOutputSize();
                    ImageExporter.SavePng(render(size, Colors.Transparent), path);
                }
                else if (extension == ".jpg" || extension == ".jpeg")
                {
                    int size = GetOutputSize();
                    ImageExporter.SaveJpeg(render(size, Colors.White), path, JpegQuality);
                }
                else if (extension == ".ico")
                {
                    List<int> icoSizes = GetSizes(_icoSizeBoxes).ToList();
                    if (icoSizes.Count == 0)
                    {
                        ShowError("ICO 未保存：请至少选择一个内嵌尺寸。");
                        return;
                    }

                    var images = icoSizes.Select(size => render(size, Colors.Transparent)).ToList();
                    IcoWriter.Save(images, path);
                }
                else
                {
                    ShowError("不支持的格式，请使用 .png、.jpg 或 .ico 扩展名。");
                }
            }
            catch (Exception ex)
            {
                ShowError("生成失败：" + ex.Message);
                return;
            }

            SaveSvgSidecar(path, isXaml, text, geometry);
        }

        private void SaveSvgSidecar(string path, bool isXaml, string text, Geometry geometry)
        {
            string svgPath = Path.ChangeExtension(path, ".svg");
            try
            {
                if (isXaml)
                {
                    if (SvgToXamlConverter.LooksLikeSvg(text))
                    {
                        SvgExporter.SaveOriginal(text, svgPath);
                    }
                    else
                    {
                        ShowError("XAML 片段暂不支持导出 SVG，已保存图像文件。");
                    }
                    return;
                }

                SvgExporter.SaveFromPath(geometry, BuildSettings(Colors.Transparent), svgPath);
            }
            catch (Exception ex)
            {
                ShowError("图像已保存，但 SVG 写入失败：" + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, "Path2Ico", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private Geometry BuildGeometry()
        {
            string data = PathTextBox.Text;
            if (string.IsNullOrWhiteSpace(data)) return null;

            try
            {
                return Geometry.Parse(SvgPathNormalizer.Normalize(data));
            }
            catch
            {
                try
                {
                    return Geometry.Parse(data);
                }
                catch
                {
                    return null;
                }
            }
        }

        private RenderSettings BuildSettings(Color background)
        {
            Color fillColor = Colors.Transparent;
            bool fillEnabled = FillEnabledCheckBox.IsChecked == true
                && TryParseColor(FillColorTextBox.Text, out fillColor);

            Color strokeColor = Colors.Black;
            bool strokeEnabled = StrokeEnabledCheckBox.IsChecked == true
                && TryParseColor(StrokeColorTextBox.Text, out strokeColor);

            double thickness;
            if (!double.TryParse(StrokeThicknessTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out thickness)
                || thickness <= 0)
            {
                thickness = 1;
            }

            double padding;
            if (!double.TryParse(PaddingTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out padding))
            {
                padding = 0.1;
            }

            return new RenderSettings
            {
                Background = background,
                Fill = fillEnabled ? new SolidColorBrush(fillColor) : null,
                HasStroke = strokeEnabled,
                StrokeColor = strokeColor,
                StrokeThickness = thickness,
                PaddingRatio = padding
            };
        }

        private int GetOutputSize()
        {
            var item = OutputSizeComboBox.SelectedItem as ComboBoxItem;
            int size;
            if (item != null && int.TryParse(item.Tag as string, out size))
            {
                return size;
            }
            return 256;
        }

        private static IEnumerable<int> GetSizes(CheckBox[] boxes)
        {
            foreach (CheckBox box in boxes)
            {
                if (box.IsChecked != true) continue;
                int size;
                if (int.TryParse(box.Tag as string, out size))
                {
                    yield return size;
                }
            }
        }

        private static bool TryParseColor(string text, out Color color)
        {
            color = Colors.Black;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string value = text.Trim();
            if (value[0] != '#') value = "#" + value;

            try
            {
                object result = ColorConverter.ConvertFromString(value);
                if (result is Color)
                {
                    color = (Color)result;
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
