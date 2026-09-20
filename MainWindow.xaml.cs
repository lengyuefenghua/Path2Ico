using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Path2Ico.Services;

namespace Path2Ico
{
    public partial class MainWindow : Window
    {
        private const int PreviewSize = 256;
        private const int JpegQuality = 90;

        private const string SamplePath =
            "M 50,5 L 61,38 L 95,38 L 67,59 L 78,92 L 50,71 L 22,92 L 33,59 L 5,38 L 39,38 Z";

        private bool _ready;
        private CheckBox[] _outputSizeBoxes;
        private CheckBox[] _icoSizeBoxes;

        public MainWindow()
        {
            InitializeComponent();

            _outputSizeBoxes = new[] { Size16, Size20, Size24, Size32, Size40, Size48, Size64, Size96, Size128, Size256 };
            _icoSizeBoxes = new[] { Ico16, Ico20, Ico24, Ico32, Ico40, Ico48, Ico64, Ico96, Ico128, Ico256 };

            OutputDirTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            PathTextBox.Text = SamplePath;

            _ready = true;
            RefreshPreview();
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

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择输出目录";
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrWhiteSpace(OutputDirTextBox.Text) && Directory.Exists(OutputDirTextBox.Text))
                {
                    dialog.SelectedPath = OutputDirTextBox.Text;
                }
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputDirTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void RefreshPreview()
        {
            if (!_ready) return;

            Geometry geometry = BuildGeometry();
            if (geometry == null)
            {
                PreviewImage.Source = null;
                SetLog(string.IsNullOrWhiteSpace(PathTextBox.Text)
                    ? "请输入路径数据。"
                    : "路径解析失败，请检查语法。");
                return;
            }

            try
            {
                PreviewImage.Source = PathRenderer.Render(geometry, PreviewSize, BuildSettings(Colors.Transparent));
                SetLog("预览已更新。");
            }
            catch (Exception ex)
            {
                PreviewImage.Source = null;
                SetLog("预览失败：" + ex.Message);
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;

            Geometry geometry = BuildGeometry();
            if (geometry == null)
            {
                SetLog("路径解析失败，无法生成。");
                return;
            }

            string directory = OutputDirTextBox.Text.Trim();
            if (directory.Length == 0)
            {
                SetLog("请先选择输出目录。");
                return;
            }
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                SetLog("无法创建输出目录：" + ex.Message);
                return;
            }

            string baseName = BaseNameTextBox.Text.Trim();
            if (baseName.Length == 0)
            {
                SetLog("请输入文件名。");
                return;
            }
            if (baseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                SetLog("文件名包含非法字符。");
                return;
            }

            List<int> outputSizes = GetSizes(_outputSizeBoxes).ToList();
            bool exportPng = FormatPng.IsChecked == true;
            bool exportJpg = FormatJpg.IsChecked == true;
            bool exportIco = FormatIco.IsChecked == true;

            if (!exportPng && !exportJpg && !exportIco)
            {
                SetLog("请至少选择一种输出格式。");
                return;
            }
            if (outputSizes.Count == 0)
            {
                SetLog("请至少选择一个输出尺寸。");
                return;
            }

            RenderSettings transparent = BuildSettings(Colors.Transparent);
            RenderSettings white = BuildSettings(Colors.White);
            var lines = new List<string>();
            int generated = 0;

            try
            {
                foreach (int size in outputSizes)
                {
                    if (exportPng)
                    {
                        string path = Path.Combine(directory, baseName + "_" + size + ".png");
                        ImageExporter.SavePng(PathRenderer.Render(geometry, size, transparent), path);
                        lines.Add(Path.GetFileName(path));
                        generated++;
                    }
                    if (exportJpg)
                    {
                        string path = Path.Combine(directory, baseName + "_" + size + ".jpg");
                        ImageExporter.SaveJpeg(PathRenderer.Render(geometry, size, white), path, JpegQuality);
                        lines.Add(Path.GetFileName(path));
                        generated++;
                    }
                }

                if (exportIco)
                {
                    List<int> icoSizes = GetSizes(_icoSizeBoxes).ToList();
                    if (icoSizes.Count == 0)
                    {
                        lines.Add("ICO 已跳过：未选择内嵌尺寸。");
                    }
                    else
                    {
                        var images = icoSizes.Select(size => PathRenderer.Render(geometry, size, transparent)).ToList();
                        string path = Path.Combine(directory, baseName + ".ico");
                        IcoWriter.Save(images, path);
                        lines.Add(Path.GetFileName(path) + "（" + string.Join(",", icoSizes) + "）");
                        generated++;
                    }
                }
            }
            catch (Exception ex)
            {
                SetLog("生成失败：" + ex.Message);
                return;
            }

            SetLog("完成，共生成 " + generated + " 个文件：\r\n"
                + string.Join("\r\n", lines) + "\r\n目录：" + directory);
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

        private void SetLog(string message)
        {
            LogTextBox.Text = message;
        }
    }
}
