using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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


namespace WykladDoPDF
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PreviewImageButton_Click(object sender, RoutedEventArgs e)
        {
            var source = Clipboard.GetImage();
            if(source == null)
            {
                MessageBox.Show("Brak obrazu w schowku", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ScreenshotImage.Source = source;
        }

        void SaveToPng(BitmapSource bitmap, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            SaveUsingEncoder(bitmap, fileName, encoder);
        }

   
        void SaveUsingEncoder(BitmapSource bitmap, string fileName, BitmapEncoder encoder)
        {
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = System.IO.File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            var img = Clipboard.GetImage();
            if(img == null)
            {
                MessageBox.Show("Brak obrazu w schowku", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if(FilesDirBox.Text.Length == 0)
            {
                SelectDirButton_Click(null, null);

                if (FilesDirBox.Text.Length == 0) return;
            }

            string dir = System.IO.Path.Combine(FilesDirBox.Text, ImageNumberBox.Text + ".png");
            SaveToPng(Clipboard.GetImage(), dir);

            MessageBox.Show("Zapisano plik " + dir, "Zapisano", MessageBoxButton.OK, MessageBoxImage.Information);

            IncrementNumberButton_Click(null, null);
            ScreenshotImage.Source = null;
        }

        private void SelectDirButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            
            string p = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            folderDialog.SelectedPath = p;
            var result = folderDialog.ShowDialog();

            if(result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            FilesDirBox.Text = folderDialog.SelectedPath;
        }

        void UpdateImagesNumber()
        {
            // Update files number
            var files = System.IO.Directory.GetFiles(FilesDirBox.Text, "*.png");

            List<int> numbers = new List<int>();
            for (int i = 0; i < files.Length; i++)
            {
                if (!files[i].EndsWith(".png")) continue; // Not a file we're looking for


                try
                {
                    int number = 0;
                    string str = files[i].Remove(0, FilesDirBox.Text.Length + 1 /* path + / */);
                    str = str.Substring(0, str.Length - 4); // remove .png
                    if (!int.TryParse(str, out number))
                    {
                        continue; // It's probably not a file we're searching for
                    }

                    // We've found one of them
                    numbers.Add(number);
                }
                catch (ArgumentOutOfRangeException) // Probably string formatting error
                {
                    continue;
                }
            }

            int it = 1;
            while (true)
            {
                bool found = false;
                for (int i = 0; i < numbers.Count; i++)
                {
                    if (numbers[i] == it)
                    {
                        found = true;
                        it++;
                    }
                }

                if (!found) break;
            }


            ImageNumberBox.Text = it.ToString();
        }

        private void ImageNumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextAllowed(e.Text);
        }

        private static readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return _regex.IsMatch(text);
        }

        private void DecrementNumberButton_Click(object sender, RoutedEventArgs e)
        {
            int n = int.Parse(ImageNumberBox.Text) - 1;
            if(n < 1)
            {
                n = 1;
            }

            ImageNumberBox.Text = n.ToString();
        }

        private void IncrementNumberButton_Click(object sender, RoutedEventArgs e)
        {
            ImageNumberBox.Text = (int.Parse(ImageNumberBox.Text) + 1).ToString();
        }

        bool alignImages = true;
        private void SavePDF_Click(object sender, RoutedEventArgs e)
        {
            //Task.Run(SaveToPDF);
            //SaveToPDF();


            string path = FilesDirBox.Text;
            int n = int.Parse(ImageNumberBox.Text);
            bool useJPG = FormatPicker.SelectedItem.ToString().Contains("JPG");

            var fileDialog = new System.Windows.Forms.SaveFileDialog();
            fileDialog.FileName = "wyklad.pdf";
            fileDialog.Filter = "Pdf Files|*.pdf";
            var result = fileDialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            //await Task.Run(() => SaveToPDF(FilesDirBox.Text, int.Parse(ImageNumberBox.Text), FormatPicker.SelectedItem.ToString().Contains("JPG")));
            Thread t = new Thread(() => SaveToPDF(path, fileDialog.FileName, n, useJPG));
            t.Start();
        }

        void SaveToPDF(string imagesPath, string targetPath, int imagesNumber, bool useJPG)
        {
            if (imagesNumber == 1)
            {
                MessageBox.Show("Nie można zapisać pustego dokumentu, dodaj zrzuty ekranu przyciskiem \"Dodaj\"", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }




            SavingProgressWindow progressWindow = null;
            FilesDirBox.Dispatcher.Invoke(() => {
                progressWindow = new SavingProgressWindow();
                progressWindow.Show();
                Application.Current.MainWindow.Visibility = Visibility.Hidden;
            });


            int maxWidth = 0;
            int maxHeight = 0;
            if (alignImages)
            {
                for (int i = 1; i < imagesNumber; i++)
                {
                    string file = System.IO.Path.Combine(imagesPath, i.ToString() + ".png");
                    if(!System.IO.File.Exists(file))
                    {
                        MessageBox.Show("Nie znaleziono pliku " + i.ToString() + ".png", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    System.Drawing.Image img = System.Drawing.Image.FromFile(file);

                    if (img.Height > maxHeight)
                    {
                        maxHeight = img.Height;
                    }

                    if (img.Width > maxWidth)
                    {
                        maxWidth = img.Width;
                    }
                }
            }


            using (System.IO.FileStream fs = new System.IO.FileStream(targetPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
            {
                using (Document doc = new Document())
                {
                    using (PdfWriter writer = PdfWriter.GetInstance(doc, fs))
                    {
                        doc.Open();
                        for (int i = 1; i < imagesNumber; i++)
                        {

                            string file = System.IO.Path.Combine(imagesPath, i.ToString() + ".png");
                            System.Drawing.Image img = System.Drawing.Image.FromFile(file);

                            iTextSharp.text.Image image = null;

                            // If using JPG convert temporarily PNG to JPG
                            string jpgFile = "";
                            if (useJPG)
                            {
                                jpgFile = file.Substring(0, file.Length - 4) /* .png */ + ".jpg";
                                img.Save(jpgFile, System.Drawing.Imaging.ImageFormat.Jpeg);

                                image = iTextSharp.text.Image.GetInstance(jpgFile);
                            }
                            else
                            {
                                image = iTextSharp.text.Image.GetInstance(file);
                            }

                             

                            if (alignImages)
                            {
                                image.SetAbsolutePosition((maxWidth - img.Width) / 2, (maxHeight - img.Height) / 2);

                                doc.SetPageSize(new iTextSharp.text.Rectangle(0, 0, maxWidth, maxHeight, 0));
                            }
                            else
                            {
                                image.SetAbsolutePosition(0, 0);
                                doc.SetPageSize(new iTextSharp.text.Rectangle(0, 0, img.Width, img.Height, 0));
                            }
                            doc.NewPage();

                            writer.DirectContent.AddImage(image, false);

                            // Delete temporary JPG file if we're using this type
                            if(useJPG)
                            {
                                System.IO.File.Delete(jpgFile);
                            }

                            progressWindow.Dispatcher.Invoke(() => progressWindow.SetValue(i * 100f / imagesNumber));
                        }

                        doc.Close();
                    }
                }
            }

            progressWindow.Dispatcher.Invoke(() => {
                progressWindow.Close();
                MessageBox.Show("Zapisano plik " + targetPath, "Zapisano", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.MainWindow.Visibility = Visibility.Visible;
            });

            

        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AlignImagesCheckBoxButton_Click(object sender, RoutedEventArgs e)
        {
            alignImages = !alignImages;

            if(alignImages)
            {   
                AlignImagesCheckBoxButtonImage.Opacity = 1;
            }
            else
            {
                AlignImagesCheckBoxButtonImage.Opacity = 0;
            }
        }

        private void FilesDirBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Directory changed, so we have to update images number, but just check if this directory event exists
            if(!System.IO.Directory.Exists(FilesDirBox.Text))
            {
                return;
            }

            UpdateImagesNumber();
        }
    }
}
