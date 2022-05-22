using PdfExtractor.Domains;
using System;
using System.Collections.Generic;
using System.Drawing;
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
    /// Interaction logic for ViewAndBoxingImageWindow.xaml
    /// </summary>
    public partial class ViewAndBoxingImageWindow : Window
    {
        PdfToImageProcessing _imageMain;
        MyPdfPage _imageClicked;

        Bitmap _tempBitmap;

        bool mouseDown = false; // Set to 'true' when mouse is held down.
        System.Windows.Point mouseDownPos; // The point where the mouse button was clicked down.

        IInputElement inputElement;

        int _ratioResize = 4;

        public ViewAndBoxingImageWindow(PdfToImageProcessing imageMain, MyPdfPage imageClicked)
        {
            _imageMain = imageMain;

            _imageMain.OnSetProperty += _imageMain_OnSetProperty;

            _imageClicked = imageClicked;

            InitializeComponent();

            canvasBgImage.Width = _imageClicked.PageImage.Width / _ratioResize;
            canvasBgImage.Height = _imageClicked.PageImage.Height / _ratioResize;

            canvasContainer.Width = canvasBgImage.Width;
            canvasContainer.Height = canvasBgImage.Height;

            mainGrid.Width = canvasContainer.Height;
            this.Height = canvasContainer.Height;

            mainGrid.Width = canvasContainer.Width*1.3;
            this.Width = mainGrid.Width;

            this.ResizeMode = ResizeMode.NoResize;

            _tempBitmap = new Bitmap(_imageClicked.PageImage, (int)canvasBgImage.Width, (int)canvasBgImage.Height);

            canvasBgImage.Source = ConvertFromBmp(_tempBitmap);

            inputElement = (IInputElement)canvasContainer;

            canvasBgImage.MouseDown += CanvasImageMain_MouseDown;
            canvasBgImage.MouseUp += CanvasImageMain_MouseUp;
            canvasBgImage.MouseMove += CanvasImageMain_MouseMove;

            DrawBoxes();
            BindingListViewRight();
        }

        private void _imageMain_OnSetProperty(string arg1, string arg2, System.Drawing.Rectangle? arg3)
        {

        }

        BitmapImage ConvertFromBmp(Bitmap bmp)
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

        private void CanvasImageMain_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            mouseDownPos = e.GetPosition(inputElement);
            canvasBgImage.CaptureMouse();

            // Initial placement of the drag selection box.         
            Canvas.SetLeft(selectionBox, mouseDownPos.X);
            Canvas.SetTop(selectionBox, mouseDownPos.Y);
            selectionBox.Width = 0;
            selectionBox.Height = 0;

            // Make the drag selection box visible.
            selectionBox.Visibility = Visibility.Visible;
        }
        private void CanvasImageMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                // When the mouse is held down, reposition the drag selection box.

                System.Windows.Point mousePos = e.GetPosition(inputElement);

                if (mouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(selectionBox, mouseDownPos.X);
                    selectionBox.Width = mousePos.X - mouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = mouseDownPos.X - mousePos.X;
                }

                if (mouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, mouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - mouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = mouseDownPos.Y - mousePos.Y;
                }
            }
        }

        private void CanvasImageMain_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {

                // Release the mouse capture and stop tracking it.
                mouseDown = false;
                canvasBgImage.ReleaseMouseCapture();

                System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)Canvas.GetLeft(selectionBox), (int)Canvas.GetTop(selectionBox), (int)selectionBox.Width, (int)selectionBox.Height);

                var prmt = new PromtSelectPartOfPdfWindow(CropImage(rect),_imageMain,_imageClicked);

                if (prmt.ShowDialog() == true)
                {
                    if (!string.IsNullOrEmpty(prmt.ResponseText) && !string.IsNullOrEmpty(prmt.ResponseType))
                    {                        
                        _imageMain.SetProperty(prmt.ResponseType, prmt.ResponseText, rect, _imageClicked.PageIndex);
                    }
                }

                // Hide the drag selection box.
                selectionBox.Visibility = Visibility.Collapsed;

                var mouseUpPos = e.GetPosition(inputElement);

                // TODO: 
                //
                // The mouse has been released, check to see if any of the items 
                // in the other canvas are contained within mouseDownPos and 
                // mouseUpPos, for any that are, select them!
                //
                DrawBoxes();

                BindingListViewRight();
            }
            catch (Exception)
            {
                //
            }
        }
        private Bitmap CropImage(System.Drawing.Rectangle cropArea)
        {
            var bmpImage = _imageClicked.PageImage;

            return bmpImage.Clone(new System.Drawing.Rectangle((int)cropArea.X * _ratioResize, (int)cropArea.Y * _ratioResize, (int)cropArea.Width * _ratioResize, (int)cropArea.Height * _ratioResize), bmpImage.PixelFormat);
        }

        System.Drawing.Color _color = System.Drawing.Color.Red;
        void DrawBoxes()
        {
            _tempBitmap = new Bitmap(_imageClicked.PageImage, (int)canvasBgImage.Width, (int)canvasBgImage.Height);

            using (Graphics graphics = Graphics.FromImage(_tempBitmap))
                foreach (var boxX in _imageMain.PdfPropertiesRegion)
                {
                    var box=boxX.Value.FirstOrDefault();

                    if (_imageClicked.PageIndex != box.Key) continue;

                    graphics.DrawRectangle(new System.Drawing.Pen(new System.Drawing.SolidBrush(_color), 2), box.Value);
                    graphics.DrawString(boxX.Key, new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Regular)
                        , new SolidBrush(_color), box.Value.X, box.Value.Y);

                    graphics.DrawString(_imageMain.PdfProperties[boxX.Key], new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Regular)
                        , new SolidBrush(_color), box.Value.X, box.Value.Y + box.Value.Height);
                }

            canvasBgImage.Source = ConvertFromBmp(_tempBitmap);
        }

        void BindingListViewRight()
        {
            DispatcherInvoke(() =>
            {
                lsvPdfProps.Items.Clear();

                foreach (var i in _imageMain.PdfProperties)
                {
                    lsvPdfProps.Items.Add(new MyKeyValue { Key = i.Key, Value = i.Value
                        ,PageIndex= _imageMain.PdfPropertiesRegion[i.Key].FirstOrDefault().Key
                    });
                }
            });
        }

        void btnDelete_OnClick(object sender, RoutedEventArgs e)
        {
            MyKeyValue val = new MyKeyValue();
            if (lsvPdfProps.SelectedItem == null)
            {
                if (lsvPdfProps.Items.Count > 0)
                    val = (MyKeyValue)lsvPdfProps.Items[0];
            }
            else
            {
                val = (MyKeyValue)lsvPdfProps.SelectedItem;
            }

            if (!string.IsNullOrEmpty(val.Key))
            {
                _imageMain.PdfPropertiesRegion.Remove(val.Key);
                _imageMain.PdfProperties.Remove(val.Key);
            }

            DrawBoxes();

            BindingListViewRight();
        }


        void DispatcherInvoke(Action a)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    a();
                }
                catch (Exception)
                {
                    //
                }
            });
        }


        public class MyKeyValue
        {
            public string Key { get; set; }
            public string Value { get; set; }

            public int PageIndex { get; set; }
        }
    }
}
