using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
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

        List<Point> bmpPixelPoint = new List<Point>();

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
                    //img.Save("image.bmp", ImageFormat.Bmp);
                    Bitmap bmp = new Bitmap(img);
                    // Hatch를 위한 배열
                    Hatch = new int[bmp.Width, bmp.Height];
                    bool flag = false;
                    bool EndColumn = false;
                    toolPathHatchingLine tpp;
                    tpp.Start = new Point(0, 0);
                    tpp.End = new Point(0, 0);
                    Point Current = new Point(0, 0);
                    int hatchInterval = 50;
                    int tempY = 0;
                    int count = 0;
                    int colCount = 0;
                    int rowCount = 0;
                    // toolPathColumnManager
                    List<toolPathHatchingLine> toolPathColumnManager = new List<toolPathHatchingLine>();

                    for (int h = 0; h < bmp.Height; h++)
                    {
                        for (int w = 0; w < bmp.Width; w++)
                        {
                            color = bmp.GetPixel(w, h);
                            rgb = (color.R + color.G + color.B) / 3;
                            if (rgb > ((128 + 128 + 128) / 3))
                            {
                                bmp.SetPixel(w, h, System.Drawing.Color.White);
                                // 흰 영역을 만났을때
                                // 직전 영역(Current)을 tpp.End에 저장
                                // 컬럼 추가
                                if (flag)
                                {
                                    tpp.End = Current;
                                    flag = false;
                                    EndColumn = true;
                                    toolPathColumnManager.Add(tpp);
                                    colCount++;
                                    //Console.WriteLine(tpp);
                                }
                                else if ((EndColumn) && (w == (bmp.Width - 1)))
                                {
                                    tpp.End = Current;
                                    flag = false;
                                    EndColumn = false;
                                    toolPathManager.Add(new List<toolPathHatchingLine>());
                                    int index = 0;
                                    while (toolPathColumnManager.Count != 0)
                                    {
                                        toolPathManager[rowCount].Add(toolPathColumnManager[index]);
                                        toolPathColumnManager.RemoveAt(0);
                                        if (toolPathColumnManager.Count != 0)
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            rowCount++;
                                        }
                                    }
                                    //toolPathColumnManager.Clear();
                                    foreach (var item in toolPathColumnManager)
                                    {
                                        Console.WriteLine(w.ToString() + "," + h.ToString());
                                        Console.WriteLine($"{item}");
                                    }
                                    //toolPathColumnManager.RemoveRange(0,toolPathColumnManager.Count);
                                    tempY += hatchInterval;
                                }
                                // 컬럼 끝
                            }
                            else
                            {
                                bmp.SetPixel(w, h, System.Drawing.Color.Black);
                                // 검은색 영역을 만났을 때
                                if ((!flag) && (count == 0))
                                {
                                    tpp.Start = new Point(w, h);
                                    flag = true;
                                    count++;
                                    tempY = h;
                                }
                                else if ((!flag) && (count != 0))
                                {
                                    if (h == tempY)
                                    {
                                        // 해칭라인 Row 0, Column 0
                                        tpp.Start = new Point(w, h);
                                        flag = true;
                                        //tempY += hatchInterval;
                                    }
                                }
                                else
                                {
                                    Current = new Point(w, h);
                                    continue;
                                }
                            }
                        }
                    } // bmp 이미지 이진화 완료
                      //HatchingManager.Add(new List<List<List<toolPathHatchingLine>>> { toolPathManager });
                      //int row, col = 0;
                      // 리스트의 데이터 확인
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
                    preImage.Source = GetBitmapSourceFromBitmap(bmp);
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
            Point Start, Current, NextStart, NextEnd, moveToolPoint;
            List<toolPathHatchingLine> toolPathRowManager = new List<toolPathHatchingLine>();
            toolPathHatchingLine tpp;
            int row = 0;
            int col = 0;
            int UpZ = -80;
            int DownZ = 100;
            // 왼쪽에서 오른쪽
            // 오른쪽에서 왼쪽
            // 제어하는 flag
            bool LeftToRight;

            // 공구를 내렸을때
            // 해칭 그리기가
            // 끝났는지
            bool isDrawFinished;

            #region 첫 데이터 그리기
            // 첫 데이터
            // row 0, col 0
            Start = toolPathManager[row][col].Start;
            Current = toolPathManager[row][col].End;
            tpp.Start = Start;
            tpp.End = Current;
            toolPathRowManager.Add(tpp);

            // 두번째 데이터
            // 맨 왼쪽
            // row 1, col 0
            NextStart = toolPathManager[row][col].Start;
            NextEnd = toolPathManager[row][col].End;

            string ToolUpStart = makeToolpath(Start, UpZ);
            string ToolUpEnd = makeToolpath(Current, UpZ);
            string StartP = makeToolpath(Start, DownZ);
            string EndP = makeToolpath(Current, DownZ);
            // 첫 좌표 공구 이동
            Order.Add("VS30;");
            Order.Add(ToolUpStart);
            Order.Add(ToolUpStart);
            Order.Add(ToolUpStart);
            // 첫 좌표 공구 내리기
            Order.Add("VS24;");
            Order.Add(StartP);

            // 첫 공구 이동
            Order.Add("VS36");
            Order.Add(StartP);
            Order.Add(EndP);
            // 첫라인
            // Left -> Right 수행
            // flag -> false
            LeftToRight = false;
            #endregion

            // 데이터를 소진하면서
            // 툴패스를 그림
            while (toolPathManager.Count != 0)
            {
                int toolPathManagerCount = toolPathManager.Count;
                // Y값 만큼 공구 이동
                // 리스트에 row가
                // 하나만 남았을 때
                if (toolPathManagerCount == 1)
                {
                    row = 0;
                    Start = toolPathManager[row][col].Start;
                    Current = toolPathManager[row][col].End;
                    StartP = makeToolpath(Start, DownZ);
                    EndP = makeToolpath(Current, DownZ);
                    ToolUpStart = makeToolpath(Start, UpZ);
                    ToolUpEnd = makeToolpath(Current, UpZ);
                    //오른쪽 -> 왼쪽
                    if (!LeftToRight)
                    {
                        Order.Add(StartP);
                        Order.Add(EndP);
                        LeftToRight = true;
                        Order.Add(ToolUpEnd);
                        Order.Add(ToolUpEnd);
                        Order.Add(ToolUpEnd);
                    }
                    else
                    {
                        Order.Add(EndP);
                        Order.Add(StartP);
                        LeftToRight = false;
                        Order.Add(ToolUpStart);
                        Order.Add(ToolUpStart);
                        Order.Add(ToolUpStart);
                    }
                }
                //해칭 그리기 시작
                else
                {
                    //다음 데이터를 가져오기 위한 row
                    row = 1;
                    // col이 여러개 일때
                    if (toolPathManager[row].Count > 1)
                    {
                        int count = toolPathManager[row].Count;
                        for (int i = 0; i < count; i++)
                        {
                           NextStart = toolPathManager[row][i].Start;
                           NextEnd = toolPathManager[row][i].End;
                            // 다음 라인의 X값이
                            // 현재 끝점의 X값 
                            // 사이에 있을때
                            bool chkmeetline = checkMeetLine(Start, Current, NextStart, NextEnd);
                            if (NextStart.X <= Current.X && Current.X <= NextEnd.X)
                            {
                                //공구를 내리고
                                //NextEnd로 이동을 한다
                                //오른쪽 -> 왼쪽
                                if (!LeftToRight)
                                {
                                    moveToolPoint = new Point(Current.X, NextEnd.Y);
                                    string ToolMoveVertical = makeToolpath(moveToolPoint, DownZ);
                                    Order.Add(ToolMoveVertical);
                                    Start = toolPathManager[row][col].Start;
                                    Current = toolPathManager[row][col].End;
                                    StartP = makeToolpath(Start, DownZ);
                                    EndP = makeToolpath(Current, DownZ);
                                    Order.Add(EndP);
                                    Order.Add(StartP);
                                    LeftToRight = true;
                                }
                                //왼쪽 -> 오른쪽
                                else if (LeftToRight)
                                {
                                    moveToolPoint = new Point(Start.X, NextStart.Y);
                                    string ToolMoveVertical = makeToolpath(moveToolPoint, DownZ);
                                    Order.Add(ToolMoveVertical);
                                    Start = toolPathManager[row][col].Start;
                                    Current = toolPathManager[row][col].End;
                                    StartP = makeToolpath(Start, DownZ);
                                    EndP = makeToolpath(Current, DownZ);
                                    Order.Add(StartP);
                                    Order.Add(EndP);
                                    LeftToRight = false;
                                }
                                //왼쪽 -> 오른쪽
                                else
                                {
                                    Order.Add(StartP);
                                    Order.Add(EndP);
                                    LeftToRight = false;
                                }
                            }
                            // 다음 라인의 X값이
                            // 현재 끝점의 X값
                            // 사이에 없을때
                            // 공구를 내려서 그릴 수 있는지
                            // 교점을 구해본다
                            else if (chkmeetline)
                            {
                                // 교점을 구해보고
                                // 가능하면
                                // 공구를
                                // 다음 라인의
                                // 끝의 X로 보내고
                                // 라인을 그린다
                                if (!LeftToRight)
                                {
                                    //공구를 움직인다
                                    //Current.X, Current.Y
                                    //-> NextEnd.X, Current.Y
                                    moveToolPoint = new Point(NextEnd.X, Current.Y);
                                    string moveTool = makeToolpath(moveToolPoint, DownZ);
                                    Order.Add(moveTool);
                                    //공구를 내린다
                                    //NextEnd.X, Current.Y
                                    //-> NextEnd.X, NextEnd.Y
                                    moveToolPoint = new Point(NextEnd.X, NextEnd.Y);
                                    moveTool = makeToolpath(moveToolPoint, DownZ);
                                    Order.Add(moveTool);
                                    //R -> L
                                    Start = toolPathManager[row][col].Start;
                                    Current = toolPathManager[row][col].End;
                                    StartP = makeToolpath(Start, DownZ);
                                    EndP = makeToolpath(Current, DownZ);
                                    Order.Add(EndP);
                                    Order.Add(StartP);
                                    LeftToRight = true;
                                }
                                //왼쪽 -> 오른쪽
                                else if (LeftToRight)
                                {
                                    //공구를 움직인다
                                    //Start.X, Start.Y
                                    //-> NextStart.X, Start.Y
                                    moveToolPoint = new Point(NextStart.X, Start.Y);
                                    string moveTool = makeToolpath(moveToolPoint, DownZ);
                                    Order.Add(moveTool);
                                    //공구를 내린다
                                    //NextSTart.X, Start.Y
                                    //-> NextStart.X, NextStart.Y
                                    moveToolPoint = new Point(NextStart.X, NextStart.Y);
                                    moveTool = makeToolpath(moveToolPoint, DownZ);
                                    Order.Add(moveTool);
                                    // L -> R
                                    Start = toolPathManager[row][col].Start;
                                    Current = toolPathManager[row][col].End;
                                    StartP = makeToolpath(Start, DownZ);
                                    EndP = makeToolpath(Current, DownZ);
                                    Order.Add(StartP);
                                    Order.Add(EndP);
                                    LeftToRight = false;
                                }
                                /* if문이
                                 * LeftToRight로
                                 * 제어가 되고 있으므로
                                 * else 안탐
                                // 공구를 든다
                                else
                                {
                                    Order.Add(StartP);
                                    Order.Add(EndP);
                                    LeftToRight = false;
                                }
                                */
                            }
                            // 현재 공구의 위치에서는
                            // 다음 라인을 그릴수 없으므로
                            // 공구를 든다
                            // 공구를 들때는
                            // 1. Y 간격 값이 크거나
                            // > 데이터 수집시 해결
                            // 2. 더이상 그릴 수 없거나
                            // 3. 더이상 그릴 게 없거나
                            else 
                            {
                                if (!LeftToRight) 
                                {
                                    isDrawFinished = true;
                                    moveToolPoint = Start;
                                }
                                else 
                                {
                                    isDrawFinished = true;
                                    moveToolPoint = Current;
                                }
                            }
                        }
                        foreach (Point item in toolPathRowManager)
                        {
                            Console.WriteLine(toolPathRowManager.Count);
                        }
                    }
                    NextStart = toolPathManager[row][col].Start;
                    NextEnd = toolPathManager[row][col].End;
                    //오른쪽 -> 왼쪽
                    if (!LeftToRight)
                    {
                        moveToolPoint = new Point(Current.X, NextEnd.Y);
                        string ToolMoveVertical = makeToolpath(moveToolPoint, DownZ);
                        Order.Add(ToolMoveVertical);
                        Start = toolPathManager[row][col].Start;
                        Current = toolPathManager[row][col].End;
                        StartP = makeToolpath(Start, DownZ);
                        EndP = makeToolpath(Current, DownZ);
                        Order.Add(EndP);
                        Order.Add(StartP);
                        LeftToRight = true;
                    }
                    //왼쪽 -> 오른쪽
                    else if (LeftToRight)
                    {
                        moveToolPoint = new Point(Start.X, NextStart.Y);
                        string ToolMoveVertical = makeToolpath(moveToolPoint, DownZ);
                        Order.Add(ToolMoveVertical);
                        Start = toolPathManager[row][col].Start;
                        Current = toolPathManager[row][col].End;
                        StartP = makeToolpath(Start, DownZ);
                        EndP = makeToolpath(Current, DownZ);
                        Order.Add(StartP);
                        Order.Add(EndP);
                        LeftToRight = false;
                    }
                    //왼쪽 -> 오른쪽
                    else
                    {
                        Order.Add(StartP);
                        Order.Add(EndP);
                        LeftToRight = false;
                    }
                }
                // 전의 데이터 삭제
                if (row == 0)
                {
                    toolPathManager.RemoveAt(row);
                }
                else
                {
                    toolPathManager.RemoveRange(0, row);
                }
            }

            Order.Add("!VO;");

            for (int i = 0; i < Order.Count; i++)
            {
                sp.WriteLine(Order[i]);
            }
            //string order = "IN;!ZC320;\r\n!CL1;\r\n!PM0,0;\r\n!ZC200;!MH-189,-164,377,349,0,2,1,1,1;\r\n!MH-189,-164,377,349,0,1,0,1,1;\r\n!SR0;\r\nVS36;\r\n!ZZ-55,-165,-200;\r\n!ZZ-55,-165,-200;\r\n!ZZ-55,-165,-200;\r\nVS24;\r\n!ZZ-55,-165,100;\r\n!ZZ-8,-165,100;\r\n!ZZ-21,-165,100;\r\n!ZZ-21,-115,100;\r\n!ZZ-74,-115,100;\r\n!ZZ-74,-65,100;\r\n!ZZ-93,-65,100;\r\n!ZZ-39,-65,100;\r\n!ZZ-58,-65,100;\r\n!ZZ-58,-15,100;\r\n!ZZ-112,-15,100;\r\n!ZZ-112,35,100;\r\n!ZZ-132,35,100;\r\n!ZZ-76,35,100;\r\n!ZZ-94,35,100;\r\n!ZZ-94,85,100;\r\n!ZZ-151,85,100;\r\n!ZZ-151,135,100;\r\n!ZZ-170,135,100;\r\n!ZZ-112,135,100;\r\n!ZZ-130,135,100;\r\n!ZZ-130,185,100;\r\n!ZZ-189,185,100;\r\nVS36;\r\n!ZZ-189,185,-200;\r\n!ZZ87,85,-200;\r\nVS24;\r\n!ZZ87,85,100;\r\n!ZZ148,85,100;\r\n!ZZ148,135,100;\r\n!ZZ168,135,100;\r\n!ZZ106,135,100;\r\n!ZZ125,135,100;\r\n!ZZ125,185,100;\r\n!ZZ189,185,100;\r\nVS36;\r\n!ZZ189,185,-200;\r\n!ZZ45,-165,-200;\r\nVS24;\r\n!ZZ45,-165,100;\r\n!ZZ-5,-165,100;\r\n!ZZ11,-165,100;\r\n!ZZ11,-115,100;\r\n!ZZ66,-115,100;\r\n!ZZ66,-65,100;\r\n!ZZ86,-65,100;\r\n!ZZ30,-65,100;\r\n!ZZ49,-65,100;\r\n!ZZ49,-15,100;\r\n!ZZ107,-15,100;\r\n!ZZ107,35,100;\r\n!ZZ127,35,100;\r\n!ZZ68,35,100;\r\nVS36;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!ZZ68,35,-200;\r\n!VO;\r\n";
            //sp.WriteLine(order);
        }

        private void initToolpath()
        {
            Order.Add("IN;");
            Order.Add("!CL1;");
            Order.Add("!PM0,0;");
            Order.Add("!MH153,187,303,252,0,2,1,1,1;");
            Order.Add("!MH153,187,303,252,0,1,0,1,1;");
            Order.Add("!SR0;");
        }

        private string makeToolpath(Point point, int z)
        {
            Point _point = point;
            string x = point.X.ToString() + ",";
            string y = point.Y.ToString() + ",";
            string _z = z.ToString();
            string toolpath = "!ZZ" + x + y + z;
            return toolpath;
        }

        //두점 사이의 개수
        private bool checkMeetLine(Point start, Point end, Point nextstart, Point nextend) 
        {
            //x1,y1
            Point AP1= start;
            //x2,y2
            Point AP2 = end;
            //x3,y3
            Point BP1 = nextstart;
            //x4,y4
            Point BP2 = nextend;
            double t;
            double s;
            // 분모
            double under = (BP2.Y - BP1.Y) * (AP2.X - AP1.X) - (BP2.X - BP1.X) * (AP2.Y - AP1.Y);
            if (under == 0)
                return false;

            double _t = (BP2.X - BP1.X) * (AP1.Y - BP1.Y) - (BP2.Y - BP1.Y) * (AP1.X - BP1.X);
            double _s = (AP2.X - AP1.X) * (AP1.Y - BP1.Y) - (AP2.Y - AP1.Y) * (AP1.X - BP1.X);

            t = _t / under;
            s = _s / under;

            if (t < 0.0 || t > 1.0 || s < 0.0 || s > 1.0)
                return false;
            if (_t == 0 && _s == 0) 
                return false;
            /*
            //교점의 위치인듯?
            IP->x = AP1.x + t * (double)(AP2.x - AP1.x);
            IP->y = AP1.y + t * (double)(AP2.y - AP1.y);
            */
            return true;
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
