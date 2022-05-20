using Newtonsoft.Json;
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

        public static List<PdfToImageProcessing> CurrentFilesToProcess { get; set; } = new List<PdfToImageProcessing>();

        static MyAppContext()
        {
            ////
            var temp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdffolder.bin");
            ///
            if (File.Exists(temp))
            {
                using (var sr = new StreamReader(temp))
                {
                    CurrentFolder = sr.ReadToEnd();
                }
            }
            //
        }

        public static void SetCurrentFolder(string path)
        {
            CurrentFolder = path.Replace("\\", "/");

            using (var sw = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdffolder.bin")))
            {
                sw.WriteLine(CurrentFolder);
                sw.Flush();
            }

            BuildFiles();
        }

        private static void BuildFiles()
        {
            var dir = new DirectoryInfo(CurrentFolder);

            CurrentFiles = new List<string>();
            CurrentFilesToProcess = new List<PdfToImageProcessing>();

            foreach (var f in dir.GetFiles())
            {
                if (f.FullName.IndexOf(".pdf", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    CurrentFiles.Add(f.FullName.Replace("\\", "/"));
                }
            }


            foreach (var f in MyAppContext.CurrentFiles)
            {
                PdfToImageProcessing item = new PdfToImageProcessing(f);

                CurrentFilesToProcess.Add(item);
            }
        }

        static bool _stop = false;

        public static void Stop()
        {
            _stop = true;
        }

        public static void Run(Action<PdfToImageProcessing>? callBack)
        {
            _stop = false;

            var _ = Task.Run(async () =>
            {

                await Task.Yield();

                while (!_stop)
                {
                    try
                    {
                        Parallel.ForEach(CurrentFilesToProcess, new ParallelOptions
                        {
                            MaxDegreeOfParallelism = (int)(Environment.ProcessorCount * 2) / 3,
                        }, itm =>
                        {
                            callBack?.Invoke(itm);
                            itm.Prepare();
                            callBack?.Invoke(itm);
                            itm.Parse();
                            callBack?.Invoke(itm);
                        });
                    }
                    catch (Exception ex)
                    {
                        //
                    }

                    await Task.Delay(1000);
                }

            });
        }
    }
}
