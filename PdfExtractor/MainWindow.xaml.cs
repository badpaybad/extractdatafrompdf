using PdfExtractor.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PdfExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            MyAppContext.Init(() =>
            {
                this.BindFilesToListView();
                this.BindCurrentPdfPreview();
            });

            InitializeComponent();

            lsvFiles.SelectionChanged += LsvFiles_SelectionChanged;
            
            //https://docs.google.com/spreadsheets/d/1Q3yQxR7sVtrCBa_HurPo8Ur2XSveaelMf7eZdDDeWDE/edit#gid=0

            lsvCurrentPdf.SelectionChanged += LsvCurrentPdf_SelectionChanged;

            btnTryParse.Click += BtnTryParse_Click;
            btnTryUpload.Click += BtnTryUpload_Click;

            btnRetryParseFailed.Click += BtnRetryParseFailed_Click;

            btnUpdateModfiyBellow.Click += BtnUpdateModfiyBellow_Click;

            txtFolder.Text = MyAppContext.CurrentFolder;

            MyAppContext.Run((itm) =>
            {
                var _ = Task.Run(() =>
                {
                    this.BindFilesToListView();

                    this.BindCurrentPdfPreview();
                });
            });
        }

        private void BtnUpdateModfiyBellow_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPdf == null) return;

            _currentPdf.SetProperty("Code", txtCode.Text, null);
            _currentPdf.SetProperty("Title", txtTitle.Text, null);
            _currentPdf.SetProperty("SignedBy", txtSignedBy.Text, null);
        }

        void DispatcherInvoke(Action callback)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    callback();
                }
                catch (Exception)
                {
                    //
                }
            });
        }

        MyPdfPage _currentPageInPdf;
        private void LsvCurrentPdf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lsvCurrentPdf.SelectedItem != null)
            {
                var itm = (MyPdfPage)lsvCurrentPdf.SelectedItem;

                _currentPageInPdf = itm;
            }
        }
        public void lsvViewImage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPageInPdf == null && lsvCurrentPdf.Items.Count > 0) _currentPageInPdf = (MyPdfPage)lsvCurrentPdf.Items[0];

            if (_currentPageInPdf != null && _currentPdf != null)
            {
                var frm = new ViewAndBoxingImageWindow(_currentPdf, _currentPageInPdf);
                frm.ShowDialog();

                this.BindFilesToListView();
                this.BindCurrentPdfPreview();
                BindCurrentPdfDetails();
            }
        }

        private void BtnRetryParseFailed_Click(object sender, RoutedEventArgs e)
        {
            MyAppContext.Run((itm) =>
            {
                var _ = Task.Run(() =>
                {
                    this.BindFilesToListView();

                    this.BindCurrentPdfPreview();
                });
            });
        }

        private void BtnTryUpload_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPdf == null) return;
        }

        private void BtnTryParse_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPdf == null) return;

            btnTryParse.Content = "PARSING ...";
            txtPdfContentText.Text = "PARSING ...";

            var _ = Task.Run(() =>
            {
                _currentPdf.Parse();

                this.BindFilesToListView();
                this.BindCurrentPdfPreview();

                DispatcherInvoke(() =>
                {
                    btnTryParse.Content = "Try parse";

                    txtPdfContentText.Text = _currentPdf.ContextText;
                });
            });

        }


        PdfToImageProcessing? _currentPdf;

        private void LsvFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lsvFiles.SelectedItem != null)
            {
                var itm = (PdfToImageProcessing)lsvFiles.SelectedItem;

                DispatcherInvoke(() =>
                {
                    lsvCurrentPdf.Items.Clear();

                    lblCurrentPdf.Content = itm.FileName + " WAITING ...";
                    txtPdfContentText.Text = String.Empty;
                });

                var _ = Task.Run(() =>
                {
                    itm.Prepare();

                    this.BindFilesToListView();

                    _currentPdf = itm;

                    _currentPdf.OnSetProperty += (a, b, c) =>
                    {
                        this.BindFilesToListView();
                        this.BindCurrentPdfPreview();
                        BindCurrentPdfDetails();
                    };

                    this.BindCurrentPdfPreview();

                    DispatcherInvoke(() =>
                    {
                        txtPdfContentText.Text = "PARSING ...";
                    });

                    var _ = Task.Run(() =>
                    {

                        _currentPdf.Parse();

                        this.BindFilesToListView();

                        this.BindCurrentPdfPreview();

                        BindCurrentPdfDetails();
                    });

                });

            }
        }

        void BindCurrentPdfDetails()
        {
            if (_currentPdf == null) return;

            var _ = Task.Run(() =>
            {
                DispatcherInvoke(() =>
                {
                    btnTryParse.Content = "Try parse";

                    txtPdfContentText.Text = _currentPdf.ContextText;

                    _currentPdf.PdfProperties.TryGetValue("Code", out var code);
                    txtCode.Text = code;

                    _currentPdf.PdfProperties.TryGetValue("Title", out var title);
                    txtTitle.Text = title;

                    _currentPdf.PdfProperties.TryGetValue("SignedBy", out var signedBy);
                    txtSignedBy.Text = signedBy;

                    _currentPdf.ContextText = txtPdfContentText.Text;

                });
            });

        }

        void BindCurrentPdfPreview()
        {
            var _ = Task.Run(() =>
              {
                  if (_currentPdf == null) return;

                  DispatcherInvoke(() =>
                  {
                      lblCurrentPdf.Content = _currentPdf.FileName;
                      lblCurrentPdfParseStatus.Content = _currentPdf.ParseStepText;
                      lblCurrentPdfUploadStatus.Content = _currentPdf.UploadStateText;

                      lsvCurrentPdf.Items.Clear();
                      foreach (var p in _currentPdf.Pages)
                      {
                          lsvCurrentPdf.Items.Add(p);
                      }
                  });
              });
        }


        private void btnChangeFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    MyAppContext.SetCurrentFolder(dialog.SelectedPath);
                    txtFolder.Text = MyAppContext.CurrentFolder;

                    var _ = Task.Run(() =>
                    {

                        this.BindFilesToListView();

                    });
                }
            }
        }

        void BindFilesToListView()
        {
            if (lsvFiles == null) return;

            var _ = Task.Run(() =>
            {
                DispatcherInvoke(() =>
                {
                    lsvFiles.Items.Clear();

                    foreach (var item in MyAppContext.CurrentFilesToProcess) lsvFiles.Items.Add(item);

                });
            });
        }

    }
}
