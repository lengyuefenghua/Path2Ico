using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Path2Ico.Services
{
    public static class IcoWriter
    {
        private const int DirectoryHeaderSize = 6;
        private const int DirectoryEntrySize = 16;

        public static void Save(IList<BitmapSource> images, string path)
        {
            if (images == null) throw new ArgumentNullException("images");
            if (images.Count == 0) throw new ArgumentException("At least one image is required.", "images");

            var ordered = images.OrderBy(i => i.PixelWidth).ToList();
            var payloads = ordered.Select(ImageExporter.EncodePng).ToList();

            using (var stream = File.Create(path))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)ordered.Count);

                int offset = DirectoryHeaderSize + DirectoryEntrySize * ordered.Count;
                for (int i = 0; i < ordered.Count; i++)
                {
                    BitmapSource image = ordered[i];
                    writer.Write((byte)(image.PixelWidth >= 256 ? 0 : image.PixelWidth));
                    writer.Write((byte)(image.PixelHeight >= 256 ? 0 : image.PixelHeight));
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write(payloads[i].Length);
                    writer.Write(offset);
                    offset += payloads[i].Length;
                }

                foreach (byte[] payload in payloads)
                {
                    writer.Write(payload);
                }
            }
        }
    }
}
