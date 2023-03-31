using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Lib.Common
{
    public class CUtil
    {
        [DllImport("gdi32.dll")] public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);
        [DllImport("user32.dll")] public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

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
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static bool LoadFolderPath(out string strdirPath)
        {
            strdirPath = "";
            try
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        strdirPath = fbd.SelectedPath;
                    }
                }

                Debug.WriteLine($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");                
                return true;
            }
            catch (Exception Desc)
            {
                Debug.WriteLine($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");                
                return false;
            }
        }

        public static string LoadImageFilePath()
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = Application.StartupPath;
                ofd.Filter = "Images Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg;*.jpeg;*.gif;*.bmp;*.png";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string strFilePath = ofd.FileName;
                    Debug.WriteLine($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
                    return strFilePath;
                }
            }
            catch (Exception Desc)
            {
                Debug.WriteLine($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return "";
            }

            return "";
        }

        public static string[] LoadImagesFilePath()
        {
            string[] Images = null;
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = Application.StartupPath;
                ofd.Filter = "Images Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg;*.jpeg;*.gif;*.bmp;*.png";
                ofd.Multiselect = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Images = ofd.FileNames;
                    Debug.WriteLine($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
                    return Images;
                }
            }
            catch (Exception Desc)
            {
                Debug.WriteLine($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return Images;
            }

            return Images;
        }

        public static string SaveImageFilePath()
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = Application.StartupPath;
                sfd.Filter = "Images Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg;*.jpeg;*.gif;*.bmp;*.png";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string strFilePath = sfd.FileName;
                    Debug.WriteLine($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
                    return strFilePath;
                }
            }
            catch (Exception Desc)
            {
                Debug.WriteLine($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return "";
            }

            return "";
        }

        public static string LoadFilePath()
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = Application.StartupPath;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string strFilePath = ofd.FileName;
                    Debug.WriteLine($"[OK] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}");
                    return strFilePath;
                }
            }
            catch (Exception Desc)
            {
                Debug.WriteLine($"[FAILED] {MethodBase.GetCurrentMethod().ReflectedType.Name}==>{MethodBase.GetCurrentMethod().Name}   Execption ==> {Desc.Message}");
                return "";
            }

            return "";
        }

        public static bool OpenCheckForm(Form form)
        {
            try
            {
                foreach (Form frm in Application.OpenForms)
                {
                    if (frm.Name == form.Name)
                    {
                        frm.Activate();
                        return false;
                    }
                }
                return true;
            }
            catch { return false; }           
        }

        public static bool OpenCheckForm(string strFormName)
        {
            try
            {
                foreach (Form frm in Application.OpenForms)
                {
                    if (frm.Name == strFormName)
                    {
                        //frm.Activate();
                        return false;
                    }
                }
                return true;
            }
            catch { return false; }           
        }

        public static Control[] GetControlsWinform(Control con)
        {
            var conList = new List<Control>();

            foreach (Control control in con.Controls)
            {
                //컨트롤 속성으로 찾는 방법
                if (control is Button)
                {
                    int nSize = 30;

                    //15 로 전달 되어 있는 인자 -> 실제 모서리 둥글게 표현 하는 인자
                    IntPtr ip = CreateRoundRectRgn(0, 0, control.Width, control.Height, nSize, nSize);
                    SetWindowRgn(control.Handle, ip, true);
                    conList.Add(control);
                }
                ////컨트롤 이름으로 찾는 방법
                //if (control.Name == "그리드뷰")
                //    conList.Add(control);

                //주석
                if (control.Controls.Count > 0)
                    conList.AddRange(GetControlsWinform(control));
            }

            return conList.ToArray();
        }
    }
}
