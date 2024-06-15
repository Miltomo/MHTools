using System.Text;

namespace MHTools
{
    public static class TimeTool
    {
        public static string FormatMS(int milliseconds, bool inEnglish = false)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);

            int hours = timeSpan.Hours;
            int minutes = timeSpan.Minutes;
            int seconds = timeSpan.Seconds;

            string f = $"{seconds}{(inEnglish ? "s" : "秒")}";
            if (minutes > 0)
                f = $"{minutes}{(inEnglish ? "m" : "分")}" + f;
            if (hours > 0)
                f = $"{hours}{(inEnglish ? "h" : "时")}" + f;

            return f;
        }

        public static string RandomTime()
        {
            var time = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            return time + "_" + RandomLetters(5);
        }

        public static string RandomLetters(int length = 5)
        {
            if (length < 1)
            {
                throw new ArgumentException("Length should be greater than 0.");
            }

            Random random = new();
            StringBuilder stringBuilder = new(length);

            for (int i = 0; i < length; i++)
            {
                // 随机选择大小写字母的 ASCII 码值范围
                int asciiValue = random.Next(2) == 0 ? random.Next(65, 91) : random.Next(97, 123);
                char randomChar = (char)asciiValue;

                stringBuilder.Append(randomChar);
            }

            return stringBuilder.ToString();
        }
    }
}
