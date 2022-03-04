using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;


namespace GTAmanager
{
    public partial class MainWindow : Window
    {
        private const string PROCESS_NAME = "GTA5";
        private const int IDLE_TIMER_SEC = 10;

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        public MainWindow()
        {
            InitializeComponent();
        }

        void CreateSoloLobby(object sender, RoutedEventArgs e)
        {
            bool suspended = SuspendProcess();

            if (suspended)
            {
                HandleSuspendGui(true, true);

                Task.Delay(IDLE_TIMER_SEC * 1000).ContinueWith(t =>
                {
                    ResumeProcess();
                    this.Dispatcher.Invoke(() =>
                    {
                        HandleSuspendGui(false);
                    });
                });
            }
            else
            {
                btnSoloLobby.IsEnabled = true;
            }
        }
        void SuspendClicked(object sender, RoutedEventArgs e)
        {
            bool result = SuspendProcess();

            if (result)
            {
                HandleSuspendGui(true);
            }
        }
        void ResumeClicked(object sender, RoutedEventArgs e)
        {
            bool result = ResumeProcess();

            if (result)
            {
                HandleSuspendGui(false);
            }
        }

        void KillGtaSwitchClicked(object sender, RoutedEventArgs e)
        {
             btnKillGta.IsEnabled = cbKillGta.IsChecked == true;
        }
        
        void KillGtaClicked(object sender, RoutedEventArgs e)
        {
            var processes = Process.GetProcessesByName(PROCESS_NAME);
            var process = processes.FirstOrDefault();

            if (process == null)
            {
                AddLog("Process not found");
            }
            else
            {
                process.Kill();
                AddLog("Process killed");
                btnKillGta.IsEnabled = false;
                cbKillGta.IsChecked = false;
            }
        }

        private void HandleSuspendGui(bool isSuspended, bool isAutoSuspend = false)
        {
            if (isSuspended)
            {
                btnSoloLobby.IsEnabled = false;

                if (isAutoSuspend)
                {
                    ProgressBarSoloLobby.Visibility = Visibility.Visible;
                    btnSuspend.IsEnabled = false;
                }
            }
            else
            {
                btnSoloLobby.IsEnabled = true;
                btnSuspend.IsEnabled = true;
                ProgressBarSoloLobby.Visibility = Visibility.Collapsed;
            }
        }

        private bool SuspendProcess()
        {
            var processes = Process.GetProcessesByName(PROCESS_NAME);
            var process = processes.FirstOrDefault();

            if (process == null)
            {
                AddLog("Process not found");
                return false;
            }

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);

                CloseHandle(pOpenThread);
            }

            AddLog("Process suspended");
            return true;
        }
        public bool ResumeProcess()
        {
            var processes = Process.GetProcessesByName(PROCESS_NAME);
            var process = processes.FirstOrDefault();

            if (process == null)
            {
                AddLog("Process not found");
                return false;
            }

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }

            AddLog("Process resumed");
            return true;
        }

        private void AddLog(string text)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
            {
                tbLog.Text = text + '\n' + tbLog.Text;
            }));
        }
    }
}
