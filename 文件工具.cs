using System.Diagnostics;

namespace MHTools
{
    public abstract class FileControl
    {
        public string? SelectedFile { get; protected set; }
        public virtual string[] Files => Directory
            .GetFiles(WorkDir)
            .OrderBy(f => new FileInfo(f).CreationTime)
            .ToArray();
        public string DefaultFileName { get; set; } = "新文件";
        public string DefaultFileType { get; set; } = "txt";
        internal string WorkDir { get; set; }
        protected FileControl(string workDir)
        {
            WorkDir = workDir;
            Directory.CreateDirectory(WorkDir);
        }
    }

    public class SimpleFileManager : FileControl
    {
        public IEnumerable<string> Names => Files
            .ToList()
            .Select(f => Path.GetFileNameWithoutExtension(f));

        public SimpleFileManager(string targetDir, string? newfileName = default, string? newfileType = default) : base(targetDir)
        {
            DefaultFileName = newfileName ?? DefaultFileName;
            DefaultFileType = newfileType ?? DefaultFileType;
        }

        public bool Select(int index)
        {
            if (index > -1)
            {
                SelectedFile = Files[index];
                return true;
            }
            return false;
        }

        public bool Select(string? name)
        {
            if (name == null)
                return false;
            return Select(Files.ToList().FindIndex(f => Path.GetFileNameWithoutExtension(f) == name));
        }

        public void CancelSelect()
        {
            SelectedFile = null;
        }

        public string NextFileName()
        {
            var files = Files;
            int number = 1;
            string fileName;
            do
            {
                fileName = DefaultFileName + number++;
            }
            while (Array.Exists(files, file => Path.GetFileNameWithoutExtension(file) == fileName));
            return fileName;
        }

        public string Add()
        {
            var fileName = Path.Combine(WorkDir, $"{NextFileName()}.{DefaultFileType}");
            File.Create(fileName).Close();
            return fileName;
        }

        public void Delete()
        {
            if (File.Exists(SelectedFile))
            {
                File.Delete(SelectedFile);
                SelectedFile = null;
            }
        }

        public bool TryRename(string newName)
        {
            if (SelectedFile == null || string.IsNullOrWhiteSpace(newName))
                return false;
            // 获取当前文件的扩展名
            string currentExtension = Path.GetExtension(SelectedFile)!;

            // 构造新的文件路径
            string newFilePath = Path.Combine(WorkDir, $"{newName}{currentExtension}");

            try
            {
                // 尝试重命名文件
                File.Move(SelectedFile, newFilePath, false);
                SelectedFile = newFilePath;
                return true;
            }
            catch (Exception ex)
            {
                // 处理异常，可以记录日志或者根据需要进行其他处理
                Debug.WriteLine(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// (单例) 集成式文件管理快速小工具
    /// </summary>
    sealed public class FileManagerHelper
    {
        private SimpleFileManager SF { get; init; }

        private static FileManagerHelper? instance;
        private FileManagerHelper(string workdir)
        {
            SF = new SimpleFileManager(workdir);
        }
        public static FileManagerHelper SetDir(string targetDir)
        {
            instance ??= new FileManagerHelper(targetDir);
            instance.SF.WorkDir = targetDir;
            return instance;
        }

        /// <summary>
        /// 将object表示的<b>文件绝对路径</b>还原为string
        /// </summary>
        /// <param name="fp"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string ToPath(object fp)
        {
            var path = fp.ToString();
            if (Path.IsPathRooted(path))
                return path!;
            throw new ArgumentException($"参数 {nameof(fp)} 错误，应为文件绝对路径");
        }

        /// <summary>
        /// 获取目标文件夹及其子文件夹内的所有文件
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static string[] GetAllFiles(string folderPath)
        {
            List<string> allFiles = [];

            // 获取当前文件夹中的所有文件
            string[] filesInCurrentFolder = Directory.GetFiles(folderPath);
            allFiles.AddRange(filesInCurrentFolder);

            // 获取当前文件夹中的所有子文件夹
            string[] subFolders = Directory.GetDirectories(folderPath);

            // 递归处理每个子文件夹
            foreach (string subFolder in subFolders)
                allFiles.AddRange(GetAllFiles(subFolder));

            return [.. allFiles];
        }

        /// <summary>
        /// 将目标字符串转换为符合文件名规范的形式
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string SanitizeFileName(string input)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // 替换不符合文件名规范的字符为下划线
            foreach (char invalidChar in invalidChars)
            {
                input = input.Replace(invalidChar, '_');
            }

            return input;
        }

        //=================================
        //========SimpleFileManager========
        //=================================

        public SimpleFileManager ToSimpleFileManager() => SF;

        /// <summary>
        /// 通过无后缀文件名找到文件的全名
        /// </summary>
        /// <param name="nameNoEx"></param>
        /// <returns>文件的绝对路径</returns>
        public string? Find(string? nameNoEx)
        {
            return SF.Files.ToList().Find(x => Path.GetFileNameWithoutExtension(x) == nameNoEx);
        }
        public string NextName(string defaultName = "")
        {
            SF.DefaultFileName = defaultName;
            return SF.NextFileName();
        }
    }
}
