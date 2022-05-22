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

        public List<MyPdfPage> Pages
        {
            get
            {

                if (_pages == null) Prepare();

                return _pages;
            }
        }

        readonly string _filepdf = string.Empty;

        TesseractEngineWrapper _ocr;

        List<MyPdfPage> _pages = new List<MyPdfPage>();

        private readonly int _threadConsume = 1;

        public Dictionary<string, string> PdfProperties = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<int, System.Drawing.Rectangle>> PdfPropertiesRegion = new Dictionary<string, Dictionary<int, System.Drawing.Rectangle>>();

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

        public void SetProperty(string propertyName, string value, System.Drawing.Rectangle? box,int boxInPageIdx)
        {
            PdfProperties[propertyName] = value;

            if (box != null)
                PdfPropertiesRegion[propertyName] = new Dictionary<int, System.Drawing.Rectangle>() { { boxInPageIdx, box.Value } };

            OnSetProperty?.Invoke(propertyName, value, box);
        }

        public List<MyPdfPage> Prepare()
        {
            if (ParseStep > 0) return _pages;

            ParseStep = 0;
            _pages = Freeware.Pdf2Png.ConvertAllPages(File.OpenRead(_filepdf))
                .Select((i, idx) =>
                {
                    var ps = new MemoryStream(i);
                    var p = new MyPdfPage
                    {
                        PageStream = ps,
                        PageBytes = i,
                        PageIndex = idx+1,
                        PageImage = new System.Drawing.Bitmap(ps),
                        ContentImages = new List<MemoryStream>(),
                    };
                    return p;
                }).OrderBy(i => i.PageIndex).ToList();

            ParseStep = 1;
            return _pages;
        }

        public event Action<int>? OnDoneParsePageIndex;

        public void Parse()
        {
            if (ParseStep > 1) return;

            List<int> pres = new List<int> { };

            Parallel.ForEach(_pages, new ParallelOptions { MaxDegreeOfParallelism = _threadConsume },
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
                _pages = null;
                ParseStep = -1;
                return;
            }

            ParseStep = 2;

            ContextText= string.Join("\r\n", _pages.Select(i => i.ContentText));

            ParseCode();

            ParseTitle();

            ParseSignedBy();

            ParseStep = 6;
        }


        public void ParseCode()
        {

            ParseStep = 3;

        }

        public void ParseTitle()
        {

            ParseStep = 4;
        }

        public void ParseSignedBy()
        {

            ParseStep = 5;
        }

        public void Reset()
        {
            _pages = null;
        }
    }
}
