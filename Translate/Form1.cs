using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Translate
{

    public partial class Form1 : Form
    {
        [DllImport("User32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;
        public Form1()
        {
            RegistryKey add = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            add.SetValue("TranslateJP-VN", Application.StartupPath + @"\" + Process.GetCurrentProcess().ProcessName + ".exe");
            if (add.GetValue("TranslateJP-VN") == null) add.SetValue("TranslateJP-VN", Application.StartupPath + @"\" + Process.GetCurrentProcess().ProcessName + ".exe");
            InitializeComponent();
            trackBar1.Value = 5;
            trackBar1.Minimum = 3;
            this.Opacity = trackBar1.Value / 10.0;
            var HotKeyManager = new HotkeyManager();
            //RegisterHotKey (Hangle, Hotkey Identifier, Modifiers, Key)
            //RegisterHotKey(HotKeyManager.Handle, 123, Constants.ALT + Constants.SHIFT, (int)Keys.P);
            RegisterHotKey(HotKeyManager.Handle, 234, Constants.CTRL, (int)Keys.Q);
            this.WindowState = FormWindowState.Normal;
        }
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (message.Msg == WM_NCHITTEST && (int)message.Result == HTCLIENT)
                message.Result = (IntPtr)HTCAPTION;
        }
        //This class is not required but makes managing the modifiers easier.
        public static class Constants
        {
            public const int NOMOD = 0x0000;
            public const int ALT = 0x0001;
            public const int CTRL = 0x0002;
            public const int SHIFT = 0x0004;
            public const int WIN = 0x0008;
            public const int WM_HOTKEY_MSG_ID = 0x0312;
        }

        public sealed class HotkeyManager : NativeWindow, IDisposable
        {
            public HotkeyManager()
            {
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
                {
                    if (m.WParam.ToInt32() == 234)
                    {
                        if (Clipboard.ContainsText(TextDataFormat.Text))
                        {
                            string text = Clipboard.GetText(TextDataFormat.Text);
                            loadData(text);
                        }
                    }
                }
                base.WndProc(ref m);
            }

            public static void loadData(string text)
            {
                panel1.Controls.Clear();
                Label title = new Label();
                string regExp = @"[^\w\d]";
                string tmp = Regex.Replace(text, regExp, "");
                string URL = "http://mazii.net/api/search/" + tmp + "/20/1";
                using (var webClient = new WebClient { Encoding = System.Text.Encoding.UTF8 })
                {
                    var json = webClient.DownloadString(URL);
                    dynamic result = JsonConvert.DeserializeObject<dynamic>(json);
                    if (result.status == "200")
                    {
                        title.Text = result.data[0].word + " (" + result.data[0].phonetic + ")\n\n";
                        title.Font = new Font("MS UI Gothic", 12, FontStyle.Bold);
                        title.ForeColor = Color.Red;
                        title.AutoSize = true;
                        panel1.Controls.Add(title);
                        int i = 1;
                        foreach (var item in result.data[0].means)
                        {
                            dynamic jsonSerial = JsonConvert.SerializeObject(item);
                            dynamic result3 = JsonConvert.DeserializeObject<dynamic>(jsonSerial);
                            var l1 = new Label
                            {
                                Text = "◆ " + result3.mean + "\n\n",
                                Font = new Font("MS UI Gothic", 12, FontStyle.Regular),
                                Location = new Point(0, 32 * i),
                                AutoSize = true,
                                ForeColor = Color.Green
                            };

                            if (result3.examples != null)
                            {
                                dynamic jsonSerial4 = JsonConvert.SerializeObject(result3.examples);
                                dynamic result4 = JsonConvert.DeserializeObject<dynamic>(jsonSerial4);
                                int j = 1;
                                foreach (var item4 in result4)
                                {
                                    var l2 = new Label
                                    {
                                        Text = "    ● " + item4.content + "\n\n       " + item4.transcription + "\n        " + item4.mean + "\n\n",
                                        AutoSize = true,
                                        Font = new Font("MS UI Gothic", 10, FontStyle.Regular),
                                        Location = new Point(0, l1.Location.Y + 70 * j - 20)
                                    };
                                    panel1.Controls.Add(l2);
                                    j++;
                                }
                            }
                            panel1.Controls.Add(l1);
                            i++;
                        }
                    }
                    else title.Text = "Not Found!!!";
                    panel1.Controls.Add(title);
                }
            }

            public void Dispose()
            {
                DestroyHandle();
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            this.Opacity = trackBar1.Value / 10.0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("アプリケーションを終了しますか？", "閉じる", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (textBox1.Text != "")
                {
                    HotkeyManager.loadData(textBox1.Text);
                }
            }
        }
    }
}