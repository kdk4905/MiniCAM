using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Point = System.Drawing.Point;

namespace MiniCAM
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        Queue Data = new Queue(); //데이터 저장 큐
        SerialPort sp = new SerialPort();
        // 리스트
        // HTMP
        List<string> Order = new List<string>();
        // toolPathData
        List<List<Point>> toolPathData = new List<List<Point>>();
        // toolPathRowManager
        List<Point> toolPathRowManager = new List<Point>();
        // toolPathColumnManager
        List<Point> toolPathColumnManager = new List<Point>();
        // toolPathManager
        List<List<toolPathHatchingLine>> toolPathManager = new List<List<toolPathHatchingLine>>();
        // HatchingManager
        List<List<List<toolPathHatchingLine>>> HatchingManager = new List<List<List<toolPathHatchingLine>>>(); 
        string T_msg = ""; //WPF Text 저장용
        // bool
        bool IsSpOpen;
        // CNC 이미지
        System.Windows.Controls.Image myImage;
        // Bitmap 이미지
        // 구조체
        // Point
        struct toolPathHatchingLine
        {
            public Point Start;
            public Point End;
        }

        // Color
        System.Drawing.Color color;
        // rgb 변수
        int rgb;
        // bmp의 좌표
        int[,] Hatch;

        List<Point>bmpPixelPoint = new List<Point>();

        public MainWindow()
        {
            InitializeComponent();
            cbx_Port.ItemsSource = SerialPort.GetPortNames();
            initToolpath();
            
            Thread t = new Thread(text);
            t.IsBackground = true;
            t.Start();
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
                sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);

                sp.Open(); //시리얼포트 열기
                sp.WriteLine("!BP1;");

                lblConnectState.Content += " 포트가 연결되었습니다.";
            }
            else
            {
                lblConnectState.Content = "연결상태 :";
                lblConnectState.Content += " 이미 포트와 연결되었습니다.";
            }
        }

        //시리얼 포트 데이터 리시브 이벤트
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string getData = sp.ReadExisting();
            Data.Enqueue(getData);
            Thread.Sleep(1);
        }

        private void MySerialReceived(object sender, EventArgs e)
        {
        }

        private void text()
        {
            while (true)
            {
                if (Data.Count > 0)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        string msg = DateTime.Now.ToString("HH:mm:ss.fff");
                        var ReceiveData = Data.Dequeue();

                        msg = string.Format("{0} : {1}\r\n", msg, ReceiveData);
                        T_msg = string.Format("{0} : {1}", T_msg, msg);
                        //T_text_list.Text = T_msg;
                        //T_text_list.SelectionStart = T_text_list.Text.Length;
                        //T_text_list.ScrollToEnd();
                    }));
                }
                Thread.Sleep(10);
            }
        }

        private void btnImageOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            if (openDialog.ShowDialog() == true)
            {
                if (File.Exists(openDialog.FileName))
                {
                    // 비트맵 이미지
                    System.Drawing.Image img = System.Drawing.Image.FromFile(openDialog.FileName);
                    img.Save("image.bmp", ImageFormat.Bmp);
                    Bitmap bmp = new Bitmap(img);
                    // Hatch를 위한 배열
                    Hatch = new int[bmp.Width, bmp.Height];
                    bool flag = false;
                    toolPathHatchingLine tpp;
                    tpp.Start = new Point(0,0);
                    tpp.End = new Point(0, 0);
                    Point CurrentPoint = new Point(0,0);
                    bool makeHatchLine;
                    for (int h = 0; h < bmp.Height; h++)
                    {
                        for (int w = 0; w < bmp.Width; w++)
                        {
                            color = bmp.GetPixel(w, h);
                            rgb = (color.R + color.G + color.B) / 3;
                            //Point bmpPoint = new Point(w, h);
                            //bmpPixelPoint.Add(bmpPoint);
                            //gray rgb(128,128,128)
                            if (rgb > ((128+128+128) / 3))
                            {
                                bmp.SetPixel(w, h, System.Drawing.Color.White);
                                // 흰 영역을 만났을때
                                // 직전 영역(Current)을 tpp.End에 저장
                                if (flag)
                                {
                                        tpp.End = CurrentPoint;
                                        flag = false;
                                        toolPathManager.Add(new List<toolPathHatchingLine> { tpp });
                                }
                            }
                            else
                            {
                                bmp.SetPixel(w, h, System.Drawing.Color.Black);
                                Hatch[w,h] = 1;
                                int z = 100;
                                string aa = makeToolpath(w.ToString(), h.ToString(), z.ToString());
                                //Console.WriteLine(aa);
                                //Debug.WriteLine(aa);
                                //Order.Add(aa);
                                // 검은색 영역을 만났을 때
                                if (!flag)
                                {
                                    // 해칭라인 Row 0, Column 0
                                        tpp.Start = new Point(w, h);
                                        flag = true;
                                }
                                else
                                {
                                    CurrentPoint.X = w;
                                    CurrentPoint.Y = h;
                                    continue;
                                }
                            }
                        }
                    } // bmp 이미지 이진화 완료
                    //HatchingManager.Add(new List<List<List<toolPathHatchingLine>>> { toolPathManager });
                    //int row, col = 0;
                    foreach (List<toolPathHatchingLine> item in toolPathManager)
                    {
                        Console.WriteLine(item.Count);
                    }

                    //CNC 할 이미지
                    myImage = new System.Windows.Controls.Image();
                    //myImage.Source = bitmapSource;
                    myImage.Width = 500;
                    myImage.Height = 500;
                    //화면에 보여줄 이미지

                    System.Windows.Controls.Image preImage = new System.Windows.Controls.Image();
                    //preImage.Source = bitmapSource;
                    preImage.Width = 200;
                    preImage.Height = 200;

                    stackpnlImage.Children.Add(preImage);
                    #region 변환된 비트맵 이미지 확인
                    //변환된 비트맵 이미지
                    //비트맵 to 비트맵소스
                    //stackpnlImage.Children.Remove(preImage);
                    //preImage.Source = GetBitmapSourceFromBitmap(bmp);
                    //stackpnlImage.Children.Add(preImage);
                    #endregion
                }
            }
        }

        private void btnOperationStart_Click(object sender, RoutedEventArgs e)
        {
            //string order = "VS36;\r\n!ZZ-55,-165,-200;\r\n!ZZ-55,-165,-200;\r\n!ZZ-55,-165,-200;\r\nVS24;\r\n!ZZ-55,-165,100;\r\n!ZZ-8,-165,100;\r\n!ZZ-21,-165,100;\r\n!ZZ-21,-115,100;\r\n!ZZ-74,-115,100;\r\n!ZZ-74,-65,100;\r\n!ZZ-93,-65,100;\r\n!ZZ-39,-65,100;\r\n!ZZ-58,-65,100;\r\n!ZZ-58,-15,100;\r\n!ZZ-112,-15,100;\r\n!ZZ-112,35,100;\r\n!ZZ-132,35,100;\r\n!ZZ-76,35,100;\r\n!ZZ-94,35,100;\r\n!ZZ-94,85,100;\r\n!ZZ-151,85,100;\r\n!ZZ-151,135,100;\r\n!ZZ-170,135,100;\r\n!ZZ-112,135,100;\r\n!ZZ-130,135,100;\r\n!ZZ-130,185,100;\r\n!ZZ-189,185,100;\r\nVS36;\r\n!ZZ-189,185,-200;\r\n!ZZ87,85,-200;\r\nVS24;\r\n!ZZ87,85,100;\r\n!ZZ148,85,100;\r\n!ZZ148,135,100;\r\n!ZZ168,135,100;\r\n!ZZ106,135,100;\r\n!ZZ125,135,100;\r\n!ZZ125,185,100;\r\n!ZZ189,185,100;\r\nVS36;\r\n!ZZ189,185,-200;\r\n!ZZ45,-165,-200;\r\nVS24;\r\n!ZZ45,-165,100;\r\n!ZZ-5,-165,100;\r\n!ZZ11,-165,100;\r\n!ZZ11,-115,100;\r\n!ZZ66,-115,100;\r\n!ZZ66,-65,100;\r\n!ZZ86,-65,100;\r\n!ZZ30,-65,100;\r\n!ZZ49,-65,100;\r\n!ZZ49,-15,100;\r\n!ZZ107,-15,100;\r\n!ZZ107,35,100;\r\n!ZZ127,35,100;\r\n!ZZ68,35,100;\r\nVS36;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!VO;";
            //Order.Add(order);
            for (int i = 0; i < Order.Count; i++)
            {
                sp.WriteLine(Order[i]);
            }
            sp.WriteLine("!VO;");
            //string order = "IN;!ZC320;\r\n!CL1;\r\n!PM0,0;\r\n!ZC200;!MH-189,-164,377,349,0,2,1,1,1;\r\n!MH-189,-164,377,349,0,1,0,1,1;\r\n!SR0;\r\nVS36;\r\n!ZZ-55,-165,-200;\r\n!ZZ-55,-165,-200;\r\n!ZZ-55,-165,-200;\r\nVS24;\r\n!ZZ-55,-165,100;\r\n!ZZ-8,-165,100;\r\n!ZZ-21,-165,100;\r\n!ZZ-21,-115,100;\r\n!ZZ-74,-115,100;\r\n!ZZ-74,-65,100;\r\n!ZZ-93,-65,100;\r\n!ZZ-39,-65,100;\r\n!ZZ-58,-65,100;\r\n!ZZ-58,-15,100;\r\n!ZZ-112,-15,100;\r\n!ZZ-112,35,100;\r\n!ZZ-132,35,100;\r\n!ZZ-76,35,100;\r\n!ZZ-94,35,100;\r\n!ZZ-94,85,100;\r\n!ZZ-151,85,100;\r\n!ZZ-151,135,100;\r\n!ZZ-170,135,100;\r\n!ZZ-112,135,100;\r\n!ZZ-130,135,100;\r\n!ZZ-130,185,100;\r\n!ZZ-189,185,100;\r\nVS36;\r\n!ZZ-189,185,-200;\r\n!ZZ87,85,-200;\r\nVS24;\r\n!ZZ87,85,100;\r\n!ZZ148,85,100;\r\n!ZZ148,135,100;\r\n!ZZ168,135,100;\r\n!ZZ106,135,100;\r\n!ZZ125,135,100;\r\n!ZZ125,185,100;\r\n!ZZ189,185,100;\r\nVS36;\r\n!ZZ189,185,-200;\r\n!ZZ45,-165,-200;\r\nVS24;\r\n!ZZ45,-165,100;\r\n!ZZ-5,-165,100;\r\n!ZZ11,-165,100;\r\n!ZZ11,-115,100;\r\n!ZZ66,-115,100;\r\n!ZZ66,-65,100;\r\n!ZZ86,-65,100;\r\n!ZZ30,-65,100;\r\n!ZZ49,-65,100;\r\n!ZZ49,-15,100;\r\n!ZZ107,-15,100;\r\n!ZZ107,35,100;\r\n!ZZ127,35,100;\r\n!ZZ68,35,100;\r\nVS36;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!VO;\r\n";
            //sp.WriteLine(order);
        }

        private void initToolpath() 
        {
            Order.Add("IN;!ZC320;");
            Order.Add("!CL1;");
            Order.Add("!PM0,0;");
            Order.Add("!ZC200;!MH-189,-164,500,500,0,2,1,1,1;");
            Order.Add("!MH-189,-164,500,500,0,1,0,1,1;");
            Order.Add("!SR0;");
            Order.Add("!VS36;");
            //Order.Add("!MH-189,-164,377,349,0,1,0,1,1;");//소재 위치 체크
        }

        private string makeToolpath(string w, string h , string z) 
        {
            string _w = w + ",";
            string _h = h + ",";
            string _z = z;
            string toolpath = "!ZZ" + _w + _h + z;
            return toolpath;
        }
        //이미지 소스 to 비트맵 
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
        //비트맵 to 이미지 소스
        private BitmapSource GetBitmapSourceFromBitmap(System.Drawing.Bitmap bitmap)
        {
            BitmapSource bitmapSource;


            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSizeOptions sizeOptions = BitmapSizeOptions.FromEmptyOptions();
            bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, sizeOptions);
            bitmapSource.Freeze();


            return bitmapSource;
        }
        //bmp to byteArr
        byte[] ConvertBitmapToByteArray(Bitmap bitmap)
        {
            byte[] result = null;
            if (bitmap != null)
            {
                MemoryStream stream = new MemoryStream();
                bitmap.Save(stream, bitmap.RawFormat);
                result = stream.ToArray();
            }
            else
            {
                Console.WriteLine("Bitmap is null.");
            }
            return result;
        }
    }
}
