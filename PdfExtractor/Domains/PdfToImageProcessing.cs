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

        public string ParseStepText
        {
            get
            {
                if (ParseStep == 0) return "Waiting";
                if (ParseStep == 1) return "Prepared";

                if (ParseStep == 2) return "Success";

                if (ParseStep == -1) return "Failed";

                return string.Empty;
            }
        }

        public List<MyPdfPage> Pages { get { return _pages; } }

        readonly string _filepdf = string.Empty;

        TesseractEngineWrapper _ocr;

        List<MyPdfPage> _pages = new List<MyPdfPage>();

        private readonly int _threadConsume = 1;

        public PdfToImageProcessing(string filePdf)
        {
            _threadConsume = (Environment.ProcessorCount * 2) / 3 + 1;

            _filepdf = filePdf.Replace("\\", "/");

            var idx = _filepdf.LastIndexOf('/');
            if (idx > 0) FileName = _filepdf.Substring(idx + 1);
            else FileName = _filepdf;

            _ocr = new TesseractEngineWrapper();
            ParseStep = 0;
        }

        public List<MyPdfPage> Prepare()
        {
            ParseStep = 0;
            _pages = Freeware.Pdf2Png.ConvertAllPages(File.OpenRead(_filepdf))
                .Select((i, idx) =>
                {
                    var ps = new MemoryStream(i);
                    var p = new MyPdfPage
                    {
                        PageStream = ps,
                        PageBytes = i,
                        PageIndex = idx,
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

            if (pres.Any(i => i == -1)) ParseStep = -1;

            ParseStep = 2;
        }

    }
}
