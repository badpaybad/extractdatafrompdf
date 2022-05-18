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
        string _filepdf = string.Empty;

        TesseractEngineWrapper _ocr;

        List<MyPdfPage> _pages = new List<MyPdfPage>();
        int _threadConsume = 1;
        public PdfToImageProcessing(string filePdf)
        {
            _threadConsume = (Environment.ProcessorCount * 2) / 3 + 1;

            _filepdf = filePdf;

            _ocr = new TesseractEngineWrapper();
        }

        public void Parse()
        {
            _pages = Freeware.Pdf2Png.ConvertAllPages(File.OpenRead(_filepdf))
                .Select((i, idx) =>
                {
                    var p = new MyPdfPage
                    {
                        PageBytes = i,
                        PageIndex = idx,
                        ContentImages = new List<MemoryStream>(),
                    };
                    return p;
                }).OrderBy(i => i.PageIndex).ToList();

            Parallel.ForEach(_pages, new ParallelOptions { MaxDegreeOfParallelism = _threadConsume },
                p =>
            {
                if (p.PageBytes == null) return;

                var ps = new MemoryStream(p.PageBytes);
                p.PageStream = ps;
                p.PageImage = new System.Drawing.Bitmap(ps);
                p.ContentText = _ocr.TryFindText(p.PageImage, "vie");
            });
        }

    }
}
