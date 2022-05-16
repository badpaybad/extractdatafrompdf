using iText.IO.Util;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfExtractor.Domains
{
    public class PdfExtractor
    {
        public class PdfPage
        {
            public int PageIndex { get; set; }
            public string RawText { get; set; }
            public List<MemoryStream> Images { get; set; }
        }

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
        public event Action<PdfPage>? OnPageExtract;

        ///https://blog.aspose.com/2021/01/25/work-with-images-in-pdf-in-csharp/
        ////http://www.nullskull.com/q/10465415/read-image-text-from-pdf-file-to-itextsharp-in-aspnet-c.aspx
        ///
        public async Task<List<PdfPage>> GetPageInfos(string pdfFullPath)
        {
            try
            {
                ////https://livebook.manning.com/book/itext-in-action-second-edition/chapter-11/182
                PdfDocument doc = new PdfDocument(new PdfReader(pdfFullPath));

                //PdfFont font = PdfFontFactory.CreateFont("Arial");

                ////https://riptutorial.com/itext/topic/5780/fonts--itext-5-versus-itext-7

                List<PdfPage> pages = new List<PdfPage>();

                int numOfPages = doc.GetNumberOfPages();

                var info = new PdfInfo() { TotalPage = numOfPages };

                if (OnState != null) OnState(info);

                for (var i = 1; i < numOfPages; i++)
                {
                    try
                    {
                        var page = doc.GetPage(i);


                        //PdfDictionary fontResources = page.GetResources().GetResource(PdfName.Font);
                        //try
                        //{

                        //    foreach (PdfObject font in fontResources.Values(true))
                        //    {

                        //        if (font is PdfDictionary)
                        //            fontResources.Put(PdfName.Encoding, PdfName.IdentityH);
                        //    }

                        //}
                        //catch { }



                        ITextExtractionStrategy strategy
                            = new LocationTextExtractionStrategy();
                        //= new SimpleTextExtractionStrategy();
                        string pageContent = PdfTextExtractor.GetTextFromPage(page, strategy);

                        pageContent = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(pageContent)));


                        TextAndImageExtractor eventListener = new TextAndImageExtractor();
                        PdfCanvasProcessor pdfCanvasProcessor = new PdfCanvasProcessor(eventListener);

                        pdfCanvasProcessor.ProcessPageContent(page);

                        PdfPage item = new PdfPage
                        {
                            Images = eventListener.GetImages(),
                            PageIndex = i - 1,
                            RawText = eventListener.GetText(),
                        };
                        pages.Add(item);

                        if (OnPageExtract != null) OnPageExtract(item);
                    }
                    catch (Exception exp)
                    {

                    }
                }

                info.Step = 1;

                if (OnState != null) OnState(info);

                return pages;
            }
            catch (Exception ex)
            {
                return new List<PdfPage>();
            }
        }

        public class TextAndImageExtractor : IEventListener
        {
            List<MemoryStream> _listImage { get; set; } = new List<MemoryStream>();

            StringBuilder _text = new StringBuilder();

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
                    _text.Append(txt.GetText());
                }
            }

            public ICollection<EventType> GetSupportedEvents()
            {
                return null;
            }
        }

    }
}
