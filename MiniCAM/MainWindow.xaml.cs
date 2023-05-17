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
        #region 필드
        Queue Data = new Queue(); //데이터 저장 큐
        SerialPort sp = new SerialPort();
        // 리스트
        // HTMP
        List<string> order = new List<string>();
        // toolPathData
        List<List<Point>> toolPathData = new List<List<Point>>();
        // toolPathRowManager
        List<Point> toolPathRowManager = new List<Point>();
        // toolPathManager
        List<List<toolPathHatchingLine>> toolPathManager = new List<List<toolPathHatchingLine>>();
        // HatchingManager
        List<List<List<toolPathHatchingLine>>> HatchingManager = new List<List<List<toolPathHatchingLine>>>();
        string T_msg = ""; //WPF Text 저장용
        string toolMoveSpeed = "VS36;";
        int downZ = 100;
        int upZ = -80;
        int hatchInterval = 5;
        int imgHeight, imgWidth;
        // bool
        bool IsSpOpen;
        // CNC 이미지
        System.Windows.Controls.Image myImage;

        string _currentName = "";
        Bitmap _currentBmp = null;
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
        List<Point> bmpPixelPoint = new List<Point>();
        #endregion
        #region 메인 윈도우
        public MainWindow()
        {
            InitializeComponent();

            string[] _currentCOM = SerialPort.GetPortNames();

            cbx_Port.ItemsSource = _currentCOM;

            if(_currentCOM.Length == 1)
            {
                cbx_Port.SelectedIndex = 0;
                DeviceConnect();
            }

           
            //initToolpath();
        }

     

        #endregion
        #region 이벤트
        //기기 연결 버튼 클릭시
        private void btnMachineConnect_Click(object sender, RoutedEventArgs e)
        {
            DeviceConnect();
        }

        private void DeviceConnect()
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

        private void MakeList(Bitmap bmp)
        {
            if(bmp == null)
            {
                MessageBox.Show("선택된 이미지 없음");
                return;
            }
            

            // 비트맵 이미지
            

            imgWidth = bmp.Width;
            imgHeight = bmp.Height;

            // flag
            // flag true - 흰색
            // flag false - 검은색
            bool flag = false;

            // EndColumn
            // EndColumn false
            // 해당 컬럼영역이 아직 만들어 지는중
            // EndColumn true
            // 컬럼 데이터 만들기 끝
            bool EndColumn = false;

            // toolPathHatchingLine - 구조체
            // Point Start, End를 가지고 있다
            // toolPathPoint의 약자로 tpp로 명명함
            toolPathHatchingLine tpp;
            tpp.Start = new Point(0, 0);
            tpp.End = new Point(0, 0);

            // Current
            // 마지막 좌표를
            // 저장하는 변수
            Point Current = new Point(0, 0);

            // 해칭 간격
            //hatchInterval = 25;
            // 5 : 0.1mm

            // 반복문에서 사용할 변수들
            int tempY = 0;
            int count = 0;
            int colCount = 0;
            int rowCount = 0;
            int colorCount = 0;

            // toolPathColumnManager
            List<toolPathHatchingLine> toolPathColumnManager = new List<toolPathHatchingLine>();

            // 2중 for
            // 세로를 기준 - h
            // 픽셀을 하나하나 확인하면서
            // 이진화 작업을 진행함
            for (int h = 0; h < bmp.Height; h++)
            {
                for (int w = 0; w < bmp.Width; w++)
                {
                    // 가져온 영역의
                    // rgb 계산
                    // ex) R 128 G 128 B 128
                    // 회색.
                    // 이보다 높은 값 = 흰색
                    // 그 외에 모든 값 = 검은색
                    //if ((h==173) && (flag))
                    //{
                    //    Console.WriteLine("디버깅 지점");
                    //}
                    //if ((h == 173) && (!flag))
                    //{

                    //}
                    //if ((w == 424) && (h == 173))
                    //{
                    //    Console.WriteLine("디버깅 지점");
                    //}
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
                            if (colorCount == 0)
                            {
                                tpp.End = tpp.Start;
                                flag = false;
                                EndColumn = true;
                                toolPathColumnManager.Add(tpp);
                                colCount++;
                            }
                            else
                            {
                                tpp.End = Current;
                                colorCount = 0;
                                flag = false;
                                EndColumn = true;
                                toolPathColumnManager.Add(tpp);
                                colCount++;
                            }
                            //Console.WriteLine(tpp);
                        }
                        //컬럼 탐색 끝
                        else if ((EndColumn) && (w == (bmp.Width - 1)))
                        {
                            tpp.End = Current;
                            colorCount = 0;
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
                            /* 툴패스 컬럼매니저
                             * 컬럼 갯수 확인
                            foreach (var item in toolPathColumnManager)
                            {
                                Console.WriteLine(w.ToString() + "," + h.ToString());
                                Console.WriteLine($"{item}");
                            }*/
                            // 다음 간격 Y로 이동
                            tempY += hatchInterval;
                        }
                        // 컬럼 끝
                    }
                    // 검은색 영역
                    else
                    {
                        bmp.SetPixel(w, h, System.Drawing.Color.Black);
                        // 검은색 영역을
                        // 처음 만났을 때

                        //영역의 0~499가
                        //다 검은색일때
                        if ((flag) && (w == (bmp.Width - 1)))
                        {
                            tpp.End = new Point(w, h);
                            colorCount = 0;
                            toolPathColumnManager.Add(tpp);
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
                            //다시 첫 검은색 탐색
                            flag = false;
                            tempY += hatchInterval;
                        }
                        if ((!flag) && (count == 0))
                        {
                            // 처음 만난 검은색
                            // 좌표 저장
                            tpp.Start = new Point(w, h);
                            flag = true;
                            count++;
                            tempY = h;
                        }
                        else if ((!flag) && (count != 0))
                        {
                            // 해칭 간격에 맞는
                            // 검은색 영역 데이터
                            if (h == tempY)
                            {
                                // 해칭라인 Row 0, Column 0
                                tpp.Start = new Point(w, h);
                                flag = true;
                                //tempY += hatchInterval;
                            }
                        }
                        // 검은색 탐색 데이터
                        // Current에 저장
                        else
                        {
                            Current = new Point(w, h);
                            colorCount++;
                            continue;
                        }
                    }
                }
            } // bmp 이미지 이진화 완료
              //HatchingManager.Add(new List<List<List<toolPathHatchingLine>>> { toolPathManager });
              //int row, col = 0;
              // 리스트의 데이터 확인
            /*foreach (List<toolPathHatchingLine> item in toolPathManager)
            {
                Console.WriteLine(item.Count);
            }*/

            //CNC 할 이미지
            myImage = new System.Windows.Controls.Image();
            //myImage.Source = bitmapSource;
            myImage.Width = 500;
            myImage.Height = 500;

            //화면에 보여줄 이미지
         
            #region 변환된 비트맵 이미지 확인
            //변환된 비트맵 이미지
            //비트맵 to 비트맵소스
            //stackpnlImage.Children.Remove(preImage);
            //preImage.Source = GetBitmapSourceFromBitmap(bmp);
            //stackpnlImage.Children.Add(preImage);
            #endregion
        }

        //이미지 불러오기 버튼 클릭시
        private void btnImageOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            if (openDialog.ShowDialog() == true)
            {
                if (File.Exists(openDialog.FileName))
                {
                    stackpnlImage.Children.Clear();
                    _currentName = openDialog.FileName;

                    System.Drawing.Image img = System.Drawing.Image.FromFile(_currentName);
                    //img.Save("image.bmp", ImageFormat.Bmp);
                    Bitmap bmp = new Bitmap(img);

                    System.Windows.Controls.Image preImage = new System.Windows.Controls.Image();
                    preImage.Source = GetBitmapSourceFromBitmap(bmp);
                    preImage.Width = 200;
                    preImage.Height = 200;
                    _currentBmp = bmp;
                    stackpnlImage.Children.Add(preImage);
                }
            }
        }
        // 조각시작 버튼 클릭시
        private void btnOperationStart_Click(object sender, RoutedEventArgs e)
        {
            MakeList(_currentBmp);

            initToolpath();
            #region 지역변수
            Point start, end, nextStart, nextEnd, tphlTemp;
            Point toolStartPoint = new Point(0, 0);
            Point toolEndPoint = new Point(0, 0);
            Point tempPoint = new Point(0, 0);
            Point current = new Point(0, 0);
            List<toolPathHatchingLine> toolPathRowManager = new List<toolPathHatchingLine>();
            List<toolPathHatchingLine> toolPathColManager = new List<toolPathHatchingLine>();
            toolPathHatchingLine tphlStart, tphlCurrent;
            int row = 0;
            int col = 0;
            //upZ = -80;
            //downZ = 100;
            // 공구를 드는 속도
            string toolUpSpeed = "VS30;";
            // 공구를 내리는 속도
            string toolDownSpeed = "VS24;";
            // 공구 움직이는 속도
            //toolMoveSpeed = "VS36;";

            // 왼쪽에서 오른쪽
            // 오른쪽에서 왼쪽
            // 제어하는 flag
            bool leftToRight;

            // 공구를 내렸을때
            // 해칭 그리기가
            // 끝났는지
            bool isDrawLastRow = false;
            // 공구 올리고
            // 해칭 그리기 첫번째인지?
            bool isDrawFirstRow = false;

            // 첫번째 로우를 그렸을때
            // col이 두개 이상이면
            bool isFirstRow = false;

            // 공구를 들었는지 내렸는지
            bool isToolUp = false;
            #endregion
            #region 첫 데이터 그리기
            //이미지 위치
            //이미지 크기
            //전송하기
            // 좌상단 X의 위치와 Y의 위치 구하기
            // 좌상단 - 245, 117

            // 첫 데이터
            // row 0, col 0
            start = toolPathManager[row][col].Start;
            end = toolPathManager[row][col].End;
            tphlStart.Start = start;
            tphlStart.End = end;
            toolPathRowManager.Add(tphlStart);
            #region 이미지의 영역과 이미지의 크기 계산
            toolStartPoint.X = start.X;
            toolStartPoint.Y = start.Y;
            // 끝점을 저장하기 위한
            // 이중 포문
            for (int i = 0; i < toolPathManager.Count; i++)
            {
                for (int j = 0; j < toolPathManager[i].Count; j++)
                {
                    if (((i + 1) == toolPathManager.Count) && (j + 1) == toolPathManager[i].Count)
                    {
                        toolEndPoint = toolPathManager[i][j].End;
                    }
                }
            }
            string toolstartP = toolStartPoint.ToString();
            int width = toolEndPoint.X - start.X;
            int height = toolEndPoint.Y - start.Y;
            string path = "!MH" + toolStartPoint.X.ToString() + "," + toolStartPoint.Y.ToString() + ","
                                    + width.ToString() + "," + height.ToString() + "," + "0,2,1,1,1;";
            order.Add(path);
            path = "!MH" + toolStartPoint.X.ToString() + "," + toolStartPoint.Y.ToString() + ","
                                    + width.ToString() + "," + height.ToString() + "," + "0,1,0,1,1;";
            order.Add(path);
            order.Add("!SR0;");
            #endregion

            // 툴패스 저장
            string toolUpStart = makeToolpath(start, upZ);
            string toolUpEnd = makeToolpath(end, upZ);
            string startP = makeToolpath(start, downZ);
            string endP = makeToolpath(end, downZ);
            string toolMove = "";
            string toolUpLastPoint = makeToolpath(current, upZ);

            // 첫 좌표 공구 이동
            order.Add("VS30;");
            order.Add(toolUpStart);
            order.Add(toolUpStart);
            order.Add(toolUpStart);

            // 첫 좌표 공구 내리기
            order.Add("VS24;");
            order.Add(startP);

            // 첫 공구 이동
            order.Add(toolMoveSpeed);
            order.Add(startP);
            order.Add(endP);
            current = end;

            // 첫라인
            // Left -> Right 수행
            // flag -> false
            leftToRight = false;
            //row0, col0 삭제
            if (toolPathManager[row].Count == 1)
            {
                toolPathManager.RemoveAt(0);
            }
            else
            {
                toolPathManager[row].RemoveAt(0);
                isFirstRow = true;
            }
            int count = 0;
            #endregion
            #region 해칭 알고리즘
            while (toolPathManager.Count != 0)
            //while (count != 2)
            {
                for (int i = 0; i < toolPathManager.Count; i++)
                {

                    if (i == toolPathManager.Count)
                    {
                        break;
                    }
                    for (int j = 0; j < toolPathManager[i].Count; j++)
                    {
                        if (isFirstRow)
                        {
                            isFirstRow = false;
                            break;
                        }

                        nextStart = toolPathManager[i][j].Start;
                        nextEnd = toolPathManager[i][j].End;

                        if (!leftToRight)
                        {
                            //마지막 라인
                            if (isDrawLastRow)
                            {
                                current = nextStart;
                                //공구 이동
                                toolMove = makeToolpath(current, upZ);
                                order.Add(toolMove);
                                order.Add(toolMoveSpeed);
                                isDrawLastRow = false;
                                isDrawFirstRow = true;
                            }

                            //다음 라인의 Y값이
                            //현재 라인의 Y값보다
                            //큰 경우
                            if (nextEnd.Y > (current.Y) + hatchInterval)
                            {
                                toolMove = makeToolpath(current, upZ);
                                order.Add(toolMove);
                                //사용 변수들 초기화
                                start = new Point(0, 0);
                                end = new Point(0, 0);
                                current = new Point(0, 0);
                                leftToRight = true;
                                isDrawLastRow = true;
                                break;
                            }

                            if (!isDrawFirstRow)
                            {
                                // 현재 라인의 끝점이
                                // 다음 라인의 사이에 있는지? 
                                if ((nextStart.X <= current.X) && (current.X <= nextEnd.X))
                                {
                                    tempPoint = new Point(current.X, nextEnd.Y);
                                    //공구 내림
                                    toolMove = makeToolpath(tempPoint, downZ);
                                    order.Add(toolMove);
                                    //공구 이동 nextEnd
                                    toolMove = makeToolpath(nextEnd, downZ);
                                    order.Add(toolMove);
                                    // 공구 이동 nextStart
                                    toolMove = makeToolpath(nextStart, downZ);
                                    order.Add(toolMove);
                                }
                                // 현재 라인의 시작점이
                                // 다음 라인의 사이에 있는지?
                                else if ((nextStart.X <= start.X) && (start.X <= nextEnd.X))
                                {
                                    tempPoint = new Point(nextEnd.X, current.Y);
                                    toolMove = makeToolpath(tempPoint, downZ);
                                    order.Add(toolMove);
                                    toolMove = makeToolpath(nextEnd, downZ);
                                    order.Add(toolMove);
                                    toolMove = makeToolpath(nextStart, downZ);
                                    order.Add(toolMove);
                                }
                                //현재 라인이
                                //이전 라인에
                                //포함되어 있을때
                                else if (
                                         ((start.X <= nextStart.X) && (nextStart.X <= current.X))
                                          && 
                                         ((start.X <= nextEnd.X) && (nextEnd.X <= current.X))
                                         )
                                {
                                    //마지막 행일때
                                    //공구 들기 만들기
                                    //[05.15] 수정 필요
                                    if (i + 1 == toolPathManager.Count)
                                    {
                                        toolMove = makeToolpath(current, upZ);
                                        order.Add(toolUpSpeed);
                                        order.Add(toolMove);
                                        order.Add(toolMove);
                                        order.Add(toolMove);
                                        //사용 변수들 초기화
                                        start = new Point(0, 0);
                                        end = new Point(0, 0);
                                        current = new Point(0, 0);
                                        leftToRight = true;
                                        isDrawLastRow = true;
                                        break;
                                    }
                                    else
                                    {
                                        //공구 이동
                                        tempPoint = new Point(nextEnd.X, current.Y);
                                        toolMove = makeToolpath(tempPoint, downZ);
                                        order.Add(toolMove);
                                        //공구 내림
                                        toolMove = makeToolpath(nextEnd, downZ);
                                        order.Add(toolMove);
                                        //공구 이동
                                        toolMove = makeToolpath(nextStart, downZ);
                                        order.Add(toolMove);
                                    }
                                }
                                else
                                {
                                    //리스트의 컬럼이 2개 이상인 경우
                                    if (toolPathManager[i].Count > 1)
                                    {
                                        //컬럼을 끝까지 탐색했는데
                                        //그릴게 없다
                                        //공구 들기
                                        if (i + 1 == toolPathManager.Count)
                                        {

                                            toolMove = makeToolpath(current, upZ);
                                            order.Add(toolMove);
                                            //사용 변수들 초기화
                                            start = new Point(0, 0);
                                            end = new Point(0, 0);
                                            current = new Point(0, 0);
                                            leftToRight = false;
                                            isDrawLastRow = true;
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    //리스트의 컬럼이 1개인 경우
                                    else
                                    {
                                        toolMove = makeToolpath(current, upZ);
                                        order.Add(toolMove);
                                        //사용 변수들 초기화
                                        start = new Point(0, 0);
                                        end = new Point(0, 0);
                                        current = new Point(0, 0);
                                        leftToRight = false;
                                        isDrawLastRow = true;
                                        break;
                                    }
                                }
                            }
                            start = nextStart;
                            end = nextEnd;
                            toolMove = makeToolpath(end, downZ);
                            order.Add(toolMove);
                            toolMove = makeToolpath(start, downZ);
                            order.Add(toolMove);
                            leftToRight = true;
                            current = nextStart;
                            isDrawFirstRow = false;
                            toolPathManager[i].RemoveAt(j);
                            if (toolPathManager[i].Count == 0)
                            {
                                toolPathManager.RemoveAt(i);
                                i -= 1;
                            }
                            // 마지막 라인인가?
                            // 맞으면 공구 올림
                            // 공구 들기 1
                            if (i + 1 == toolPathManager.Count)
                            {
                                toolMove = makeToolpath(current, upZ);
                                order.Add(toolUpSpeed);
                                order.Add(toolMove);
                                order.Add(toolMove);
                                order.Add(toolMove);
                                //사용 변수들 초기화
                                start = new Point(0, 0);
                                end = new Point(0, 0);
                                current = new Point(0, 0);
                                isDrawLastRow = true;
                            }
                            break;
                        }
                        else
                        {
                            if (isDrawLastRow)
                            {
                                current = nextEnd;
                                //공구 이동
                                toolMove = makeToolpath(current, upZ);
                                order.Add(toolMove);
                                order.Add(toolMoveSpeed);
                                isDrawLastRow = false;
                                isDrawFirstRow = true;
                            }
                            if (!isDrawFirstRow)
                            {
                                //다음 라인의 Y값이
                                //현재 라인의 Y값보다
                                //큰 경우
                                if (nextEnd.Y > (current.Y) + hatchInterval)
                                {
                                    toolMove = makeToolpath(current, upZ);
                                    order.Add(toolMove);
                                    //사용 변수들 초기화
                                    start = new Point(0, 0);
                                    end = new Point(0, 0);
                                    current = new Point(0, 0);
                                    leftToRight = false;
                                    isDrawLastRow = true;
                                    break;
                                }
                                // 현재 라인의 끝점이
                                // 다음 라인의 사이에 있는지? 
                                if ((nextStart.X <= current.X) && (current.X <= nextEnd.X))
                                {
                                    //기본
                                    tempPoint = new Point(current.X, nextEnd.Y);
                                    //공구 내림
                                    toolMove = makeToolpath(tempPoint, downZ);
                                    order.Add(toolMove);
                                    // 공구 이동 nextStart
                                    toolMove = makeToolpath(nextStart, downZ);
                                    order.Add(toolMove);
                                    //공구 이동 nextEnd
                                    toolMove = makeToolpath(nextEnd, downZ);
                                    order.Add(toolMove);
                                }
                                // 현재 라인의 시작점이
                                // 다음 라인의 사이에 있는지?
                                else if ((nextStart.X <= end.X) && (end.X <= nextEnd.X))
                                {
                                    tempPoint = new Point(nextStart.X, current.Y);
                                    toolMove = makeToolpath(tempPoint, downZ);
                                    order.Add(toolMove);
                                    toolMove = makeToolpath(nextStart, downZ);
                                    order.Add(toolMove);
                                    toolMove = makeToolpath(nextEnd, downZ);
                                    order.Add(toolMove);
                                }
                                // 현재 라인이
                                // 다음 라인을
                                // 포함할 때
                                else if (
                                         ((start.X <= nextStart.X) && (nextStart.X <= current.X))
                                                                   &&
                                           ((start.X <= nextEnd.X) && (nextEnd.X <= current.X))
                                         )
                                {
                                    if (i + 1 == toolPathManager.Count)
                                    {
                                        toolMove = makeToolpath(current, upZ);
                                        order.Add(toolUpSpeed);
                                        order.Add(toolMove);
                                        order.Add(toolMove);
                                        order.Add(toolMove);
                                        //사용 변수들 초기화
                                        start = new Point(0, 0);
                                        end = new Point(0, 0);
                                        current = new Point(0, 0);
                                        leftToRight = false;
                                        isDrawLastRow = true;
                                        break;
                                    }
                                    else
                                    {
                                        //공구 이동
                                        tempPoint = new Point(nextStart.X, current.Y);
                                        toolMove = makeToolpath(tempPoint, downZ);
                                        order.Add(toolMove);
                                        //공구 내림
                                        toolMove = makeToolpath(nextStart, downZ);
                                        order.Add(toolMove);
                                        //공구 이동
                                        toolMove = makeToolpath(nextEnd, downZ);
                                        order.Add(toolMove);
                                    }
                                }
                                //답X, 공구 들어야함
                                else
                                {
                                    //리스트의 컬럼이 2개 이상인 경우
                                    if (toolPathManager[i].Count > 1)
                                    {
                                        //컬럼을 끝까지 탐색했는데
                                        //그릴게 없다
                                        //공구 들기
                                        if (i + 1 == toolPathManager.Count)
                                        {
                                            toolMove = makeToolpath(current, upZ);
                                            order.Add(toolMove);
                                            //사용 변수들 초기화
                                            start = new Point(0, 0);
                                            end = new Point(0, 0);
                                            current = new Point(0, 0);
                                            leftToRight = false;
                                            isDrawLastRow = true;
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    //리스트의 컬럼이 1개인 경우
                                    else
                                    {
                                        toolMove = makeToolpath(current, upZ);
                                        order.Add(toolMove);
                                        //사용 변수들 초기화
                                        start = new Point(0, 0);
                                        end = new Point(0, 0);
                                        current = new Point(0, 0);
                                        leftToRight = false;
                                        isDrawLastRow = true;
                                        break;
                                    }
                                }
                            }
                            start = nextStart;
                            end = nextEnd;
                            toolMove = makeToolpath(start, downZ);
                            order.Add(toolMove);
                            toolMove = makeToolpath(end, downZ);
                            order.Add(toolMove);
                            leftToRight = false;
                            current = nextEnd;
                            isDrawFirstRow = false;
                            toolPathManager[i].RemoveAt(j);
                            if (toolPathManager[i].Count == 0)
                            {
                                toolPathManager.RemoveAt(i);
                                i -= 1;
                            }
                            // 마지막 라인인가?
                            // 맞으면 공구 올림
                            if (i + 1 == toolPathManager.Count)
                            {
                                toolMove = makeToolpath(current, upZ);
                                order.Add(toolUpSpeed);
                                order.Add(toolMove);
                                order.Add(toolMove);
                                order.Add(toolMove);
                                //사용 변수들 초기화
                                start = new Point(0, 0);
                                end = new Point(0, 0);
                                current = new Point(0, 0);
                                isDrawLastRow = true;
                            }
                            break;
                        }
                    }
                }
            }
            order.Add("!VO;");
            #endregion
            #region EX-HPGL
            for (int i = 0; i < order.Count; i++)
            {
                sp.WriteLine(order[i]);
            }
            #endregion
        }
        #endregion
        #region 메서드

        #region 툴패스 초기화
        //툴패스를 초기화 하는 메서드
        private void initToolpath()
        {
            order.Add("IN;");
            order.Add("!CL1;");
            order.Add("!PM0,0;");

        }
        #endregion
        #region 툴패스 만들기
        // 툴패스를 만드는 메서드
        private string makeToolpath(Point point, int z)
        {
            Point _point = point;
            string x = point.X.ToString() + ",";
            string y = point.Y.ToString() + ",";
            string _z = z.ToString();
            string toolpath = "!ZZ" + x + y + z;
            return toolpath;
        }
        #endregion
        #region 두점 사이의 개수
        //두점 사이의 개수
        private bool checkMeetLine(Point start, Point end, Point nextstart, Point nextend)
        {
            //x1,y1
            Point AP1 = start;
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
        #endregion
        #region 이미지 소스 to 비트맵
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
        #endregion
        #region 비트맵 to 이미지 소스
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
        #endregion
        #region 시리얼 포트 데이터 리시브 이벤트
        //시리얼 포트 데이터 리시브 이벤트
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string getData = sp.ReadExisting();
            Data.Enqueue(getData);
            Thread.Sleep(1);
        }
        #endregion
        #endregion
        #region 기계 설정

        private void btnMoveToolSpeed_Click(object sender, RoutedEventArgs e)
        {
            string setMachineOperation = "";
            setMachineOperation = txtMoveToolSpeed.Text;
            toolMoveSpeed = "VS" + setMachineOperation + ";";
            lblMoveToolSpeed.Content = "속도 : " + setMachineOperation + "mm/s";
        }

        private void btnDownZ_Click(object sender, RoutedEventArgs e)
        {
            string setMachineOperation = "";
            double cal;
            setMachineOperation = txtDownZ.Text;
            cal = Convert.ToDouble(setMachineOperation) * 1000;
            downZ = (int)cal;
            lblDownZ.Content = "깊이 : " + setMachineOperation + "mm";
        }

        private void btnUpZ_Click(object sender, RoutedEventArgs e)
        {
            string setMachineOperation = "";
            double cal;
            setMachineOperation = txtUpZ.Text;
            cal = Convert.ToDouble(setMachineOperation) * 1000;
            upZ = -(int)cal;
            lblUpz.Content = "이동높이 : " + setMachineOperation + "mm";
        }

        private void btnHatchInterval_Click(object sender, RoutedEventArgs e)
        {
            string setMachineOperation = "";
            double cal;
            setMachineOperation = txtHatchInterval.Text;
            cal = Convert.ToDouble(setMachineOperation) * 50;
            hatchInterval = (int)cal;
            lblHatchInterval.Content = "해칭간격 : " + setMachineOperation + "mm";
        }
        #endregion
    }
}
