using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kyc_fileCounter.Class
{
    internal class Log
    {
        private static void InitLogFolder()
        {
            if (!System.IO.Directory.Exists("Logs"))
                System.IO.Directory.CreateDirectory("Logs");
            if (!System.IO.Directory.Exists(@"Logs\" + Utilities.SystemDate.ToString("yyyy-MM-dd")))
                System.IO.Directory.CreateDirectory(@"Logs\" + Utilities.SystemDate.ToString("yyyy-MM-dd"));
        }

        public static void SaveToErrorLog(string strData)
        {
            InitLogFolder();
            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(@"Logs\" + DateTime.Now.ToString("yyyy-MM-dd") + @"\Error.txt", true);
                sw.WriteLine(strData);
                sw.Dispose();
                sw.Close();
            }
            catch (Exception ex)
            {
            }
        }

        public static void SaveToSystemLog(string strData)
        {
            InitLogFolder();
            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(@"Logs\" + DateTime.Now.ToString("yyyy-MM-dd") + @"\System.txt", true);
                sw.WriteLine(strData);
                sw.Dispose();
                sw.Close();
            }
            catch (Exception ex)
            {
            }
        }

        public static void SaveToDoneIDs(string strData, DateTime dtmReportDate)
        {
            InitLogFolder();
            try
            {
                System.IO.StreamWriter sw = new System.IO.StreamWriter(Utilities.DoneIDsFile(dtmReportDate), true);
                sw.Write(strData);
                sw.Dispose();
                sw.Close();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
