using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PdfExtractor.Domains
{
    public class DataInfo
    {
        public DataInfo()
        {
            //
        }

        public DataInfo(PdfToImageProcessing src)
        {
            FilePdf = src.FilePdf;
            FileName = src.FileName;
            ParseStep = src.ParseStep;
            UploadStateText = src.UploadStateText;
            Pages = src.Pages;
            PdfProperties = src.PdfProperties;
            PdfPropertiesRegion = src.PdfPropertiesRegion;
            ContextText = src.ContextText;
            RatioResize = src.RatioResize;
        }
        public int RatioResize { get; set; }
        public string FilePdf { get; set; } = String.Empty;
        public int ParseStep { get; set; }
        public string FileName { get; set; } = String.Empty;
        public string UploadStateText { get; set; } = string.Empty;

        public string ContextText { get; set; } = String.Empty;

        [JsonIgnore]
        public List<MyPdfPage> Pages { get; set; } = new List<MyPdfPage>();

        public Dictionary<string, string> PdfProperties { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<int, System.Drawing.Rectangle?>> PdfPropertiesRegion { get; set; } = new Dictionary<string, Dictionary<int, System.Drawing.Rectangle?>>();

        public PdfToImageProcessing ToImageProcessing()
        {
            var filePdf = FilePdf;

            var temp = new PdfToImageProcessing(filePdf)
            {
                FileName = this.FileName,
                ParseStep = this.ParseStep,
                UploadStateText = this.UploadStateText,
                PdfProperties = this.PdfProperties,
                PdfPropertiesRegion = this.PdfPropertiesRegion,
                ContextText = this.ContextText,
                RatioResize = this.RatioResize,
            };

            ////temp.ConvertPagesImages();

            return temp;
        }
    }

}
