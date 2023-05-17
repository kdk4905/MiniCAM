using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Reflection.Emit;
using System.Collections;
using System.Threading;

namespace MiniCAM
{
    public class Connection
    {
        #region 필드
        Queue Data = new Queue();
        SerialPort SerialPort = new SerialPort();
        #endregion

        #region 생성자
        public Connection(SerialPort sp, Queue data) 
        {
            SerialPort = sp;
            Data = data;
        }
        #endregion

        #region 메서드
        public string DeviceConnect(SerialPort sp, string portName) 
        {

            if (!sp.IsOpen)
            {
                sp.PortName = portName;
                sp.BaudRate = 115200;
                sp.DataBits = 8;
                sp.Parity = Parity.None;
                sp.StopBits = StopBits.One;

                sp.Open(); //시리얼포트 열기
                sp.WriteLine("!BP1;");

                sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);

                string msg = "포트가 연결되었습니다.";
                return msg; 
            }
            else
            {
                string msg = "연결상태 : 이미 포트와 연결되었습니다.";
                return msg;
            }
        }

        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string getData = SerialPort.ReadExisting();
            Data.Enqueue(getData);
            Thread.Sleep(1);
        }
        #endregion
    }
}
