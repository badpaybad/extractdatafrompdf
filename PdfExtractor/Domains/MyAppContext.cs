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

        public static void Init(Action? callBack)
        {
            ////
            var temp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdffolder.bin");
            ///
            if (File.Exists(temp))
            {
                using (var sr = new StreamReader(temp))
                {
                    CurrentFolder = sr.ReadToEnd().Trim(new char[] {' ','\r','\n'});
                }

                BuildFiles();
            }
            //

            callBack?.Invoke();
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

            List<string> files = dir.GetFiles().Select(i=>i.FullName).ToList();

            //files = new List<string> { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "5.pdf") };
          
            foreach (var f in files)
            {
                if (f.IndexOf(".pdf", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    string file = f.Replace("\\", "/");
                    
                    CurrentFiles.Add(file);

                    PdfToImageProcessing item = new PdfToImageProcessing(file);
                    CurrentFilesToProcess.Add(item);
                }
            }

        }

        static void AddFileIfNotExisted()
        {
            var dir = new DirectoryInfo(CurrentFolder);


            List<string> files = dir.GetFiles().Select(i => i.FullName).ToList();

            //files = new List<string> { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "5.pdf") };

            foreach (var f in files)
            {
                if (f.IndexOf(".pdf", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    var file = f.Replace("\\", "/");
                    if(!CurrentFiles.Contains(file))
                    {
                        CurrentFiles.Add(file);

                        PdfToImageProcessing item = new PdfToImageProcessing(file);
                        CurrentFilesToProcess.Add(item);
                    }
                }
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

                int threadConsume = (int)(Environment.ProcessorCount * 1) / 5 +1;

                while (!_stop)
                {
                    try
                    {
                        AddFileIfNotExisted();

                        Parallel.ForEach(CurrentFilesToProcess, new ParallelOptions
                        {
                            MaxDegreeOfParallelism = threadConsume,
                        }, itm =>
                        {
                            if (itm.ParseStep > 0) return;

                            try {

                                callBack?.Invoke(itm);
                                itm.Prepare();
                                callBack?.Invoke(itm);
                                itm.Parse();
                                callBack?.Invoke(itm);
                            }
                            catch {
                                itm.Reset();
                            }
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
