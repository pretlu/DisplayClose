using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DisplayClose
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            startTime = DateTime.Now;
            comboBox1.SelectedIndex = 4;
            Start();
        }

        //定义一个热键的名字
        const int ONEKEY = 1;
        readonly DateTime startTime;
        DateTime lastCloseTime;


        ///
        /// 监视Windows消息
        /// 重载WndProc方法，用于实现热键响应
        ///
        ///
        private int i = 0;
        private bool closekey = false;
        protected override void WndProc(ref Message m)
        {
            i++;
            // Debug.WriteLine("进入到了监视"+i);

            const int WM_HOTKEY = 0x0312;
            //按快捷键
            switch (m.Msg)
            {

                case WM_HOTKEY:
                    switch (m.WParam.ToInt32())
                    {
                        case ONEKEY:
                            //此处填写快捷键响应代码
                            Debug.WriteLine("你按下了我" + i);
                            closekey = true;
                            break;
                        case 2:     //按其它键
                                    //此处填写快捷键响应代码
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }
        #region SendMessage
        /// <summary>
        /// 关闭显示器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseLCD()
        {
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MONITORPOWER, 2);    // 2 为关闭显示器， －1则打开显示器
            lastCloseTime = DateTime.Now;
            isClose = true;
            lastStapClose = true;
            Console.WriteLine("已关闭屏幕");


        }

        public const uint WM_SYSCOMMAND = 0x0112;
        public const uint SC_MONITORPOWER = 0xF170;
        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, uint wParam, int lParam);
        #endregion

        private int count = 0;
        private bool closeing = false;
        /// <summary>
        /// 用定时器优化关闭显示器的时间
        /// 当用户按下快捷键时，前后给一点延时，以防响应时就被唤醒
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        ///
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (closekey)
            {
                closekey = false;
                closeing = true;
                if (count < 10)
                {
                    count = 0;
                }

            }
            //一秒后关闭
            if (closeing)
            {
                count++;
                if (count == 5)
                {
                    Debug.WriteLine("关闭显示器");
                    CloseLCD();
                    countManual++;

                }
                else if (count > 20)
                {
                    count = 0;
                    closeing = false;
                    Debug.WriteLine("关闭显示器完成");
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CloseLCD();
            countManual++;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Start();
        }

        /// <summary>
        /// 开始注册快捷键
        /// </summary>
        public void Start()
        {
            if (checkBox1.Checked)
            {
                bool v = HotKey.RegisterHotKey(Handle, ONEKEY, HotKey.KeyModifiers.WindowsKey | HotKey.KeyModifiers.Ctrl, Keys.Z);
                if (v == false)
                {
                    MessageBox.Show("快捷键已被占用！注册失败！");

                }
                else
                {
                    // MessageBox.Show("快捷键注册成功");
                }
            }
            else
            {
                bool b = HotKey.UnregisterHotKey(Handle, ONEKEY);

                if (b)
                {
                    MessageBox.Show("快捷键解除成功！");
                }
                else
                {
                    MessageBox.Show("快捷键解除失败！");
                }
            }
        }

        //存放选择项对应的时间
        long[] selectTime = { 1, 2, 3, 5, 10, 15, 20, 30, 45, 60, 120, 180, 300, -1 };
        TimeSpan runTime;//总运行时间

        bool isClose = false;//是否关闭了屏幕

        private long lastTime;
        private bool lastStapClose;//记录上一步是否是关闭屏幕
        private int countAuto = 0;
        private int countManual = 0;
        private int flagYiwai = 0;
        private int countYiwai = 0;

        /// <summary>
        /// 时间统计
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer2_Tick(object sender, EventArgs e)
        {
            //计算本程序总运行时间
            //Console.WriteLine(startTime);
            runTime = DateTime.Now - startTime;

            //计算剩余的关屏时间
            //Console.WriteLine(runTime);
            if (selectTime[comboBox1.SelectedIndex] != -1)
            {
                long noActiva = ActiveUser.GetLastInputTime();

                long elapsed = selectTime[comboBox1.SelectedIndex] * 60 - noActiva;

                if (elapsed <= 0)//已达到倒计时
                {
                    //当屏没关时才关
                    if (!isClose)
                    {
                        //自动关闭屏幕                    
                        countAuto++;
                        CloseLCD();
                    }

                }
                else
                {
                    Debug.WriteLine("意外a activa：" + noActiva + " flagyiwai:" + flagYiwai);
 
                    if (noActiva == 0)//用户在活动
                    {
                        //唤醒了显示器
                        if (isClose && !lastStapClose)
                        {
                            Debug.WriteLine("估计被唤醒了");
                            isClose = false;
                            flagYiwai = 1;
                        }
                        else
                        {
                            flagYiwai = 0;
                        }   
                    }
                    else
                    {
                        Debug.WriteLine("意外b activa：" + noActiva + " flagyiwai:" + flagYiwai + " lastTime:" + lastTime);
                        //1是否为意外唤醒验证
                        if (flagYiwai != 0 && noActiva > lastTime)
                        {
                            flagYiwai++;

                            //2满足意外唤醒条件:10秒后无操作
                            if (flagYiwai > 10)
                            {
                                flagYiwai = 0;
                                countYiwai++;
                                if (checkBox1.Checked)
                                {
                                    //自动关闭屏幕                    
                                    countAuto++;
                                    CloseLCD();
                                }
                            }
                        }
                        else
                        {
                            flagYiwai = 0;
                        }


                    }
                }
                lastTime = noActiva;
                lastStapClose = false;
                Console.WriteLine(DateTime.Now+"----"+elapsed);
                label2.Text = "主动关屏: " + countManual + "  意外唤醒: " + countYiwai + "  自动关屏: " + countAuto + "  关屏倒计时: " + elapsed;

            }
            else
            {
                label2.Text = "主动关屏: " + countManual + "  意外唤醒: " + countYiwai;
            }
            label3.Text = "已运行: " + runTime + "  上次关屏: " + lastCloseTime;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.WriteLine(comboBox1.SelectedIndex);
            Console.WriteLine(selectTime[comboBox1.SelectedIndex]);
            if (selectTime[comboBox1.SelectedIndex] == -1)
            {
                checkBox2.Enabled = false;

            }else
            {
                checkBox2.Enabled = true;
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {

        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 退出，关闭所有的线程
            notifyIcon1.Visible = false;//设置图标不可见  
            this.Dispose();//关闭窗体  
            this.Close();//释放资源  
            Application.Exit(); //关闭应用程序窗体  
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //关闭时隐藏
            e.Cancel = true;
            this.Visible = false;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //鼠标左键时发生
                this.Show();                                //窗体显示  
                this.WindowState = FormWindowState.Normal;  //窗体状态默认大小
            }
        }

        private void notifyIcon1_MouseMove(object sender, MouseEventArgs e)
        {
            notifyIcon1.Text = "只关显示器";
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
