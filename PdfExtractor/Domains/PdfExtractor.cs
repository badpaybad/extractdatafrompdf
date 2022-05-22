using iText.IO.Font;
using iText.IO.Util;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PdfExtractor.Domains
{
    public class MyPdfPage
    {
        public int PageIndex { get; set; }
        public string ContentText { get; set; } = String.Empty;
        public List<MemoryStream> ContentImages { get; set; } = new List<MemoryStream>();

        public byte[]? PageBytes { get; set; }
        public MemoryStream? PageStream { get; set; }

        public Bitmap? PageImage { get; set; }

        public BitmapImage? ResizeTo(int percentage)
        {
            var img = PageImage;

            if (img == null) return null;

            int neww = (img.Width * percentage) / 100;
            var newh = (img.Height * percentage) / 100;

            using (MemoryStream memory = new MemoryStream())
            {
                Bitmap thumb = new Bitmap(img, neww, newh);

                thumb.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public System.Windows.Media.Imaging.BitmapImage? PageBitmapData
        {
            get
            {
                try
                {

                    if (PageImage == null) return null;

                    using (MemoryStream memory = new MemoryStream())
                    {
                        Bitmap thumb = new Bitmap(PageImage, 120, 120 * PageImage.Height / PageImage.Width);

                        thumb.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                        memory.Position = 0;
                        BitmapImage bitmapimage = new BitmapImage();
                        bitmapimage.BeginInit();
                        bitmapimage.StreamSource = memory;
                        bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapimage.EndInit();

                        return bitmapimage;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

    }

    public class PdfExtractor
    {

        public class PdfInfo
        {
            public int TotalPage { get; set; }

            public int Step { get; set; }
        }

        static PdfExtractor()
        {
            iText.IO.Util.ResourceUtil.AddToResourceSearch("itext.font_asian.dll");
            iText.IO.Util.ResourceUtil.AddToResourceSearch("iTextAsian.dll");
            iText.IO.Util.ResourceUtil.AddToResourceSearch("iTextAsianCmaps.dll");
        }

        public event Action<PdfInfo>? OnState;
        public event Action<MyPdfPage>? OnPageExtract;

        ///https://blog.aspose.com/2021/01/25/work-with-images-in-pdf-in-csharp/
        ////http://www.nullskull.com/q/10465415/read-image-text-from-pdf-file-to-itextsharp-in-aspnet-c.aspx
        ///
        public async Task<List<MyPdfPage>> GetPageInfos(string pdfFullPath)
        {
            await Task.Yield();
            try
            {
                ////https://livebook.manning.com/book/itext-in-action-second-edition/chapter-11/182
                PdfDocument doc = new PdfDocument(new PdfReader(pdfFullPath));

                ////PdfFont font = PdfFontFactory.CreateFont("Arial");

                ////https://riptutorial.com/itext/topic/5780/fonts--itext-5-versus-itext-7

                List<MyPdfPage> pages = new List<MyPdfPage>();

                int numOfPages = doc.GetNumberOfPages();

                var info = new PdfInfo() { TotalPage = numOfPages };

                if (OnState != null) OnState(info);

                for (var i = 1; i <= numOfPages; i++)
                {
                    try
                    {
                        var page = doc.GetPage(i);

                        ////PdfDictionary fontResources = page.GetResources().GetResource(PdfName.Font);
                        ////try
                        ////{
                        ////    foreach (PdfObject font in fontResources.Values(true))
                        ////    {
                        ////        if (font is PdfDictionary)
                        ////            fontResources.Put(PdfName.Encoding, PdfName.IdentityH);
                        ////    }
                        ////}
                        ////catch { }

                        ////ITextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                        ////= new SimpleTextExtractionStrategy();
                        ////string pageContent =  Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(PdfTextExtractor.GetTextFromPage(page, strategy))));


                        TextAndImageExtractor eventListener = new TextAndImageExtractor();
                        PdfCanvasProcessor pdfCanvasProcessor = new PdfCanvasProcessor(eventListener);

                        pdfCanvasProcessor.ProcessPageContent(page);

                        ////using (var msw = new MemoryStream())
                        ////{
                        ////    PdfDocument pdf = new PdfDocument(new PdfWriter(new MemoryStream()));
                        ////    PdfFormXObject xobj = page.CopyAsFormXObject(pdf);
                        ////    ////iText.Layout.Element.Image image = new iText.Layout.Element.Image(xobj);
                        ////    ////iText.Layout.Document docx = new iText.Layout.Document(pdf);
                        ////}

                        MyPdfPage item = new MyPdfPage
                        {
                            ContentImages = eventListener.GetImages(),
                            PageIndex = i,
                            ContentText = TextAndImageExtractor.NormalizeText(eventListener.GetText())
                        };

                        pages.Add(item);

                        if (OnPageExtract != null) OnPageExtract(item);
                    }
                    catch
                    {
                        //
                    }
                }

                info.Step = 1;

                if (OnState != null) OnState(info);

                return pages;
            }
            catch
            {
                return new List<MyPdfPage>();
            }
        }
        public interface IChunk
        {
        }
        public class ImageChunk : IChunk
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float W { get; set; }
            public float H { get; set; }
            public System.Drawing.Image? Image { get; set; }
        }
        public class TextChunk : IChunk
        {
            public string Text { get; set; } = String.Empty;
            public iText.Kernel.Geom.Rectangle? Rect { get; set; }
            public string FontFamily { get; set; } = String.Empty;
            public int FontSize { get; set; }
            public FontStyle FontStyle { get; set; }
            public float SpaceWidth { get; set; }
            public Color Color { get; internal set; }
        }
        public class ImageListener : FilteredEventListener
        {
            private readonly SortedDictionary<float, IChunk> chunkDictionairy;
            private readonly Func<float> increaseCounter;

            public ImageListener(SortedDictionary<float, IChunk> chunkDictionairy, Func<float> increaseCounter)
            {
                this.chunkDictionairy = chunkDictionairy;
                this.increaseCounter = increaseCounter;
            }

            public override void EventOccurred(IEventData data, EventType type)
            {
                if (type != EventType.RENDER_IMAGE) return;

                float counter = increaseCounter();

                var renderInfo = (ImageRenderInfo)data;

                var imageObject = renderInfo.GetImage();
                Bitmap image;

                try
                {
                    var imageBytes = imageObject.GetImageBytes();
                    image = new Bitmap(new MemoryStream(imageBytes));
                }
                catch
                {
                    return;
                }

                var smask = imageObject.GetPdfObject().GetAsStream(PdfName.SMask);

                if (smask != null)
                {
                    try
                    {
                        var maskImageObject = new PdfImageXObject(smask);
                        var maskBytes = maskImageObject.GetImageBytes();
                        using (var maskImage = new Bitmap(new MemoryStream(maskBytes)))
                        {
                            image = GenerateMaskedImage(image, maskImage);
                        }
                    }
                    catch
                    {
                        //
                    }
                }

                var matix = renderInfo.GetImageCtm();

                var imageChunk = new ImageChunk
                {
                    X = matix.Get(iText.Kernel.Geom.Matrix.I31),
                    Y = matix.Get(iText.Kernel.Geom.Matrix.I32),
                    W = matix.Get(iText.Kernel.Geom.Matrix.I11),
                    H = matix.Get(iText.Kernel.Geom.Matrix.I22),
                    Image = image
                };

                chunkDictionairy.Add(counter, imageChunk);

                base.EventOccurred(data, type);
            }

            private Bitmap GenerateMaskedImage(Bitmap image, Bitmap mask)
            {
                var output = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
                var rect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
                var bitsMask = mask.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var bitsInput = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var bitsOutput = output.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                unsafe
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        byte* ptrMask = (byte*)bitsMask.Scan0 + y * bitsMask.Stride;
                        byte* ptrInput = (byte*)bitsInput.Scan0 + y * bitsInput.Stride;
                        byte* ptrOutput = (byte*)bitsOutput.Scan0 + y * bitsOutput.Stride;
                        for (int x = 0; x < image.Width; x++)
                        {
                            ptrOutput[4 * x] = ptrInput[4 * x];           // blue
                            ptrOutput[4 * x + 1] = ptrInput[4 * x + 1];   // green
                            ptrOutput[4 * x + 2] = ptrInput[4 * x + 2];   // red
                            ptrOutput[4 * x + 3] = ptrMask[4 * x];        // alpha
                        }
                    }
                }

                mask.UnlockBits(bitsMask);
                image.UnlockBits(bitsInput);
                output.UnlockBits(bitsOutput);

                return output;
            }
        }
        public class TextListener : LocationTextExtractionStrategy
        {
            private readonly SortedDictionary<float, IChunk> chunkDictionairy;
            private readonly Func<float> increaseCounter;

            public TextListener(SortedDictionary<float, IChunk> chunkDictionairy, Func<float> increaseCounter)
            {
                this.chunkDictionairy = chunkDictionairy;
                this.increaseCounter = increaseCounter;
            }

            public override void EventOccurred(IEventData data, EventType type)
            {
                if (!type.Equals(EventType.RENDER_TEXT)) return;

                TextRenderInfo renderInfo = (TextRenderInfo)data;

                float counter = increaseCounter();

                var font = renderInfo.GetFont().GetFontProgram();
                var originalFontName = font.ToString();
                var fontRegex = Regex.Match(originalFontName, @"(?<=\+)[a-zA-Z\s]+");

                string fontName = fontRegex.Success ? fontRegex.Value : originalFontName;

                var fontStyle = font.GetFontNames().GetFontStyle();

                float curFontSize = renderInfo.GetFontSize();

                float key = counter;

                IList<TextRenderInfo> text = renderInfo.GetCharacterRenderInfos();
                foreach (TextRenderInfo character in text)
                {
                    key += 0.001f;

                    ///var textRenderMode = character.GetTextRenderMode();
                    ///var opacity = character.GetGraphicsState().GetFillOpacity();

                    string letter = character.GetText();

                    Color color;

                    var fillColor = character.GetFillColor();
                    var colors = fillColor.GetColorValue();
                    if (colors.Length == 1)
                    {
                        color = Color.FromArgb((int)(255 * (1 - colors[0])), Color.Black);
                    }
                    else if (colors.Length == 3)
                    {
                        color = Color.FromArgb((int)(255 * colors[0]), (int)(255 * colors[1]), (int)(255 * colors[2]));
                    }
                    else if (colors.Length == 4)
                    {
                        color = Color.FromArgb((int)(255 * colors[0]), (int)(255 * colors[1]), (int)(255 * colors[2]), (int)(255 * colors[3]));
                    }
                    else
                    {
                        color = Color.Black;
                    }

                    if (string.IsNullOrWhiteSpace(letter)) continue;

                    //Get the bounding box for the chunk of text
                    var bottomLeft = character.GetDescentLine().GetStartPoint();
                    var topRight = character.GetAscentLine().GetEndPoint();

                    //Create a rectangle from it
                    var rect = new iText.Kernel.Geom.Rectangle(
                                                            bottomLeft.Get(Vector.I1),
                                                            topRight.Get(Vector.I2),
                                                            topRight.Get(Vector.I1),
                                                            topRight.Get(Vector.I2)
                                                            );

                    var currentChunk = new TextChunk()
                    {
                        Text = letter,
                        Rect = rect,
                        FontFamily = fontName,
                        FontSize = (int)curFontSize,
                        FontStyle = fontStyle,
                        Color = color,
                        SpaceWidth = character.GetSingleSpaceWidth() / 2f
                    };

                    chunkDictionairy.Add(key, currentChunk);
                }

                base.EventOccurred(data, type);
            }
        }

        private static int counter;

        private readonly Func<float> IncreaseCounter = () =>
        {
            counter = Interlocked.Increment(ref counter);
            return counter;
        };

        public Bitmap ConvertToBitmap(PdfPage pdfPage)
        {
            var rotation = pdfPage.GetRotation();

            var chunkDictionairy = new SortedDictionary<float, IChunk>();

            FilteredEventListener listener = new FilteredEventListener();
            listener.AttachEventListener(new TextListener(chunkDictionairy, IncreaseCounter));
            listener.AttachEventListener(new ImageListener(chunkDictionairy, IncreaseCounter));
            PdfCanvasProcessor processor = new PdfCanvasProcessor(listener);
            processor.ProcessPageContent(pdfPage);

            ////var size = currentPage.GetPageSizeWithRotation();
            var size = pdfPage.GetPageSize();

            var width = size.GetWidth().PointsToPixels();
            var height = size.GetHeight().PointsToPixels();

            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

                var _imgs = chunkDictionairy.Where(c => c.Value is ImageChunk)
                    .Select(chunk =>
                    {

                        g.ResetTransform();

                        g.RotateTransform(-rotation);

                        var imageChunk = (ImageChunk)chunk.Value;

                        var imgW = imageChunk.W.PointsToPixels();
                        var imgH = imageChunk.H.PointsToPixels();
                        var imgX = imageChunk.X.PointsToPixels();
                        var imgY = (size.GetHeight() - imageChunk.Y - imageChunk.H).PointsToPixels();

                        g.TranslateTransform(imgX, imgY, MatrixOrder.Append);
                        if (imageChunk.Image != null)
                        {
                            g.DrawImage(imageChunk.Image, 0, 0, imgW, imgH);
                            imageChunk.Image.Dispose();
                        }
                        return chunk;
                    })
                    .ToList();
                _imgs.Clear();

                var _txts = chunkDictionairy.Where(c => c.Value is TextChunk)
                    .Select(chunk =>
                    {
                        g.ResetTransform();

                        g.RotateTransform(-rotation);

                        var textChunk = (TextChunk)chunk.Value;

                        if (textChunk.Rect == null)
                        {
                            return chunk;
                        }

                        var chunkX = textChunk.Rect.GetX().PointsToPixels();
                        var chunkY = bmp.Height - textChunk.Rect.GetY().PointsToPixels();

                        var fontSize = textChunk.FontSize.PointsToPixels();

                        Font font;
                        try
                        {
                            font = new Font(textChunk.FontFamily, fontSize, textChunk.FontStyle, GraphicsUnit.Pixel);
                        }
                        catch
                        {
                            font = new Font("Arial", 11, textChunk.FontStyle, GraphicsUnit.Pixel);
                        }

                        g.TranslateTransform(chunkX, chunkY, MatrixOrder.Append);

                        ////g.DrawString(textChunk.Text, font, new SolidBrush(textChunk.Color), chunkX, chunkY);

                        g.DrawString(textChunk.Text, font, new SolidBrush(textChunk.Color), 0, 0);
                        return chunk;
                    }).ToList();
                _txts.Clear();

                g.Flush();
            }

            return bmp;
        }


        public class TextAndImageExtractor : IEventListener
        {
            List<MemoryStream> _listImage { get; set; } = new List<MemoryStream>();

            readonly StringBuilder _text = new StringBuilder();

            public string GetText()
            {
                return _text.ToString();
            }

            public List<MemoryStream> GetImages()
            {
                return _listImage.ToList();
            }

            public void EventOccurred(IEventData data, EventType type)
            {
                if (type == EventType.RENDER_IMAGE)
                {
                    ImageRenderInfo img = (ImageRenderInfo)data;
                    try
                    {
                        _listImage.Add(new MemoryStream(img.GetImage().GetImageBytes()));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                if (type == EventType.RENDER_TEXT
                    //|| type ==EventType.BEGIN_TEXT || type== EventType.END_TEXT
                    )
                {
                    TextRenderInfo txt = (TextRenderInfo)data;
                    _text.Append(NormalizeText(txt.GetText()));
                }
            }

            static char[] _split = { ' ', '\r', '\n' };

            public static string NormalizeText(string text)
            {
                text = RemDuplicate(text, "  ", " ");
                text = RemDuplicate(text, "\r \r", "\r");
                text = RemDuplicate(text, "\r\r", "\r");
                text = RemDuplicate(text, "\n \n", "\n");
                text = RemDuplicate(text, "\n\n", "\n");
                text = RemDuplicate(text, "\r\n \r\n", "\r\n");
                text = RemDuplicate(text, "\r\n\r\n", "\r\n");

                text = text.Trim(_split);
                return text;

                string RemDuplicate(string intxt, string toRem, string replaceBy)
                {
                    while (intxt.IndexOf(toRem) >= 0)
                    {
                        intxt = intxt.Replace(toRem, replaceBy);
                    }

                    return intxt;
                }
            }
            public ICollection<EventType> GetSupportedEvents()
            {
                return new List<EventType>();
            }
        }

    }

    public static class MeasuringExtensions
    {
        //72 points == 1
        //72px x 72px is 1inch x 1inch at a 72dpi resolution

        internal static FontStyle GetFontStyle(this FontNames fontNames)
        {
            var fontname = fontNames.GetFontName();
            var fontStyleRegex = Regex.Match(fontname, @"[-,][\w\s]+$");

            if (fontStyleRegex.Success)
            {
                var result = fontStyleRegex.Value.ToLower();
                if (result.Contains("bold"))
                {
                    return FontStyle.Bold;
                }
            }

            return FontStyle.Regular;
        }
        public static int Dpi { get; set; } = 300;

        public static float PixelsToPoints(this float value, int? dpi = null)
        {
            return value / (dpi ?? Dpi) * 72;
        }

        public static int PointsToPixels(this int value, int? dpi = null)
        {
            return PointsToPixels((float)value);
        }

        public static int PointsToPixels(this float value, int? dpi = null)
        {
            return (int)(value * (dpi ?? Dpi) / 72);
        }
    }
}
