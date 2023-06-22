using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
//using iText.IO.Image;
//using Spire.Pdf.Graphics;
//using Spire.Pdf;

using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using iText.IO.Image;
using SkiaSharp;


namespace PdfExtractor.Domains
{
    public class PdfToTextMrQuan
    {
        string GetFileName(string pathFile)
        {
            pathFile = pathFile.Replace("\\", "/");

            var f = new FileInfo(pathFile);

            return f.Name.Replace(f.Extension, string.Empty);
        }

        TesseractEngineWrapper img2textEngine = new TesseractEngineWrapper();


        public async Task Convert(string pdfPathFile, string folderToSaveImages = "")
        {
            await Task.Yield();
            var destDir = Path.Combine(folderToSaveImages, GetFileName(pdfPathFile));



            //var allpage = await pdfExtractor.ConvertToImage(pdfPathFile);

            //for (var i = 0; i < allpage.Count; i++)
            //{
            //    var bmp = allpage[i];
            //    string imagePath = Path.Combine(destDir, $"{i}.jpeg");
            //    bmp.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);

            //    await BoxingWordInPage(imagePath, i, destDir);
            //}

            PdfToImageProcessing pdfToImageProcessing = new PdfToImageProcessing(pdfPathFile);


            pdfToImageProcessing.ConvertToPagesImages();


            for (int i = 0; i < pdfToImageProcessing.Pages.Count; i++)
            {
                MyPdfPage img = pdfToImageProcessing.Pages[i];
                var bmp = img.PageImage;

                string imagePath = Path.Combine(destDir, $"{i}.jpeg");
                bmp.Save(imagePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                await BoxingWordInPage(imagePath, i, destDir);
            }


        }

        public async Task BoxingWordInPage(string imagePath, int pageIdx, string destDir)
        {
            await Task.Yield();

            var folderWordsInPage = Path.Combine(destDir, $"{pageIdx}");

            if(Directory.Exists(folderWordsInPage)==false) { Directory.CreateDirectory(folderWordsInPage); }

            List<string> textFoundInPage = new List<string>();

            using (var image = new Image<Bgr, byte>(imagePath))
            using (var gray = image.Convert<Gray, byte>())
            {
                using (var canny = gray.Canny(100, 60))
                using (var dilated = canny.Dilate(2))
                using (var contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(dilated, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                    List<PointF[]> vectors = new List<PointF[]>();

                    for (int i = 0; i < contours.Size; i++)
                    {
                        try
                        {

                            using (var contour = contours[i])
                            {
                                var box = CvInvoke.MinAreaRect(contour);

                                if (box.Size.Height < 10 || box.Size.Width < 10)
                                    continue;

                                if (box.Size.Height > 200)
                                    continue;

                                PointF[] vertices = box.GetVertices();

                                vectors.Add(vertices);

                                var pTopLeft = vertices[0];
                                var pBottomRight = vertices[2];
                                var x = (int)pTopLeft.X;
                                var y = (int)pTopLeft.Y;
                                var w = (int)(pBottomRight.X - pTopLeft.X);
                                var h = (int)(pBottomRight.Y - pTopLeft.Y);
                                var xdelta = w / 4;
                                var ydelta = h / 4;
                              
                                // Define the region of interest (ROI) for cropping
                                Rectangle roi = new Rectangle(x-xdelta, y-ydelta, w+xdelta, h+ydelta);
                                // Replace with the desired ROI coordinates and dimensions
                                // Crop the image using the ROI
                                Image<Bgr, byte> croppedImage = image.Copy(roi);
                                // Save the cropped image

                                var text = img2textEngine.TryFindText(croppedImage.ToJpegData());
                                if (!string.IsNullOrEmpty(text))
                                {
                                    textFoundInPage.Add(text);
                                }
                                var croppedImagePath = Path.Combine(folderWordsInPage, $"{i}.jpeg");
                                croppedImage.Save(croppedImagePath);                             

                            }

                            foreach(var vertices in vectors)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    CvInvoke.Line(image, Point.Round(vertices[j]), Point.Round(vertices[(j + 1) % 4]), new Bgr(Color.Red).MCvScalar, 2);

                                    //Rectangle bword = new Rectangle(x - xdelta, y + ydelta, x + xdelta, h - ydelta);
                                    //MCvScalar fillColor = new MCvScalar(0, 0, 0);
                                    //CvInvoke.Rectangle(image, bword, fillColor, -1);
                                }
                            }
                        }
                        catch (Exception e) { }

                    }
                }

                // Save the image with detected word boxes

                // Example: Save the image with bounding boxes (optional)
                string outputImagePath = Path.Combine(destDir, $"{pageIdx}_boxed.jpeg");
                image.Save(outputImagePath);
            }

            var filePageAllText = Path.Combine(destDir, $"{pageIdx}.txt");

            using (var sw = new StreamWriter(filePageAllText, false))
            {
                var s = string.Join(" ", textFoundInPage);
                sw.Write(s);
                sw.Flush();
            }
        }

        public async Task BoxingCharTextInPage(string imagePath, int pageIdx, string destDir)
        {
            await Task.Yield();


            using (var image = new Image<Bgr, byte>(imagePath))
            {
                // Convert image to grayscale
                var grayImage = image.Convert<Gray, byte>();

                // Apply adaptive thresholding to enhance text regions
                grayImage._ThresholdBinaryInv(new Gray(180), new Gray(255));

                // Find contours in the image
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(
                    grayImage,
                    contours,
                    null,
                    RetrType.List,
                    ChainApproxMethod.ChainApproxSimple);

                using (var clonedImage = image.Clone())
                {
                    // Iterate through the contours and crop the bounding boxes around text regions
                    for (int i = 0; i < contours.Size; i++)
                    {
                        var contour = contours[i];

                        // Skip small contours
                        if (CvInvoke.ContourArea(contour) < 100)
                            continue;

                        // Get the bounding rectangle around the contour
                        Rectangle boundingBox = CvInvoke.BoundingRectangle(contour);

                        // Draw the bounding rectangle on the cloned image (optional)
                        CvInvoke.Rectangle(clonedImage, boundingBox, new Bgr(Color.Red).MCvScalar, 2);

                        // Crop the text region using the bounding rectangle
                        var croppedImage = image.GetSubRect(boundingBox);


                        // Do something with the cropped image or recognized text
                        // For example, save the cropped image or process the recognized text

                        // Example: Save the cropped image
                        string croppedImagePath = Path.Combine(destDir, $"{pageIdx}_{i}_croped.jpeg");
                        croppedImage.Save(croppedImagePath);

                        // Perform OCR on the cropped image to extract the text (optional)

                        // Access the recognized text (optional)
                        var fileText = Path.Combine(destDir, $"{pageIdx}.txt");
                        string recognizedText = img2textEngine.TryFindText(croppedImage.ToJpegData(), "vie");
                        using (var sw = new StreamWriter(fileText, false))
                        {
                            sw.Write(recognizedText);
                            sw.Flush();
                        }

                    }
                }

                // Example: Save the image with bounding boxes (optional)
                string outputImagePath = Path.Combine(destDir, $"{pageIdx}_boxed.jpeg");
                image.Save(outputImagePath);
            }

        }

    }
}
