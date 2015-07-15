using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MccDaq;

namespace Read_Thermocouple_Set_Temp
{
    public partial class Form1 : Form
    {
        public const int BLOCKSIZE = 10;
        public const int NUM_CHANNELS = 8;
        public const string DEVICE = "2408";
        bool temp_Read = false;
        public Form1()
        {
            InitializeComponent();
            stop_button.Enabled = false;
        }

        private void start_button_Click(object sender, EventArgs e)
        {
            temp_Read = true;
            backgroundWorker1.RunWorkerAsync();
            start_button.Enabled = false;
            stop_button.Enabled = true;
            desiredTemp.Enabled = false;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            MccDaq.ErrorInfo RetVal;
            int BoardNum = 0;

            //locate the USB-TC
            BoardNum = GetBoardNum(DEVICE);

            if (BoardNum == -1)
            {
                MessageBox.Show("No USB-{0} detected!", DEVICE);
                return; //exit program
            }
            else
            {
                MccBoard daq = new MccDaq.MccBoard(BoardNum);
                float setTemp = (float)Convert.ToDouble(desiredTemp.Text);
                bool targetTemp = false;
                int allSamples = 0;
                float[] tempData = new float[NUM_CHANNELS];
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                string[] filePath = new string[NUM_CHANNELS];
                StringBuilder[] fileString = new StringBuilder[NUM_CHANNELS];
                for (int j = 0; j < NUM_CHANNELS; j++)
                {
                    string fileDateTime = string.Format("_{0:yyyy-MM-dd_hh-mm}",
        DateTime.Now);
                    filePath[j] = "Channel" + j + fileDateTime + ".csv";
                    fileString[j] = new StringBuilder();
                    fileString[j].AppendLine(string.Format("Channel{0}", j));
                    fileString[j].AppendLine(string.Format("Time,Elapsed Time,Temperature (C)"));

                }
                while (temp_Read == true && targetTemp == false)
                {
                    for (int j = 0; j < NUM_CHANNELS; j++)
                    {
                        RetVal = daq.TIn(j, TempScale.Celsius, out tempData[j], ThermocoupleOptions.Filter);
                        IsError(RetVal);
                        // Get the elapsed time as a TimeSpan value.
                        TimeSpan ts = stopWatch.Elapsed;
                        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
                        fileString[j].AppendLine(string.Format("{0},{1},{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), elapsedTime, tempData[j].ToString("0.000").PadLeft(10)));
                        Console.WriteLine("The temperature is on channel {0}" + " is: {1} " + "at elasped time {2} current time {3} ", j, tempData[j].ToString("0.000").PadLeft(10), elapsedTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    }
                    for (int j = 0; j < NUM_CHANNELS; j++)
                    {
                        if ((float)tempData[j] >= setTemp)
                        {
                            allSamples++;
                        }
                    }
                    this.Invoke((MethodInvoker)delegate()
                    {
                        this.textBox1.Text = tempData[0].ToString("0.000");
                        this.textBox2.Text = tempData[1].ToString("0.000");
                        this.textBox3.Text = tempData[2].ToString("0.000");
                        this.textBox4.Text = tempData[3].ToString("0.000");
                        this.textBox5.Text = tempData[4].ToString("0.000");
                        this.textBox6.Text = tempData[5].ToString("0.000");
                        this.textBox7.Text = tempData[6].ToString("0.000");
                        this.textBox8.Text = tempData[7].ToString("0.000");
                    });
                    if (allSamples == NUM_CHANNELS)
                    {

                        targetTemp = true;
                        this.Invoke((MethodInvoker)delegate()
                        {
                            this.start_button.Enabled = true;
                            this.desiredTemp.Enabled = true;
                            this.stop_button.Enabled = false;
                        });
                    }
                    else
                    {
                        allSamples = 0;
                    }
                    //System.Threading.Thread.Sleep(0); //max rate is 2Hz or 500mS per read.
                }
                for (int j = 0; j < NUM_CHANNELS; j++)
                {
                    File.WriteAllText(filePath[j].ToString(), fileString[j].ToString());
                }
            }
        }

        /************************************************************************/
        public static int GetBoardNum(string dev)
        {
            for (int BoardNum = 0; BoardNum < 99; BoardNum++)
            {
                MccDaq.MccBoard daq = new MccDaq.MccBoard(BoardNum);
                if (daq.BoardName.Contains(dev))
                {
                    Console.WriteLine("USB-{0} board number = {1}", dev, BoardNum.ToString());
                    daq.FlashLED();
                    return BoardNum;
                }
            }
            return -1;
        }
        /************************************************************************/
        /************************************************************************/

        public static int IsError(ErrorInfo e)
        {
            if (e.Value != 0)
            {
                MessageBox.Show(e.Message);
                return 1;
            }
            return 0;
        }

        private void stop_button_Click(object sender, EventArgs e)
        {
            temp_Read = false;
            this.backgroundWorker1.CancelAsync();
            start_button.Enabled = true;
            desiredTemp.Enabled = true;
            stop_button.Enabled = false;

        }

        /************************************************************************/
    }
}
