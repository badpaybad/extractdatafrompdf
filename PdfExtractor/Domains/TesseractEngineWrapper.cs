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
        static ConcurrentDictionary<string, TesseractEngine> _engine= new ConcurrentDictionary<string, TesseractEngine>();
        
        public string TryFindText(byte [] byteStream, string lang = "eng")
        {
            var folderData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
                string ocrtext = string.Empty;

            if(!_engine.TryGetValue(lang, out var _tesseractEngine))
            {
                _tesseractEngine =  new TesseractEngine(folderData, lang);
                _engine.TryAdd(lang, _tesseractEngine);
            }

            //using (TesseractEngine _tesseractEngine = new TesseractEngine(folderData, lang))
            //{
                using (var xxx = new MemoryStream())
                {
                    using (var bmp = new Bitmap(new MemoryStream(byteStream)))
                    {
                        bmp.Save(xxx, System.Drawing.Imaging.ImageFormat.Tiff);

                        using (Pix pix = Pix.LoadTiffFromMemory(xxx.ToArray()))                        
                        {
                            using (var page = _tesseractEngine.Process(pix))
                            {
                                ocrtext = page.GetText();
                            }
                            //pix.Save(fileImage + ".png", ImageFormat.Png);
                        }
                    }                       
                }                    
            //}

            return ocrtext;
        }

    }
}
