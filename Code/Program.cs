namespace XpsConverter
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Xps.Packaging;

    class Program
    {
        public static readonly ImageFormat DefaultImageFormat = ImageFormat.Png;
        private static readonly string[] helpOptions = new[] { "?", "-h", "-help" };

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
                if (helpOptions.Any(x => string.Equals(x, args[0], StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("First argument: file path");
                    Console.WriteLine("Second argument: image format type (bmp, png, jpeg, etc.)");
                    Console.WriteLine("Third argument: file name format");
                }
                else
                    SaveXpsPagesToBitMap(
                        args[0],
                        args.Length < 2 ? DefaultImageFormat : (ImageFormat)typeof(ImageFormat).GetProperty((char.ToUpper(args[1].First()).ToString() + args[1].Substring(1).ToLower())).GetValue(null),
                        args.Length < 3 || args[2] == string.Empty ? null : args[2]);
        }

        private static void SaveXpsPagesToBitMap(string xpsFilePath, ImageFormat imageFormat, string fileNameFormat = null)
        {
            var xpsDocument = new XpsDocument(xpsFilePath, FileAccess.Read);
            var fixedDocumentSequence = xpsDocument.GetFixedDocumentSequence();

            var directoryName = Path.GetFileNameWithoutExtension(xpsFilePath);
            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

            var pageCount = fixedDocumentSequence.DocumentPaginator.PageCount;
            var formatPrefix = "{0:";
            if (fileNameFormat == null) fileNameFormat = formatPrefix + new string('0', pageCount.ToString().Length) + "}";

            // Output image files from each page.
            for (var pageNum = 0; pageNum < pageCount; pageNum++)
            {
                var docPage = fixedDocumentSequence.DocumentPaginator.GetPage(pageNum);

                var renderTarget = new RenderTargetBitmap((int)docPage.Size.Width, (int)docPage.Size.Height, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(docPage.Visual);

                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                var fileName = fileNameFormat.IndexOf(formatPrefix) != -1 || pageCount == 1 ? string.Format(fileNameFormat, pageNum) : pageNum.ToString(fileNameFormat);
                var filePath = Path.Combine(directoryName, fileName + "." + imageFormat.ToString().ToLower());

                if (imageFormat == ImageFormat.Bmp)
                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        encoder.Save(stream);
                else
                    using (var stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        using (var bmp = new Bitmap(stream))
                            bmp.Save(filePath, imageFormat);
                    }
            }
        }
    }
}
