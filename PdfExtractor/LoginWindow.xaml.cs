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
using System.Windows.Shapes;

namespace PdfExtractor
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            LoadExistedToken();

            InitializeComponent();

            btnLogin.Click += BtnLogin_Click;

            this.Closing += LoginWindow_Closing;
        }   

        private void LoginWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
           //
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = MyAppContext.Login(txtUid.Text,txtPwd.Text);
        }

        void LoadExistedToken()
        {
            //
        }
    }
}
