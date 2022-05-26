using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfExtractor.Domains
{
    public class PdfToImageProcessing
    {
        public string FilePdf { get { return _filepdf; } }

        public string FileName { get; set; }

        public int ParseStep { get; set; } = 0;

        public string UploadStateText { get; set; } = string.Empty;

        public string ContextText { get; set; }

        public string ParseStepText
        {
            get
            {
                if (ParseStep == 0) return "Waiting";
                if (ParseStep == 1) return "Prepared";

                if (ParseStep == 2) return "Got Content";

                if (ParseStep == 3) return "Got Code";
                if (ParseStep == 4) return "Got Title";

                if (ParseStep == 5) return "Got Signed by";

                if (ParseStep == 6) return "Done";

                if (ParseStep == -1) return "Failed";

                return string.Empty;
            }
        }

        public List<MyPdfPage> Pages { get; set; } = new List<MyPdfPage>();

        readonly string _filepdf = string.Empty;

        TesseractEngineWrapper _ocr;

        private readonly int _threadConsume = 1;

        public Dictionary<string, string> PdfProperties { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<int, System.Drawing.Rectangle?>> PdfPropertiesRegion { get; set; } = new Dictionary<string, Dictionary<int, System.Drawing.Rectangle?>>();

        public PdfToImageProcessing(string filePdf)
        {
            _threadConsume = (Environment.ProcessorCount * 1) / 5 + 1;

            _filepdf = filePdf.Replace("\\", "/");

            var idx = _filepdf.LastIndexOf('/');
            if (idx > 0) FileName = _filepdf.Substring(idx + 1);
            else FileName = _filepdf;

            _ocr = new TesseractEngineWrapper();
            ParseStep = 0;
        }

        public event Action<string, string, System.Drawing.Rectangle?>? OnSetProperty;

        public int RatioResize { get; set; }

        public void SetRatioResize(int ratioResize)
        {
            RatioResize = ratioResize;
        }
        public void SetProperty(string propertyName, string value, System.Drawing.Rectangle? box, int boxInPageIdx)
        {

            PdfProperties[propertyName] = value;

            if (box != null)
                PdfPropertiesRegion[propertyName] = new Dictionary<int, System.Drawing.Rectangle?>() { { boxInPageIdx, box.Value } };

            OnSetProperty?.Invoke(propertyName, value, box);
        }

        public void ConvertToPagesImages()
        {
            var listFileInByte = Freeware.Pdf2Png.ConvertAllPages(File.OpenRead(_filepdf));

            Pages = new List<MyPdfPage>();

            for (int idx = 0; idx < listFileInByte.Count; idx++)
            {
                byte[]? i = listFileInByte[idx];
                var ps = new MemoryStream(i);
                var p = new MyPdfPage
                {
                    PageStream = ps,
                    PageBytes = i,
                    PageIndex = idx + 1,
                    PageImage = new System.Drawing.Bitmap(ps),
                    ContentImages = new List<MemoryStream>(),
                };
                Pages.Add(p);
            }

            Pages = Pages.OrderBy(i => i.PageIndex).ToList();
        }

        public List<MyPdfPage> Prepare()
        {
            if (ParseStep > 0)
            {
                if (Pages == null || Pages.Count == 0)
                {
                    ConvertToPagesImages();
                }

                return Pages ?? new List<MyPdfPage>();
            }

            ParseStep = 0;

            ConvertToPagesImages();

            ParseStep = 1;
            return Pages;
        }

        public event Action<int>? OnDoneParsePageIndex;

        public void Parse()
        {
            if (ParseStep > 1) return;

            List<int> pres = new List<int> { };

            Parallel.ForEach(Pages, new ParallelOptions { MaxDegreeOfParallelism = _threadConsume },
                p =>
            {
                if (p.PageBytes == null || p.PageImage == null)
                {
                    pres.Add(-1);
                    return;
                }

                try
                {
                    p.ContentText = _ocr.TryFindText(p.PageImage, "vie");
                }
                catch { pres.Add(-1); }

                OnDoneParsePageIndex?.Invoke(p.PageIndex);
            });

            if (pres.Any(i => i == -1))
            {
                Pages = null;
                ParseStep = -1;
                return;
            }

            ParseStep = 2;

            ContextText = string.Join("\r\n", Pages.Select(i => i.ContentText));

            ParseCode();

            ParseTitle();

            ParseSignedBy();

            ParseDate();

            ParseSignedAt();

            ParseStep = ParseStep + 1;
        }

        System.Drawing.Bitmap CropImage(System.Drawing.Bitmap src, System.Drawing.Rectangle cropArea)
        {
            if (RatioResize <= 0) return new System.Drawing.Bitmap(1, 1);

            var bmpImage = src;

            return bmpImage.Clone(new System.Drawing.Rectangle((int)cropArea.X * RatioResize, (int)cropArea.Y * RatioResize, (int)cropArea.Width * RatioResize, (int)cropArea.Height * RatioResize), bmpImage.PixelFormat);
        }

        string GetTextByTemplate(string propName, out int pageIdx, out System.Drawing.Rectangle? box)
        {
            pageIdx = -1;
            box = null;

            if (RatioResize <= 0) return string.Empty;

            var template = MyAppContext.GetTemplate();

            var minPage = template.CropArea.SelectMany(i => i.Value).Min(i => i.Key);
            var maxPage = template.CropArea.SelectMany(i => i.Value).Max(i => i.Key);

            var text = string.Empty;

            if (template.CropArea.TryGetValue(propName, out var value))
            {
                var boxing = value.FirstOrDefault();

                box = boxing.Value;


                if (boxing.Key == minPage)
                {
                    MyPdfPage? myPdfPage = Pages.FirstOrDefault();

                    if (myPdfPage != null && myPdfPage.PageImage != null && box != null)
                        text = new TesseractEngineWrapper().TryFindText(CropImage(myPdfPage.PageImage, box.Value));

                    //first page
                    pageIdx = 1;
                }
                else if (boxing.Key == maxPage)
                {
                    MyPdfPage? myPdfPage = Pages.LastOrDefault();

                    if (myPdfPage != null && myPdfPage.PageImage != null && box != null)
                        text = new TesseractEngineWrapper().TryFindText(CropImage(myPdfPage.PageImage, box.Value));

                    pageIdx = Pages.Count;
                    //last page
                }
                else
                {
                    //page index
                    pageIdx = boxing.Key;

                    if (pageIdx > 0 && pageIdx < Pages.Count)
                    {
                        MyPdfPage? myPdfPage = Pages[pageIdx - 1];

                        if (myPdfPage != null && myPdfPage.PageImage != null && box != null)
                            text = new TesseractEngineWrapper().TryFindText(CropImage(myPdfPage.PageImage, box.Value));

                    }

                }
            }

            return text;
        }


        public void ParseCode()
        {
            //SetProperty("Code")

            var text = GetTextByTemplate("Code", out int pageIdx, out System.Drawing.Rectangle? box);

            SetProperty("Code", text, box, pageIdx);

            ParseStep = 3;
        }

        public void ParseTitle()
        {
            var text = GetTextByTemplate("Title", out int pageIdx, out System.Drawing.Rectangle? box);

            SetProperty("Title", text, box, pageIdx);
            //SetProperty("Title")
            ParseStep = 4;
        }

        public void ParseSignedBy()
        {
            var text = GetTextByTemplate("SignedBy", out int pageIdx, out System.Drawing.Rectangle? box);

            SetProperty("SignedBy", text, box, pageIdx);
            //SetProperty("SignedBy")
            ParseStep = 5;
        }


        public void ParseDate()
        {
            //SetProperty("Code")

            var text = GetTextByTemplate("Date", out int pageIdx, out System.Drawing.Rectangle? box);

            SetProperty("Date", text, box, pageIdx);

            ParseStep = 6;
        }

        public void ParseSignedAt()
        {
            var text = GetTextByTemplate("SignedAt", out int pageIdx, out System.Drawing.Rectangle? box);

            SetProperty("SignedAt", text, box, pageIdx);
            //SetProperty("SignedBy")
            ParseStep = 7;
        }
        public void Reset()
        {
            Pages = null;
            Pages = new List<MyPdfPage>();
        }
    }
}
