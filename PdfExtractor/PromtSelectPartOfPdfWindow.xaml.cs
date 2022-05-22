using PdfExtractor.Domains;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace PdfExtractor
{
    /// <summary>
    /// Interaction logic for PromtSelectPartOfPdfWindow.xaml
    /// </summary>
    public partial class PromtSelectPartOfPdfWindow : Window
    {
        System.Drawing.Bitmap _croped;
        PdfToImageProcessing _filPdf; MyPdfPage _pagePdf;
        public PromtSelectPartOfPdfWindow(System.Drawing.Bitmap croped, PdfToImageProcessing filPdf, MyPdfPage pagePdf)
        {
            _filPdf = filPdf;
            _pagePdf = pagePdf; 
            _croped = croped;

            InitializeComponent();

            imgCroped.Source = ConvertFromBmp(_croped);


            btnOk.Click += BtnOk_Click;

            btnOk.IsEnabled = false;
            txtCroped.Text = "Parsing ... ";

            var _ = Task.Run(() =>
            {
                var text = new TesseractEngineWrapper().TryFindText(_croped, "vie");

                Dispatcher.Invoke(() =>
                {
                    txtCroped.Text = text;
                    btnOk.IsEnabled = true;
                });
            });
        }
        public string ResponseType { get {
                var temp= Descendants<RadioButton>((DependencyObject) gridMain).Where(i=>i.IsChecked==true).
                    Select(i=>i.Content.ToString()).FirstOrDefault();

                return temp;
            } }
        public string ResponseText { get { return txtCroped.Text; } }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            //if (_filPdf.PdfProperties.ContainsKey(ResponseType))
            //{
            //    if(System.Windows.MessageBox.Show($"{ResponseType} : Existed, REPLACE?", $"{ResponseType} : Existed, REPLACE?") 
            //        != MessageBoxResult.OK)
            //    {
            //        txtCroped.Text = String.Empty;
            //        DialogResult = false;
            //        return;
            //    }
            //}

            DialogResult = true;
        }

        BitmapImage ConvertFromBmp(System.Drawing.Bitmap bmp)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);

                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        IEnumerable<T> Descendants<T>(DependencyObject dependencyItem) where T : DependencyObject
        {
            if (dependencyItem != null)
            {
                for (var index = 0; index < VisualTreeHelper.GetChildrenCount(dependencyItem); index++)
                {
                    var child = VisualTreeHelper.GetChild(dependencyItem, index);
                    if (child is T dependencyObject)
                    {
                        yield return dependencyObject;
                    }

                    foreach (var childOfChild in Descendants<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }


    }
}
