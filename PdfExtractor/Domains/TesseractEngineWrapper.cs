using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace PdfExtractor.Domains
{
    public class TesseractEngineWrapper
    {
        //static ConcurrentDictionary<string, TesseractEngine> _engine = new ConcurrentDictionary<string, TesseractEngine>();

        string folderData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
        static char[] _trimChars = { ' ', '\r', '\n' };
        public TesseractEngineWrapper()
        {

        }

        public string TryFindText(byte[] byteStream, string lang = "eng")
        {
            using (var bmp = new Bitmap(new MemoryStream(byteStream)))
            {
                return TryFindText(bmp, lang);
            }
        }

        public string TryFindText(Bitmap bmp, string lang = "eng")
        {
            string ocrtext = string.Empty;

            //if (!_engine.TryGetValue(lang, out var tesseractEngine))
            //{
            // 
            //    tesseractEngine = new TesseractEngine(folderData, lang);
            //    _engine.TryAdd(lang, tesseractEngine);
            //}
            using (var tesseractEngine = new TesseractEngine(folderData, lang))
            using (var xxx = new MemoryStream())
            {
                bmp.Save(xxx, System.Drawing.Imaging.ImageFormat.Tiff);

                using (Pix pix = Pix.LoadTiffFromMemory(xxx.ToArray()))
                {
                    using (var page = tesseractEngine.Process(pix))
                    {
                        ocrtext =NormalizeText( page.GetText());
                    }
                }
            }

            return ocrtext.Trim(_trimChars);
        }

        static char[] _split = { ' ', '\r', '\n' };
        public static string NormalizeText(string text)
        {
            text=text??string.Empty;

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

    }
}
