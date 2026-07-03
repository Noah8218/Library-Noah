using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace Lib.Common
{
    public class AppUtil
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SetSystemTime([In] SystemTime st);

        public struct SystemTime
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        /// <summary>
        /// 사용 예 => ParseEnum<(enum 타입)>(cbType.SelectedItem.ToString())
        /// </summary>
        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        /// <summary>
        /// 원본/대상 폴더의 파일들을 비교하여 데이터를 backup합니다.
        /// </summary>
        public static void SynchFolder(DirectoryInfo existingDir, DirectoryInfo copyDir)
        {
            try
            {
                FileInfo[] existingFiles = existingDir.GetFiles();
                FileInfo[] copyFiles = copyDir.GetFiles();

                bool findFile = false;
                int nIndex = 0;

                foreach (FileInfo existingFile in existingFiles)
                {
                    findFile = false;
                    nIndex = -1;
                    foreach (FileInfo copyFile in copyFiles)
                    {
                        nIndex++;

                        if (copyFile == null)
                        {
                            continue;
                        }

                        if (existingFile.Name == copyFile.Name)
                        {
                            findFile = true;

                            if (existingFile.LastWriteTime != copyFile.LastWriteTime)
                            {
                                try
                                {
                                    if (existingFile.LastWriteTime > copyFile.LastWriteTime)
                                    {
                                        File.Copy(existingFile.FullName, copyFile.FullName, true);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                copyFiles[nIndex] = null;

                                break;
                            }
                        }
                    }

                    if (!findFile)
                    {
                        try
                        {
                            string path = Path.Combine(copyDir.FullName, existingFile.Name);
                            existingFile.CopyTo(path);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                DirectoryInfo[] existingFolders = existingDir.GetDirectories();
                DirectoryInfo[] copyFolders = copyDir.GetDirectories();

                foreach (DirectoryInfo existingFolder in existingFolders)
                {
                    findFile = false;
                    nIndex = -1;

                    foreach (DirectoryInfo copyFolder in copyFolders)
                    {
                        nIndex++;

                        if (copyFolder == null)
                        {
                            continue;
                        }

                        if (existingFolder.Name == copyFolder.Name)
                        {
                            findFile = true;
                            SynchFolder(existingFolder, copyFolder);
                            copyFolders[nIndex] = null;
                        }
                    }

                    if (!findFile)
                    {
                        try
                        {
                            string path = Path.Combine(copyDir.FullName, existingFolder.Name);
                            Directory.CreateDirectory(path);
                            SynchFolder(existingFolder, new DirectoryInfo(path));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static bool InitDirectory(string strFolderName)
        {
            string strFolderPath = Path.Combine(AppContext.BaseDirectory, strFolderName);
            DirectoryInfo dirRecipe = new DirectoryInfo(strFolderPath);
            if (dirRecipe.Exists == false) dirRecipe.Create();

            return true;
        }

        public static double DrivePercent(string strTargetDriver, out double TotalSize, out double AvaliableSize)
        {
            double dPercent = 0;

            TotalSize = 0.0D;
            AvaliableSize = 0.0D;

            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.Name == strTargetDriver)
                    {
                        TotalSize = drive.TotalSize / 1000000.0D / 1024.0D;
                        AvaliableSize = drive.AvailableFreeSpace / 1000000.0D / 1024.0D;

                        double dUsedSize = (int)((drive.TotalSize - drive.AvailableFreeSpace) / 1000000 / 1024.0D);

                        dPercent = dUsedSize / TotalSize * 100.0D;
                    }
                }
            }
            catch (Exception)
            {
            }

            return dPercent;
        }

        public static string[] AvalibleComports()
        {
            return SerialPort.GetPortNames();
        }
    }
}
