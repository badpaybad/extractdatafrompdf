using System.Collections.Generic;

namespace PdfExtractor.Domains
{
    public class TemplateCropImageText
    {
        public int RatioResize { get; set; }
        public Dictionary<string, Dictionary<int, System.Drawing.Rectangle?>> CropArea { get; set; } = new Dictionary<string, Dictionary<int, System.Drawing.Rectangle?>>();
    }
}
