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
            InitializeComponent();

            lsvFiles.SelectionChanged += LsvFiles_SelectionChanged;
        }   

        private void LsvFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lsvFiles.SelectedItem != null)
            {
                var itm = (PdfToImageProcessing)lsvFiles.SelectedItem;
                this.Title = itm.FileName;
                var _ = Task.Run(() => {
                    itm.Prepare();                    
                    this.BindFilesToListView();
                });
                
            }
        }

        public List<PdfToImageProcessing> CurrentFiles { get; set; } =new List<PdfToImageProcessing>();

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
                        foreach (var f in MyAppContext.CurrentFiles)
                        {
                            PdfToImageProcessing item = new PdfToImageProcessing(f);

                            CurrentFiles.Add(item);
                        }

                        this.BindFilesToListView();

                    });
                }
            }
        }

        void BindFilesToListView()
        {
            Dispatcher.Invoke(() =>
            {
                lsvFiles.Items.Clear();

                foreach (var item in CurrentFiles) lsvFiles.Items.Add(item);

            });
         
        }

    }
}
