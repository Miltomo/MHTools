using System.Diagnostics;

namespace MHTools.MoniWorld
{
    /// <summary>
    /// (单例) ADB程序助手类：创建、管理、调用、控制
    /// </summary>
    public partial class AdbHelper
    {
        /// <summary>
        /// 输出结果
        /// </summary>
        public string Result { get; private set; } = string.Empty;

        /// <summary>
        /// 操作设备名
        /// </summary>
        public string EmulatorName { get; set; } = string.Empty;
        private ProcessStartInfo PSI { get; init; }

        private static AdbHelper? _instance;
        public static AdbHelper Instance
        {
            get
            {
                _instance ??= new AdbHelper();
                return _instance;
            }
        }

        private AdbHelper()
        {
            PSI = new()
            {
                FileName = "adb",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        private bool Execute()
        {
            try
            {
                // 创建一个进程对象
                Process process = new()
                {
                    StartInfo = PSI
                };

                // 启动进程
                process.Start();

                // 从进程读取输出
                Result = process.StandardOutput.ReadToEnd();

                // 等待进程完成
                process.WaitForExit();

                Debug.WriteLine(Result);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("发生异常：" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 设置ADB程序所在目录
        /// </summary>
        /// <param name="path"></param>
        public static void SetProgramPath(string path)
        {
            Instance.PSI.FileName = path;
        }

        /// <summary>
        /// 设置工作目录/输出目录
        /// </summary>
        /// <param name="dir"></param>
        public static void SetWorkDir(string dir)
        {
            Instance.PSI.WorkingDirectory = dir;
        }

        /// <summary>
        /// 强制结束所有正在运行的ADB进程；这有助于减少offline异常
        /// </summary>
        public static void KillAll()
        {
            try
            {
                // 获取所有同名进程
                Process[] processes = Process.GetProcessesByName("adb");

                // 结束每一个同名进程
                foreach (Process process in processes)
                {
                    process.Kill();
                    Debug.WriteLine($"进程 {process.ProcessName} 已结束");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"结束进程时出错：{ex.Message}");
            }
        }
    }
}
