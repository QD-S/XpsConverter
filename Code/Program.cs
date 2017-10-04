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

        [STAThread]
        static void Main(string[] args)
        {
            // First argument: file path
            // Second argument: image format type
            if (args.Length > 0)
                SaveXpsPagesToBitMap(args[0], args.Length < 2 ? DefaultImageFormat : (ImageFormat)typeof(ImageFormat).GetProperty((char.ToUpper(args[1].First()).ToString() + args[1].Substring(1).ToLower())).GetValue(null));
        }

        private static void SaveXpsPagesToBitMap(string xpsFilePath, ImageFormat imageFormat, string fileNameFormat = null)
        {
            var xpsDocument = new XpsDocument(xpsFilePath, FileAccess.Read);
            var fixedDocumentSequence = xpsDocument.GetFixedDocumentSequence();

            var directoryName = Path.GetFileNameWithoutExtension(xpsFilePath);
            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

            if (fileNameFormat == null) fileNameFormat = new string('0', fixedDocumentSequence.DocumentPaginator.PageCount.ToString().Length);

            // Output image files from each page.
            for (var pageNum = 0; pageNum < fixedDocumentSequence.DocumentPaginator.PageCount; pageNum++)
            {
                var docPage = fixedDocumentSequence.DocumentPaginator.GetPage(pageNum);

                var renderTarget = new RenderTargetBitmap((int)docPage.Size.Width, (int)docPage.Size.Height, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(docPage.Visual);

                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                var filePath = Path.Combine(directoryName, pageNum.ToString(fileNameFormat) + "." + imageFormat.ToString().ToLower());

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
