using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MHTools.MoniWorld
{
    public static partial class Emulator
    {
        public class EmulatorItem
        {
            public string ID { get; set; }
            public string State { get; set; }
            public string Type { get; set; }
            public int[] Size { get; set; }

            public override string ToString()
            {
                string min, max;
                try
                {
                    min = Size.Min().ToString();
                    max = Size.Max().ToString();
                }
                catch (Exception)
                {
                    min = "error";
                    max = "error";
                    Size = [-1, -1];
                }

                return $"{Type} ({ID}) [{min}x{max}]";
            }
        }

        public enum Type
        {
            MuMu = 16384,
        }

        public static string[] GetPotentialDevices()
        {
            List<string> others = [];
            var emS = Enum.GetValues<Type>();
            foreach (var em in emS)
            {
                var start = (int)em;
                while (true)
                {
                    if (CheckConnection(port: start))
                    {
                        others.Add($"127.0.0.1:{start++}");
                        continue;
                    }
                    break;
                }
            }

            return [.. others];
        }

        public static string TypePredict(string id)
        {
            if (雷电Regex().IsMatch(id))
                return "雷电模拟器";
            else
            {
                var m = PortRegex().Match(id);
                if (m.Success)
                {
                    int port = int.Parse(m.Value);
                    var all = Enum.GetValues<Type>();

                    foreach (var em in all)
                    {
                        int v = (int)em;
                        if (port >= v && port < (v + 1000))
                            return em.ToString();
                    }
                }
            }
            return "未知设备";
        }

        private static bool CheckConnection(string ip = "127.0.0.1", int port = 0)
        {
            try
            {
                Process process = new();

                // 配置 ProcessStartInfo 对象
                ProcessStartInfo processStartInfo = new()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C netstat -an | findstr \"LISTENING\" | findstr \"{ip}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.StartInfo = processStartInfo;

                // 启动进程
                process.Start();

                // 读取并输出命令执行的输出流
                string output = process.StandardOutput.ReadToEnd();

                // 等待命令执行完成
                process.WaitForExit();

                // 关闭进程
                process.Close();

                return Regex.Match(output, $@"{Regex.Escape(ip)}:{port}").Success;
            }
            catch (Exception ex)
            {
                // 处理异常，例如输出到日志或返回 false
                Trace.WriteLine($"Error checking connection: {ex.Message}");
                return false;
            }
        }

        [GeneratedRegex("^emulator-55")]
        private static partial Regex 雷电Regex();
        [GeneratedRegex("(?<=:)\\d+$")]
        private static partial Regex PortRegex();
    }
}
