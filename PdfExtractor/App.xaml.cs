using Microsoft.Extensions.DependencyInjection;
using PdfExtractor.Domains;
using PdfExtractor.Test;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
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
            this.DispatcherUnhandledException += (s, e) =>
            {
                if (e != null && e.Exception != null)
                {
                    Console.WriteLine(e.Exception.Message);
                    Console.WriteLine(e.Exception.StackTrace);
                }
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                if (e != null && e.ExceptionObject != null)
                {   
                    Console.WriteLine(e.ExceptionObject);
                }
            };
            base.OnStartup(e);
            
            // var testpdf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "4.pdf");

            //var x= new PdfToImageProcessing(testpdf);

            // x.Parse();

            ////

            ////var xxx= Freeware.Pdf2Png.ConvertAllPages(File.OpenRead(testpdf));

            ////for (int i = 0; i < xxx.Count; i++)
            ////{
            ////    byte[]? x = xxx[i];
            ////    new Bitmap(new MemoryStream(x)).Save($"D:/{i}.png");
            ////}

            ////var _ = Task.Run(async () => {

            ////    var pdfPages = await new Domains.PdfExtractor().GetPageInfos(testpdf);
            ////});
        }
    }
}
