using PdfExtractor.Domains;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PdfExtractor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var testpdf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "4.pdf");

            var _ = Task.Run(async () => {

                var pdfPages = await new Domains.PdfExtractor().GetPageInfos(testpdf);
            });
        }
    }
}
