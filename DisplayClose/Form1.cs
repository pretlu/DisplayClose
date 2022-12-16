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

namespace DisplayClose {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
         //   startTime = DateTime.Now;
            Start();
            AutoLocation();
        }
        private void Form1_Load(object sender, EventArgs e) {
            //读用户配置
            comboBox1.SelectedIndex = Properties.Settings.Default.selectIndex;
            checkBox1.Checked = Properties.Settings.Default.isHotkey;
            checkBox2.Checked = Properties.Settings.Default.isAccident;
            comboBox2.SelectedIndex = Properties.Settings.Default.selectIndexS;

            sleepTimeTotal = Properties.Settings.Default.sleepTotal;
            closeTimeTotal = Properties.Settings.Default.closeTotal;

        }
        /// <summary>
        /// 自动调整位置
        /// 避免UI缩放导致显示不全
        /// </summary>
        private void AutoLocation() {
            // throw new NotImplementedException();
            //   label2.Top = label1.Height+ label1.Height/2;

        }

        //定义一个热键的名字
        const int ONEKEY = 1;



        ///
        /// 监视Windows消息
        /// 重载WndProc方法，用于实现热键响应
        ///
        ///
        private int i = 0;
        private bool closekey = false;
        protected override void WndProc(ref Message m) {
            i++;
            // Debug.WriteLine("进入到了监视"+i);

            const int WM_HOTKEY = 0x0312;
            //按快捷键
            switch (m.Msg) {

                case WM_HOTKEY:
                    switch (m.WParam.ToInt32()) {
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
        private void timer1_Tick(object sender, EventArgs e) {
            if (closekey) {
                closekey = false;
                closeing = true;
                if (count < 10) {
                    count = 0;
                }

            }
            //一秒后关闭
            if (closeing) {
                count++;
                if (count == 5) {
                    Debug.WriteLine("关闭显示器");
                    CloseLCD();
                    countManual++;

                } else if (count > 20) {
                    count = 0;
                    closeing = false;
                    Debug.WriteLine("关闭显示器完成");
                }
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            CloseLCD();
            countManual++;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {
            Start();
            ////写用户配置
            Properties.Settings.Default.isHotkey = checkBox1.Checked;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 开始注册快捷键
        /// </summary>
        public void Start() {
            if (checkBox1.Checked) {
                bool v = HotKey.RegisterHotKey(Handle, ONEKEY, HotKey.KeyModifiers.WindowsKey | HotKey.KeyModifiers.Ctrl, Keys.Z);
                if (v == false) {
                    MessageBox.Show("快捷键已被占用！注册失败！");

                } else {
                    // MessageBox.Show("快捷键注册成功");
                }
            } else {
                bool b = HotKey.UnregisterHotKey(Handle, ONEKEY);

                if (b) {
                    //MessageBox.Show("快捷键解除成功！");
                } else {
                    MessageBox.Show("快捷键解除失败！");
                }
            }
        }



        /// <summary>
        /// 时间统计
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer2_Tick(object sender, EventArgs e) {
            Run();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            Debug.WriteLine(comboBox1.SelectedIndex);
            //Console.WriteLine(selectTime[comboBox1.SelectedIndex]);
            if (selectTime[comboBox1.SelectedIndex] == -1) {
                checkBox2.Enabled = false;

            } else {
                checkBox2.Enabled = true;
            }
            ////写用户配置
            Properties.Settings.Default.selectIndex = comboBox1.SelectedIndex;
            Properties.Settings.Default.Save();
        }


        //存放选择项对应的时间
        long[] selectTime = { 1, 2, 3, 5, 10, 15, 20, 30, 45, 60, 120, 180, 300, -1 };
        int[] selectTimeS = { 1, 2, 3, 5, 10, 15, 20, 30, 45, 60, 120, 180, 300, -1 };
        private int countAuto = 0;
        private int countManual = 0;

        long ontimeCloseLast = 0;
        long ontimeSleepLast = 0;
        bool checkAcd = false;
        bool isCloseLED = false;
        bool isCloseSleep = false;
        bool isCloseSleepLast = false;
        int countAcd = 0;

        readonly static DateTime startTime= DateTime.Now;
        DateTime closeTimeStar;
        TimeSpan closeTimeLast;
        TimeSpan closeTimeTotal;
        DateTime sleepTimeStar ;
        TimeSpan sleepTimeLast;
        TimeSpan sleepTimeTotal;



        /// <summary>
        /// 主业务
        /// </summary>
        private void Run() {
            //定义变量
            long activeLatest = ActiveUser.GetLastInputTime();//距上次活动间隔时间，秒
            long timeClose = selectTime[comboBox1.SelectedIndex] * 60;
            long timeSleep = selectTimeS[comboBox2.SelectedIndex] * 60;
            long ontimeClose = timeClose - activeLatest;
            long ontimeSleep = timeSleep - activeLatest;
            if (ontimeClose <= 0 && ontimeCloseLast > 0)//刚到关屏时间
            {
                Debug.WriteLine(DateTime.Now + "刚到设定时间，准备关屏");
                if (!isCloseSleep && !isCloseLED) {
                    CloseLCD();
                    countAuto++;
                }
            } else if (ontimeClose >= 0 && ontimeCloseLast < 0) {//刚到开屏时间
                Debug.WriteLine(DateTime.Now + "显示器已被唤醒");
                //统计时长
                Debug.WriteLine(DateTime.Now + "初始值" + sleepTimeStar);
                if (isCloseLED && !isCloseSleep) {//统计关屏时长
                    closeTimeLast = DateTime.Now - closeTimeStar;
                    closeTimeTotal += closeTimeLast;
                    Debug.WriteLine(DateTime.Now + "关屏用时" + closeTimeLast + "总时：" + closeTimeTotal);
                    //保存数据
                    Properties.Settings.Default.closeTotal= closeTimeTotal;
                    Properties.Settings.Default.Save();
                }
                if (isCloseSleep) {
                    sleepTimeLast = DateTime.Now - sleepTimeStar;
                    sleepTimeTotal += sleepTimeLast;
                    Debug.WriteLine(DateTime.Now + "睡眠用时" + sleepTimeLast + "总时：" + sleepTimeTotal);

                    //保存数据
                    Properties.Settings.Default.sleepTotal = sleepTimeTotal;
                    Properties.Settings.Default.Save();
                }


                //数据重置
                checkAcd = true;
                isCloseLED = false;
                isCloseSleepLast = isCloseSleep;
                isCloseSleep = false;



            } else {
                if (checkAcd)//检查是否为意外唤醒
                {
                    if (activeLatest == 0) {
                        checkAcd = false;
                        Debug.WriteLine(DateTime.Now + "意外唤醒判断：不是意外唤醒");
                    } else if (activeLatest > 20) {
                        checkAcd = false;
                        Debug.WriteLine(DateTime.Now + "意外唤醒判断：是意外唤醒，20秒内无操作。");
                        countAcd++;
                        if (Properties.Settings.Default.isAccident) {
                            if (isCloseSleepLast) {
                                CloseSleep();
                            } else {
                                CloseLCD();
                                countAuto++;
                            }
                        }
                    }
                }
            }

            if (ontimeSleep <= 0 && ontimeSleepLast > 0) {
                Debug.WriteLine(DateTime.Now + "刚到设定时间，准备睡眠");
                if (!isCloseSleep) {
                    if (isCloseLED) {//统计关屏时长
                        closeTimeLast = DateTime.Now - closeTimeStar;
                        closeTimeTotal += closeTimeLast;
                        Debug.WriteLine(DateTime.Now + "关屏用时" + closeTimeLast+"总时："+closeTimeTotal);
                        //保存数据
                        Properties.Settings.Default.closeTotal = closeTimeTotal;
                        Properties.Settings.Default.Save();
                    }
                    CloseSleep();
                }
            }
            ontimeCloseLast = ontimeClose;
            ontimeSleepLast = ontimeSleep;
            TimeSpan runTime = DateTime.Now - startTime;
            label3.Text = "本次运行: " + runTime.ToString(@"d\天hh\:mm\.ss") + "  上次关屏: " + closeTimeStar;
            label2.Text = "意外唤醒：" + countAcd + "  手动/自动关屏: " + countManual + "/" + countAuto + "  关屏倒计时: " + ontimeClose;
            label5.Text = "本次关屏时长: " + closeTimeLast.ToString(@"d\天hh\:mm\.ss") + " 总时长:" + closeTimeTotal.ToString(@"d\天hh\:mm\.ss");
            label6.Text = "本次睡眠时长: " + sleepTimeLast.ToString(@"d\天hh\:mm\.ss") + " 总时长:" + sleepTimeTotal.ToString(@"d\天hh\:mm\.ss");

        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e) {

            ////写用户配置
            Properties.Settings.Default.isAccident = checkBox2.Checked;
            Properties.Settings.Default.Save();
        }

        private void notifyIcon1_Click(object sender, EventArgs e) {

        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e) {
            // 退出，关闭所有的线程
            notifyIcon1.Visible = false;//设置图标不可见  
            this.Dispose();//关闭窗体  
            this.Close();//释放资源  
            Application.Exit(); //关闭应用程序窗体  
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            //关闭时隐藏
            e.Cancel = true;
            this.Visible = false;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e) {

            if (e.Button == MouseButtons.Left) {
                //鼠标左键时发生
                this.Show();                                //窗体显示  
                this.WindowState = FormWindowState.Normal;  //窗体状态默认大小
            }
        }

        private void notifyIcon1_MouseMove(object sender, MouseEventArgs e) {
            notifyIcon1.Text = "只关显示器";
        }

        private void label3_Click(object sender, EventArgs e) {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) {
            ////写用户配置
            Properties.Settings.Default.selectIndexS = comboBox2.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        //实现睡眠，代码如下：
        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        /// <summary>
        /// 系统睡眠
        /// </summary>
        private void CloseSleep() {
            isCloseSleep = true;
            sleepTimeStar= DateTime.Now;
            SetSuspendState(false, true, true);//睡眠
            Debug.WriteLine(DateTime.Now + "已睡眠");

        }

        /// <summary>
        /// 关闭显示器
        /// </summary>
        private void CloseLCD() {
            isCloseLED = true;
           SendMessage(this.Handle, WM_SYSCOMMAND, SC_MONITORPOWER, 2);    // 2 为关闭显示器， －1则打开显示器
            closeTimeStar = DateTime.Now;
            Debug.WriteLine(DateTime.Now + "已关屏");
        }

    }
}
