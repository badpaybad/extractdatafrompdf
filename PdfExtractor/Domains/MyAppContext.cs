using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfExtractor.Domains
{
    internal static class MyAppContext
    {
        public static string Token { get; set; }
        public static string CurrentFolder { get; set; }
        public static List<string> CurrentFiles { get; set; }
        public static void SetCurrentFolder(string path)
        {
            CurrentFolder = path.Replace("\\","/");

            var dir= new DirectoryInfo(CurrentFolder);

            CurrentFiles = new List<string>();

            foreach (var f in dir.GetFiles())
            {
                if(f.FullName.IndexOf(".pdf", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    CurrentFiles.Add(f.FullName.Replace("\\", "/"));
                }
            }
        }
    }
}
