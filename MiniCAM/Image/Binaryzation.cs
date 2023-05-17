using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MiniCAM;

namespace MiniCAM.Image
{
    //public class Binaryzation
    //{
    //    ToolPathStruct toolPathStruct = new ToolPathStruct();

    //    List<List<toolPathHatchingLine>> toolPathManager = new List<List<toolPathHatchingLine>>();

    //    public List<List<toolPathHatchingLine>> MakeList(Bitmap bmp)
    //    {
    //        if (bmp == null)
    //        {
    //            MessageBox.Show("선택된 이미지 없음");
    //            return;
    //        }


    //        // 비트맵 이미지


    //        imgWidth = bmp.Width;
    //        imgHeight = bmp.Height;

    //        // flag
    //        // flag true - 흰색
    //        // flag false - 검은색
    //        bool flag = false;

    //        // EndColumn
    //        // EndColumn false
    //        // 해당 컬럼영역이 아직 만들어 지는중
    //        // EndColumn true
    //        // 컬럼 데이터 만들기 끝
    //        bool EndColumn = false;

    //        // toolPathHatchingLine - 구조체
    //        // Point Start, End를 가지고 있다
    //        // toolPathPoint의 약자로 tpp로 명명함
    //        toolPathHatchingLine tpp;
    //        tpp.Start = new Point(0, 0);
    //        tpp.End = new Point(0, 0);

    //        // Current
    //        // 마지막 좌표를
    //        // 저장하는 변수
    //        Point Current = new Point(0, 0);

    //        // 해칭 간격
    //        //hatchInterval = 25;
    //        // 5 : 0.1mm

    //        // 반복문에서 사용할 변수들
    //        int tempY = 0;
    //        int count = 0;
    //        int colCount = 0;
    //        int rowCount = 0;
    //        int colorCount = 0;

    //        // toolPathColumnManager
    //        List<toolPathHatchingLine> toolPathColumnManager = new List<toolPathHatchingLine>();

    //        // 2중 for
    //        // 세로를 기준 - h
    //        // 픽셀을 하나하나 확인하면서
    //        // 이진화 작업을 진행함
    //        for (int h = 0; h < bmp.Height; h++)
    //        {
    //            for (int w = 0; w < bmp.Width; w++)
    //            {
    //                // 가져온 영역의
    //                // rgb 계산
    //                // ex) R 128 G 128 B 128
    //                // 회색.
    //                // 이보다 높은 값 = 흰색
    //                // 그 외에 모든 값 = 검은색
    //                //if ((h==173) && (flag))
    //                //{
    //                //    Console.WriteLine("디버깅 지점");
    //                //}
    //                //if ((h == 173) && (!flag))
    //                //{

    //                //}
    //                //if ((w == 424) && (h == 173))
    //                //{
    //                //    Console.WriteLine("디버깅 지점");
    //                //}
    //                color = bmp.GetPixel(w, h);
    //                rgb = (color.R + color.G + color.B) / 3;
    //                if (rgb > ((128 + 128 + 128) / 3))
    //                {
    //                    bmp.SetPixel(w, h, System.Drawing.Color.White);
    //                    // 흰 영역을 만났을때
    //                    // 직전 영역(Current)을 tpp.End에 저장
    //                    // 컬럼 추가
    //                    if (flag)
    //                    {
    //                        if (colorCount == 0)
    //                        {
    //                            tpp.End = tpp.Start;
    //                            flag = false;
    //                            EndColumn = true;
    //                            toolPathColumnManager.Add(tpp);
    //                            colCount++;
    //                        }
    //                        else
    //                        {
    //                            tpp.End = Current;
    //                            colorCount = 0;
    //                            flag = false;
    //                            EndColumn = true;
    //                            toolPathColumnManager.Add(tpp);
    //                            colCount++;
    //                        }
    //                        //Console.WriteLine(tpp);
    //                    }
    //                    //컬럼 탐색 끝
    //                    else if ((EndColumn) && (w == (bmp.Width - 1)))
    //                    {
    //                        tpp.End = Current;
    //                        colorCount = 0;
    //                        flag = false;
    //                        EndColumn = false;
    //                        toolPathManager.Add(new List<toolPathHatchingLine>());
    //                        int index = 0;
    //                        while (toolPathColumnManager.Count != 0)
    //                        {
    //                            toolPathManager[rowCount].Add(toolPathColumnManager[index]);
    //                            toolPathColumnManager.RemoveAt(0);
    //                            if (toolPathColumnManager.Count != 0)
    //                            {
    //                                continue;
    //                            }
    //                            else
    //                            {
    //                                rowCount++;
    //                            }
    //                        }
    //                        /* 툴패스 컬럼매니저
    //                         * 컬럼 갯수 확인
    //                        foreach (var item in toolPathColumnManager)
    //                        {
    //                            Console.WriteLine(w.ToString() + "," + h.ToString());
    //                            Console.WriteLine($"{item}");
    //                        }*/
    //                        // 다음 간격 Y로 이동
    //                        tempY += hatchInterval;
    //                    }
    //                    // 컬럼 끝
    //                }
    //                // 검은색 영역
    //                else
    //                {
    //                    bmp.SetPixel(w, h, System.Drawing.Color.Black);
    //                    // 검은색 영역을
    //                    // 처음 만났을 때

    //                    //영역의 0~499가
    //                    //다 검은색일때
    //                    if ((flag) && (w == (bmp.Width - 1)))
    //                    {
    //                        tpp.End = new Point(w, h);
    //                        colorCount = 0;
    //                        toolPathColumnManager.Add(tpp);
    //                        toolPathManager.Add(new List<toolPathHatchingLine>());
    //                        int index = 0;
    //                        while (toolPathColumnManager.Count != 0)
    //                        {

    //                            toolPathManager[rowCount].Add(toolPathColumnManager[index]);
    //                            toolPathColumnManager.RemoveAt(0);
    //                            if (toolPathColumnManager.Count != 0)
    //                            {
    //                                continue;
    //                            }
    //                            else
    //                            {
    //                                rowCount++;
    //                            }
    //                        }
    //                        //다시 첫 검은색 탐색
    //                        flag = false;
    //                        tempY += hatchInterval;
    //                    }
    //                    if ((!flag) && (count == 0))
    //                    {
    //                        // 처음 만난 검은색
    //                        // 좌표 저장
    //                        tpp.Start = new Point(w, h);
    //                        flag = true;
    //                        count++;
    //                        tempY = h;
    //                    }
    //                    else if ((!flag) && (count != 0))
    //                    {
    //                        // 해칭 간격에 맞는
    //                        // 검은색 영역 데이터
    //                        if (h == tempY)
    //                        {
    //                            // 해칭라인 Row 0, Column 0
    //                            tpp.Start = new Point(w, h);
    //                            flag = true;
    //                            //tempY += hatchInterval;
    //                        }
    //                    }
    //                    // 검은색 탐색 데이터
    //                    // Current에 저장
    //                    else
    //                    {
    //                        Current = new Point(w, h);
    //                        colorCount++;
    //                        continue;
    //                    }
    //                }
    //            }
    //        } // bmp 이미지 이진화 완료
    //          //HatchingManager.Add(new List<List<List<toolPathHatchingLine>>> { toolPathManager });
    //          //int row, col = 0;
    //          // 리스트의 데이터 확인
    //        /*foreach (List<toolPathHatchingLine> item in toolPathManager)
    //        {
    //            Console.WriteLine(item.Count);
    //        }*/

    //        //CNC 할 이미지
    //        myImage = new System.Windows.Controls.Image();
    //        //myImage.Source = bitmapSource;
    //        myImage.Width = 500;
    //        myImage.Height = 500;

    //        //화면에 보여줄 이미지

    //        #region 변환된 비트맵 이미지 확인
    //        //변환된 비트맵 이미지
    //        //비트맵 to 비트맵소스
    //        //stackpnlImage.Children.Remove(preImage);
    //        //preImage.Source = GetBitmapSourceFromBitmap(bmp);
    //        //stackpnlImage.Children.Add(preImage);
    //        #endregion
    //    }
    //}
}
