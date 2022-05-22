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
        public static LogedInfo? Token { get; set; }
        public static string CurrentFolder { get; set; } = String.Empty;
        public static List<string> CurrentFiles { get; set; } = new List<string>();

        public static List<PdfToImageProcessing> CurrentFilesToProcess { get; set; } = new List<PdfToImageProcessing>();

        public class LogedInfo
        {
            public string Uid { get; set; } = String.Empty;
            public string Pwd { get; set; } = string.Empty;
            public string AccessToken { get; set; } = string.Empty;
        }
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

        static void LoadDataSaved()
        {
            try
            {
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
            }

            public string FilePdf { get; set; } = String.Empty;
            public int ParseStep { get; set; } 
            public string FileName { get; set; } = String.Empty;
            public string UploadStateText { get; set; } = string.Empty;

            public string ContextText { get; set; } = String.Empty;

            [JsonIgnore]
            public List<MyPdfPage> Pages { get; set; } = new List<MyPdfPage>();

            public Dictionary<string, string> PdfProperties { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, Dictionary<int, System.Drawing.Rectangle>> PdfPropertiesRegion { get; set; } = new Dictionary<string, Dictionary<int, System.Drawing.Rectangle>>();

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

                };

                ////temp.ConvertPagesImages();

                return temp;
            }
        }
    }
}
