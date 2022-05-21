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
            if (_currentPageInPdf == null && lsvCurrentPdf.Items.Count>0) _currentPageInPdf = (MyPdfPage)lsvCurrentPdf.Items[0];

            if (_currentPageInPdf != null && _currentPdf!=null)
            {
                var frm= new ViewAndBoxingImageWindow(_currentPdf, _currentPageInPdf);
                frm.ShowDialog();
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

            var _ = Task.Run(() => {
                _currentPdf.Parse();

                this.BindFilesToListView();
                this.BindCurrentPdfPreview();

                Dispatcher.Invoke(() =>
                {
                    btnTryParse.Content = "Try parse";

                    txtPdfContentText.Text = String.Join("\r\n\r\n", _currentPdf.Pages.Select(i => i.ContentText));
                });
            });
            
        }


        PdfToImageProcessing? _currentPdf;

        private void LsvFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lsvFiles.SelectedItem != null)
            {
                var itm = (PdfToImageProcessing)lsvFiles.SelectedItem;

                Dispatcher.Invoke(() => {
                    lsvCurrentPdf.Items.Clear();

                    lblCurrentPdf.Content = itm.FileName +" WAITING ...";
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

                    Dispatcher.Invoke(() =>
                    {
                        txtPdfContentText.Text = "PARSING ...";
                    });                    

                    var _ = Task.Run(() => {

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

            var _ = Task.Run(() => {


                Dispatcher.Invoke(() =>
                {
                    btnTryParse.Content = "Try parse";

                    txtPdfContentText.Text = String.Join("\r\n\r\n", _currentPdf.Pages.Select(i => i.ContentText));

                    
                });
            });

        }

        void BindCurrentPdfPreview()
        {
            var _ = Task.Run(() =>
              {
                  if (_currentPdf == null) return;

                  Dispatcher.Invoke(() =>
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
            var _ = Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    lsvFiles.Items.Clear();

                    foreach (var item in MyAppContext.CurrentFilesToProcess) lsvFiles.Items.Add(item);

                });
            });
        }

    }
}
