using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading;
using System.IO.Ports;



using System.IO;


namespace CCD
{
    public partial class Form1 : Form
    {
        private byte[] data = new byte[1024];
        private Thread t;
        private Thread t1;
        Socket clientSocket;
        bool server_open_flag = false;
        bool server_open_change = false;
        bool tcp_sta_change = false;
        bool main_form_close_flag = false;
        Series series1;
        int osc_x = 1;  // 파형 X 좌표
        private List<byte> data_buffer = new List<byte>(7296);//기본적으로 1페이지의 메모리를 할당하고 항상 초과하지 않도록 제한
        private List<byte> ecg_buffer = new List<byte>(4096);
        bool ReceiveData_Flag;    //수신 프로세스 중에 직렬 포트가 닫히지 않도록 수신 플래그를 설정
        int[] data_temp = new int[3648];


        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            createSeries();
            CreateChart();
            comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());    //사용 가능한 포트 번호 가져오기
            serialPort1.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
        }
        private void CreateChart()
        {
            ChartArea chartArea = new ChartArea();
            chartArea.Name = "FirstArea";

            chartArea.AxisX.ScrollBar.Enabled = true;       //스크롤 바 활성화
            chartArea.AxisX.ScaleView.Zoomable = true;
            chartArea.AxisX.ScrollBar.ButtonStyle = ScrollBarButtonStyles.All;//X축 스크롤 막대 버튼 활성화
            chartArea.AxisX.IsLabelAutoFit = true;
            chartArea.AxisX.LabelAutoFitMinFontSize = 5;

            chartArea.CursorX.AutoScroll = true;            //스크롤 여부
            chartArea.CursorX.IsUserEnabled = true;
            chartArea.CursorX.IsUserSelectionEnabled = true;
            chartArea.CursorX.SelectionColor = Color.SkyBlue;
            chartArea.CursorX.IntervalType = DateTimeIntervalType.Auto;

            chartArea.CursorY.IsUserEnabled = true;
            chartArea.CursorY.AutoScroll = true;
            chartArea.CursorY.IsUserSelectionEnabled = true;
            chartArea.CursorY.SelectionColor = Color.SkyBlue;
        
            chartArea.BackColor = Color.White;                      //배경색
            //chartArea.BackSecondaryColor = Color.White;           //그라데이션 배경색
            chartArea.BackGradientStyle = GradientStyle.TopBottom;  //그라데이션 방식
            chartArea.BackHatchStyle = ChartHatchStyle.None;        //배경 그림자
            chartArea.BorderDashStyle = ChartDashStyle.NotSet;      //경계선 스타일
            chartArea.BorderWidth = 1;                              //테두리 너비
            chartArea.BorderColor = Color.Black;

            // Axis
            //chartArea.AxisY.Title = @"Value";
            //chartArea.AxisY.LabelAutoFitMinFontSize = 5;
            //chartArea.AxisY.LineWidth = 2;
            //chartArea.AxisY.LineColor = Color.Black;
            //chartArea.AxisY.Enabled = AxisEnabled.True;
            chartArea.AxisY.IsLabelAutoFit = true;
            chartArea.AxisY.LabelAutoFitMinFontSize = 5;

            //chartArea.AxisX.Title = @"Time";
            //chartArea.AxisX.LabelStyle.Angle = -15;


            chartArea.AxisX.LabelStyle.IsEndLabelVisible = true;        //show the last label
            chartArea.AxisX.Interval = 10;
            chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chartArea.AxisX.IntervalType = DateTimeIntervalType.NotSet;
            chartArea.AxisX.TextOrientation = TextOrientation.Auto;
            chartArea.AxisX.LineWidth = 2;
            chartArea.AxisX.LineColor = Color.Black;
            chartArea.AxisX.Enabled = AxisEnabled.True;
            chartArea.AxisX.ScaleView.MinSizeType = DateTimeIntervalType.Months;
            chartArea.AxisX.Crossing = 0;

            chartArea.Position.Height = 90;
            chartArea.Position.Width = 90;
            chartArea.Position.X = 5;
            chartArea.Position.Y = 5;

            chart_ecg.ChartAreas.Add(chartArea);
            chart_ecg.BackGradientStyle = GradientStyle.TopBottom;
            //차트의 테두리 색상、
            chart_ecg.BorderlineColor = Color.FromArgb(26, 59, 105);
            //차트의 경계선 스타일
            chart_ecg.BorderlineDashStyle = ChartDashStyle.Solid;
            //차트 경계선의 너비
            chart_ecg.BorderlineWidth = 1;
            //차트 테두리 스킨
            //chart_ecg.BorderSkin.SkinStyle = BorderSkinStyle.FrameThin3;

            chart_ecg.ChartAreas[0].AxisX.Interval = 200;        //X 좌표의 해상도 설정
            chart_ecg.ChartAreas[0].AxisX.ScaleView.Size = 3800; //X 좌표 길이 설정

            chart_ecg.ChartAreas[0].AxisY.Interval = 500;        //Y 좌표의 해상도 설정
            chart_ecg.ChartAreas[0].AxisY.ScaleView.Size = 5000; //Y 좌표 길이 설정

            //chart_ecg.ChartAreas[0].AxisY.Enabled = AxisEnabled.True;
            //chart_ecg.ChartAreas[0].AxisX.Enabled = AxisEnabled.True;

            //눈금선 설정  
            chart_ecg.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.LightSkyBlue;            
            chart_ecg.ChartAreas[0].AxisX.MajorGrid.Interval = 200;//그리드 간격
            chart_ecg.ChartAreas[0].AxisX.MajorGrid.LineDashStyle= ChartDashStyle.Dash;///선 스타일

            chart_ecg.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightSkyBlue;
            chart_ecg.ChartAreas[0].AxisY.MajorGrid.Interval = 500;
            chart_ecg.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;///선 스타일


            chartArea.AxisX.MajorGrid.Enabled = true;
            chartArea.AxisY.MajorGrid.Enabled = true;
        }
        private void createSeries()
        {
            //Series1
            int i;
            series1 = new Series();
            series1.ChartArea = "FirstArea";
            chart_ecg.Series.Add(series1);

            //Series1 style
            series1.ToolTip = "#VALX,#VALY";    //데이터 포인트 위로 마우스를 가져가면 XY 값이 표시됨.

            series1.Name = "series1";
            series1.ChartType = SeriesChartType.Line;  // type:라인
            series1.BorderWidth = 1;
            series1.Color = Color.Red;
            series1.XValueType = ChartValueType.Int32;//x axis type
            series1.YValueType = ChartValueType.Int32;//y axis type

            //Marker
            series1.MarkerStyle = MarkerStyle.Square;
            series1.MarkerSize = 2;
            series1.MarkerColor = Color.Red;

            this.chart_ecg.Legends.Clear();

            for (i = 1; i < 3648; i++)
            {
                series1.Points.AddXY(i, data_temp[i]);   
            }
        }

        public void Write()
        {
            FileStream fs = new FileStream("E:\\ak.txt", FileMode.Create);

            //바이트 배열 가져오기
            byte[] data = System.Text.Encoding.Default.GetBytes("Hello World!");

            //쓰기 시작
            fs.Write(data, 0, data.Length);

            //버퍼 플래시 및 스트림 닫기
            fs.Flush();
            fs.Close();
        }

        //TCP 전송 정보 처리
        private void deal_data()
        {
            int i;            
            while (true)
            {
                int buffer_num = data_buffer.Count;    //처리 중 Count가 변경되지 않도록 미리 저장
                if (comboBox3.SelectedIndex == 0)////AD 샘플링 비트 설정
                {
                    if (buffer_num >= 7296)   //최소 4바이트
                    {
                        for (i = 1; i < 3648; i++) data_temp[i] = data_buffer[2 * i + 1] * 256 + data_buffer[2 * i];
                        Action act = delegate ()
                        {
                            series1.Points.Clear();////지우기
                            for (i = 1; i < 3648; i++)
                            {
                                
                                series1.Points.AddXY(i, data_temp[i]);   //추가
                            }
                        };
                        this.Invoke(act);
                        data_buffer.Clear();
                    }

                }
                else if (comboBox3.SelectedIndex == 1)////AD 샘플링 비트 설정
                {
                    if (buffer_num >= 3648)   
                    {
                        for (i = 1; i < 3648; i++) data_temp[i] = data_buffer[i] * 16;
                        Action act = delegate ()
                        {
                            series1.Points.Clear();
                            for (i = 1; i < 3648; i++)
                            {                                
                                series1.Points.AddXY(i, data_temp[i]);  
                            }
                        };
                        this.Invoke(act);
                        data_buffer.Clear();
                    }
                }
                else if (comboBox3.SelectedIndex == 2)
                {
                    if (buffer_num >= 12)   
                    {
                        if (data_buffer[0] == 0XFE)
                        {
                            textBox3.Text = Convert.ToString(data_buffer[1] * 100 + data_buffer[2]);   //최대 좌표
                            textBox4.Text = Convert.ToString(data_buffer[3] * 100 + data_buffer[4]);   //최소 자표
                            textBox5.Text = Convert.ToString(data_buffer[5] * 100 + data_buffer[6]);   //평균값
                            textBox6.Text = Convert.ToString(data_buffer[7] * 100 + data_buffer[8]);   //최대값
                            textBox7.Text = Convert.ToString(data_buffer[9] * 100 + data_buffer[10]);   //최소값
                            //series1.Points.AddXY(Convert.ToInt16(textBox3.Text), Convert.ToInt16(textBox6.Text));   //그림최대점
                            //series1.Points.AddXY(Convert.ToInt16(textBox4.Text), Convert.ToInt16(textBox7.Text));   //
                            
                        }
                        data_buffer.Clear();
                    }
                    if (main_form_close_flag == true)
                    {
                        t1.Abort();      //스레드 종료
                    }
                }

                if (main_form_close_flag == true)
                {
                    t1.Abort();      //스레드 종료
                }
            }
        }
        UInt64 receive_tick = 0;

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)//처리 이벤트 수신
        {
            this.BeginInvoke((EventHandler)(delegate    //데이터 처리를 위해 별도의 스레드 열기
            {
                ReceiveData_Flag = true;    //수신 프로세스 중에 직렬 포트가 닫히지 않도록 수신 플래그를 설정
                try
                {
                    int num = serialPort1.BytesToRead;  //수신 버퍼의 바이트 수 가져오기
                    if (num != 0)                       //데이터가 비어 있지 않은 경우
                    {
                        receive_tick = time_tick;   //현재 시간을 기록
                        byte[] received_buf = new byte[num];    //저장할 num 크기의 바이트 데이터 생성
                        serialPort1.Read(received_buf, 0, num); //수신 버퍼에서 배열로 num 바이트 읽기
                        data_buffer.AddRange(received_buf); //캐시에 저장
                    }
                }
                catch (Exception ex)
                {
                    //System.Media.SystemSounds.Beep.Play();
                    MessageBox.Show("에러:" + ex.Message, "메세지");
                }
                ReceiveData_Flag = false;//수신 플래그 지우기

            }
            ));
        }




        public void OpenPort()//대리인 호출을 위해 직렬 포트 열기
        {
            try
            {
                serialPort1.PortName = comboBox1.Text;
                serialPort1.BaudRate = 921600;////921600
                serialPort1.DataBits = 8;
                serialPort1.Parity = System.IO.Ports.Parity.None;
                serialPort1.StopBits = System.IO.Ports.StopBits.One;
                serialPort1.Open();
            }
            catch
            {
                MessageBox.Show("포트를 열지 못했습니다. 포트를 확인하십시오.", "에러");
            }
        }
        public void ClosePort()//대리인 호출을 위한 포트 닫기
        {
            try
            {
                while (ReceiveData_Flag == true) ;  //이 수신 처리가 완료될 때까지 기다리기.
                serialPort1.Close();
            }
            catch (Exception ex) { MessageBox.Show("에러:" + ex.Message, "메세지"); }
        }

        UInt64 time_tick = 5000;
        bool tcp_flag = false;





        //버튼 연결
        private void button_start_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                OpenPort();
                if (serialPort1.IsOpen == true)
                {
                    t1 = new Thread(deal_data);     //메시지를 처리하기 위해 스레드 실행 루프 열기
                    t1.Start();
                    button_start.Text = "연결 해제";
                    comboBox2.SelectedIndex = 0;////시간설정
                    comboBox3.SelectedIndex = 0;////AD 샘플링 비트 설정
                }
            }
            else
            {
                main_form_close_flag = true;
                button_start.Text = "연결";
                ClosePort();
            }
        }

        //포트로 데이터 보내기
        public void SendCommand(byte[] writeBytes)
        {
            //byte[] WriteBuffer = Convert.ToByte(CommandString,32);
            serialPort1.Write(writeBytes, 0, writeBytes.Length);
        }

        /// <summary>
        /// 바이트를 보내기
        /// </summary>
        /// <param name="port_DataWrite">보낼 바이트</param>
        /// <returns></returns>
        private void port_DataWrite(byte[] writeBytes)
        {           
             serialPort1.Write(writeBytes, 0, writeBytes.Length);
        }
        private void start_DataWrite()//캡처 시작 버튼
        {
            byte[] data = new byte[1];

            if (comboBox3.SelectedIndex == 0)////AD 샘플링 비트 설정
                data[0] = 0xA1;
            else if (comboBox3.SelectedIndex == 1)
                data[0] = 0xA2;
            else if (comboBox3.SelectedIndex == 2)
                data[0] = 0xA3;

            if (serialPort1.IsOpen == true)
            {
                port_DataWrite(data);
                data_buffer.Clear();////포트에서 모든 데이터 지우기
            }
            else
            {
                timer2.Stop();//예약 획득 시작 버튼
                MessageBox.Show("직렬 포트를 열지 못했습니다. 직렬 포트를 확인하십시오.", "에러");
                //timer2.Stop();//예약 획득 시작 버튼
            }
        }

        //캡처 시작 버튼
        private void button1_Click(object sender, EventArgs e)
        {
            start_DataWrite();


        }
       


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            main_form_close_flag = true;
            System.Windows.Forms.Application.Exit();
            System.Environment.Exit(0);//프로세스를 종료할 때 모든 스레드를 닫는 것이 매우 중요함. 이 코드가 없으면 페이지가 닫히고 스레드가 계속 열려 있음.
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void checkbox_chart_mode_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chart_ecg_Click(object sender, EventArgs e)
        {

        }

        //콤보 상자의 옵션 변경 이벤트
        private void comboBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            //콤보 상자에서 선택한 값이 변경되면 팝업 메시지 상자에 현재 콤보 상자에서 선택한 값이 표시됩니다.
            //MessageBox.Show("선택한 옵션은：" + comboBox2.Text);
            //if (Res == 0xB1) SH_period = 20;///적분 시간 t_int = SH_period / 2 MHz = 10 μs */
            //else if (Res == 0xB2) SH_period = 40;///적분 시간 t_int = SH_period / 2 MHz = 20 μs */
            //else if (Res == 0xB3) SH_period = 100;///적분 시간 t_int = SH_period / 2 MHz = 50 μs */
            //else if (Res == 0xB4) SH_period = 120;///적분 시간 t_int = SH_period / 2 MHz = 60 μs */
            //else if (Res == 0xB5) SH_period = 150;///적분 시간 t_int = SH_period / 2 MHz = 75 μs */
            //else if (Res == 0xB6) SH_period = 200;///적분 시간 t_int = SH_period / 2 MHz = 100 μs */
            //else if (Res == 0xB7) SH_period = 1000;///적분 시간 t_int = SH_period / 2 MHz = 500 μs */
            //else if (Res == 0xB8) SH_period = 2500;///적분 시간 t_int = SH_period / 2 MHz = 1.25 ms */
            //else if (Res == 0xB9) SH_period = 5000;///적분 시간 t_int = SH_period / 2 MHz = 2.5 ms */	
            //else if (Res == 0xBA) SH_period = 15000;///적분 시간 t_int = SH_period / 2 MHz = 7.5 ms */

            byte[] data = new byte[1];
            if(comboBox2.SelectedIndex == 0) data[0] = 0xB1;
            else if (comboBox2.SelectedIndex == 1) data[0] = 0xB2;
            else if (comboBox2.SelectedIndex == 2) data[0] = 0xB3;
            else if (comboBox2.SelectedIndex == 3) data[0] = 0xB4;
            else if (comboBox2.SelectedIndex == 4) data[0] = 0xB5;
            else if (comboBox2.SelectedIndex == 5) data[0] = 0xB6;
            else if (comboBox2.SelectedIndex == 6) data[0] = 0xB7;
            else if (comboBox2.SelectedIndex == 7) data[0] = 0xB8;
            else if (comboBox2.SelectedIndex == 8) data[0] = 0xB9;
            else if (comboBox2.SelectedIndex == 9) data[0] = 0xBA;
            if (serialPort1.IsOpen == true)
            {
                port_DataWrite(data);
                data_buffer.Clear();////포트에서 모든 데이터 지우기
            }
            else
            {
                MessageBox.Show("직렬 포트를 열지 못했습니다. 직렬 포트를 확인하십시오.", "에러");
                ///this.comboBox2.SelectedIndex = 1;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
            //FileStream fs = new FileStream("E:\\dat.txt", FileMode.Create);
            FileStream fs = new FileStream(textBox1.Text, FileMode.Create);
            
            //바이트 배열 가져오기
            string result = string.Join("\r\n", data_temp);
            byte[] data = System.Text.Encoding.Default.GetBytes(result);
            fs.Write(data, 0, data.Length);
            
            //버퍼 플러시, 스트림 닫기
            fs.Flush();
            fs.Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {

                int num11 = Convert.ToInt32(textBox2.Text);
                //타이머 Tick 이벤트가 1초마다 호출되도록 설정
                timer2.Interval = num11;
                
                timer2.Start();//예약 획득 시작 버튼
            }
            else
            {
                timer2.Stop();//예약 획득 시작 버튼
            }
                
        }


        //전환되는 타이머를 트리거하는 이벤트
        private void timer2_Tick(object sender, EventArgs e)
        {
            start_DataWrite();//캡처 시작 버튼
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            int num11 = Convert.ToInt32(textBox2.Text);
            //타이머 Tick 이벤트가 1초마다 호출되도록 설정
            timer2.Interval = num11;
        }
    }
}
