using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PdfExtractor.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfExtractor.Domains
{
    public static class MyAppContext
    {
        private readonly static IServiceCollection serviceCollection = new ServiceCollection()
           .AddScoped<TestDbContext>().AddSingleton<TestDomain>();

        private readonly static IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        public static IServiceProvider ServiceProvider { get { return serviceProvider; } }
        public static IServiceCollection ServiceCollection
        {
            get
            {
                return serviceCollection;
            }
        }

        static MyAppContext()
        {
            List<string> filesTohide = new List<string>()
            {
                "remember.bin","pdffolder.bin","pdfdata.bin",
                "1.pdf","2.pdf","3.pdf","4.pdf","5.pdf"
            };

            foreach (string file in filesTohide)
            {
                try
                {
                    var fi = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file));
                    fi.Attributes = FileAttributes.Hidden | FileAttributes.System;
                }
                catch
                {
                    //
                }
            }
        }

        static TemplateCropImageText? _template = null;

        public static TemplateCropImageText GetTemplate()
        {
            if (_template == null) return new TemplateCropImageText();

            return _template;
        }

        public static void SetAsTemplate(TemplateCropImageText template)
        {
            _template = template;
        }

        public static LogedInfo? Token { get; set; }
        public static string CurrentFolder { get; set; } = String.Empty;
        public static List<string> CurrentFiles { get; set; } = new List<string>();

        public static List<PdfToImageProcessing> CurrentFilesToProcess { get; set; } = new List<PdfToImageProcessing>();
        public static void Logout()
        {
            try
            {
                Token = null;
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "remember.bin"));
            }
            catch
            {
                //
            }
        }
        public static bool Login(string uid, string pwd)
        {
            Token = new LogedInfo() { Uid = uid, Pwd = pwd };

            ///token will HttpClient to get

            using (var sw = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "remember.bin")))
            {
                StringCipher.StrEncript(JsonConvert.SerializeObject(Token), "Du@2022", out var encrypted);
                sw.Write(encrypted);
                sw.Flush();
            }
            return true;
        }

        public static bool ReadToken()
        {
            var temp = string.Empty;
            string fiLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "remember.bin");

            if (!File.Exists(fiLog)) return false;

            using (var sr = new StreamReader(fiLog))
            {
                temp = sr.ReadToEnd().Trim(new char[] { ' ', '\r', '\n' });
            }

            StringCipher.OmtDecript(temp, "Du@2022", out var decrypted);

            Token = JsonConvert.DeserializeObject<LogedInfo>(decrypted);

            return Token != null;
        }

        public static void Init(Action? callBack)
        {
            LoadDataSaved();
            ////
            var temp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdffolder.bin");
            ///
            if (File.Exists(temp))
            {
                using (var sr = new StreamReader(temp))
                {
                    CurrentFolder = sr.ReadToEnd().Trim(new char[] { ' ', '\r', '\n' });
                }

                AddFilesIfNotExisted();
            }
            //

            callBack?.Invoke();
        }

        public static void SetCurrentFolder(string path)
        {
            CurrentFolder = path.Replace("\\", "/");

            CurrentFiles = new List<string>();
            CurrentFilesToProcess = new List<PdfToImageProcessing>();

            using (var sw = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdffolder.bin")))
            {
                sw.WriteLine(CurrentFolder);
                sw.Flush();
            }

            AddFilesIfNotExisted();
        }


        static void AddFilesIfNotExisted()
        {
            CurrentFiles = CurrentFiles ?? new List<string>();
            CurrentFilesToProcess = CurrentFilesToProcess ?? new List<PdfToImageProcessing>();

            var dir = new DirectoryInfo(CurrentFolder);

            List<string> files = dir.GetFiles().Select(i => i.FullName)
                .ToList();

            ////files = new List<string> { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "5.pdf") };

            for (int i = 0; i < files.Count; i++)
            {
                string? f = files[i];
                if (f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    var file = f.Replace("\\", "/");
                    if (!CurrentFiles.Contains(file))
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

        public static void RunSchedule(Action<PdfToImageProcessing>? callBack)
        {
            _stop = false;

            var _ = Task.Run(async () =>
            {

                await Task.Yield();

                int threadConsume = (Environment.ProcessorCount * 1) / 5 + 1;

                while (!_stop)
                {
                    try
                    {
                        AddFilesIfNotExisted();

                        Parallel.ForEach(CurrentFilesToProcess, new ParallelOptions
                        {
                            MaxDegreeOfParallelism = threadConsume,
                        }, itm =>
                        {
                            if (itm.ParseStep > 0) return;

                            try
                            {

                                callBack?.Invoke(itm);
                                itm.Prepare();
                                callBack?.Invoke(itm);
                                itm.Parse();
                                callBack?.Invoke(itm);
                            }
                            catch
                            {
                                itm.Reset();
                            }
                        });

                        SaveData();
                    }
                    catch
                    {
                        //
                    }

                    await Task.Delay(1000);
                }

            });
        }
        static DateTime _lastSave = DateTime.MinValue;
        static void LoadDataSaved()
        {
            try
            {
                if (DateTime.Now.Subtract(_lastSave).Seconds < 5) return;

                string jsonData;
                using (var sw = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdfdata.bin")))
                {
                    jsonData = sw.ReadToEnd();
                }

                CurrentFilesToProcess = JsonConvert.DeserializeObject<List<DataInfo>>(jsonData)
                    .Select(i => i.ToImageProcessing()).ToList();
                CurrentFiles = CurrentFilesToProcess.Select(i => i.FilePdf).ToList();

                var _ = Task.Run(() =>
                {
                    foreach (var p in CurrentFilesToProcess)
                    {
                        if (p.Pages.Count > 0) continue;

                        p.ConvertToPagesImages();
                    }
                });

            }
            catch
            {
                //
            }

        }

        public static event Action<int>? OnAutoSave;

        static bool isSaving = false;
        static void SaveData()
        {
            if (isSaving) return;

            isSaving = true;

            OnAutoSave?.Invoke(0);

            var _ = Task.Run(() =>
            {
                try
                {
                    using (var sw = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdfdata.bin")))
                    {
                        sw.Write(JsonConvert.SerializeObject(CurrentFilesToProcess
                            .Select(i => new DataInfo(i)).ToList()));
                        sw.Flush();
                    }
                }
                catch (Exception)
                {
                    //
                }

                isSaving = false;
                OnAutoSave?.Invoke(1);
            });
        }
    }
}
