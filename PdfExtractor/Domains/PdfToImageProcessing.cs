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

        List<byte[]> _images = new List<byte[]>();
        TesseractEngineWrapper _ocr;

        public PdfToImageProcessing(string filePdf)
        {
            _filepdf = filePdf;

            _ocr = new TesseractEngineWrapper();           
        }

        public void ConvertToImage()
        {
            _images = Freeware.Pdf2Png.ConvertAllPages(File.OpenRead(_filepdf));
        }

        public void GetText()
        {
            List<string> text = new List<string>();

            //Parallel.ForEach(_images, img =>
            //{
            //    var xxx = _ocr.TryFindText(img, "vie");
            //    text.Add(xxx);
            //});

            foreach(var img in _images)
            {
                var xxx = _ocr.TryFindText(img, "vie");
                text.Add(xxx);
            }
        }

    }
}
