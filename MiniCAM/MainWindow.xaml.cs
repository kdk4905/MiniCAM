using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
using static System.Net.Mime.MediaTypeNames;
using Color = System.Drawing.Color;
using Image = System.Windows.Controls.Image;

namespace MiniCAM
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort sp = new SerialPort();
        // CNC 이미지
        Image myImage;
        // Bitmap 이미지
        Bitmap bitmap;
        // Color 객체
        // 비트맵 흰색, 검은색 구분
        Color color;

        public MainWindow()
        {
            InitializeComponent();
            cbx_Port.ItemsSource = SerialPort.GetPortNames();
//          lblMachineInfo.Content = SerialPort.GetPortNames();
        }

        private void btnMachineConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!sp.IsOpen)
            {
                sp.PortName = cbx_Port.Text;
                sp.BaudRate = 115200;
                sp.DataBits = 8;
                sp.Parity = Parity.None;
                sp.StopBits = StopBits.One;
                sp.Open();

                lblConnectState.Content += " 포트가 연결되었습니다.";
            }
            else
            {
                lblConnectState.Content = "연결상태 :";
                lblConnectState.Content += " 이미 포트와 연결되었습니다.";
            }
        }

        private void btnImageOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();

            if (openDialog.ShowDialog() == true)
            {
                if (File.Exists(openDialog.FileName))
                {
                    
                    bitmap = new Bitmap(openDialog.FileName);
                    bitmap.SetResolution(200, 200);
                    //Stream imageStreamSource = new FileStream(openDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    //PngBitmapDecoder decoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    BitmapSource bitmapSource = BmpConvert(bitmap);
                    //int stride = (int)bitmap.Width;
                    //int size = (int)bitmap.Height;
                    //byte[] pixels = new byte[size];
                    //bitmapSource.CopyPixels(pixels, stride, 0);

                    //CNC 할 이미지
                    myImage = new Image();
                    myImage.Source = bitmapSource;
                    myImage.Width = 500;
                    myImage.Height = 500;

                    //CNC 이미지 비트맵 변환
                    bitmap = new Bitmap(GetBitmapFromBitmapSource(bitmapSource));
                    //bitmap = new Bitmap(myImage.Source);
                    Bitmap btmFile = new Bitmap(bitmap, 500, 500);
                    //btmFile.SetResolution(200,200);
                    //bitmap.Width = 500;
                    //bitmap.Height = 500;
                    //.SetResolution() 해상도
                    //bitmap.SetResolution(10, 10);
                    //bitmap.SetResolution((float)myImage.Width, (float)myImage.Height);

                    //비트맵 GetPixel(x,y)
                    //color = new Color();
                    List<Color> colors = new List<Color>();

                    for (int y = 0; y < btmFile.Height; y++)
                    {
                        for (int x = 0; x < btmFile.Width; x++)
                        {
                            color = btmFile.GetPixel(x, y);
                            if ((color.R == 0 && color.G == 0 && color.B == 0)||(color.R != 255 && color.G !=255 && color.B != 255))
                            {
                                color = Color.FromArgb(255, 0, 0);
                                btmFile.SetPixel(x, y, color);
                            }
                        }
                    }
                    //화면에 보여줄 이미지
                    Image preImage = new Image();
                    preImage.Source = bitmapSource;
                    preImage.Width = 200;
                    preImage.Height = 200;

                    myImage.Tag = System.IO.Path.GetFullPath(openDialog.FileName);
                    
                    stackpnlImage.Children.Add(preImage);
                    // 25000 출력 완료
                    MessageBox.Show(colors.Count.ToString());
                    for (int i = 0; i < colors.Count; i++)
                    {
                        MessageBox.Show(colors[i].ToString());
                        break;
                    }
                    stackpnlImage.Children.Remove(preImage);
                    preImage.Source = BmpConvert(bitmap);
                    stackpnlImage.Children.Add(preImage);
                }
            }
        }
        public static BitmapSource BmpConvert(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        private System.Drawing.Bitmap GetBitmapFromBitmapSource(BitmapSource bitmapSource)
        {
            System.Drawing.Bitmap bitmap;


            using (MemoryStream memoryStream = new MemoryStream())
            {
                BitmapEncoder bitmapEncoder = new BmpBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                bitmapEncoder.Save(memoryStream);


                bitmap = new System.Drawing.Bitmap(memoryStream);
            }


            return bitmap;
        }
        private System.Drawing.Bitmap readfromFile(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            byte[] bytes = new byte[fs.Length];
            fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
            MemoryStream ms = new MemoryStream(bytes);
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(ms);
            return bmp;
        }
    }
}
