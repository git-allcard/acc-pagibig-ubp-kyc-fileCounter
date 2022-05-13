using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kyc_fileCounter
{
    class Utilities
    {      
       
        public static DateTime SystemDate;
        public static DateTime ReportStartDate;
        public static DateTime ReportEndDate;
        public static string ConStr;
        public static string ConStrSys;

        public static string TimeStamp()
        {
            return DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") + " ";
        }   

        public static string DoneIDsFile(DateTime dtmReportDate)
        {
            return @"Logs\" + dtmReportDate.ToString("yyyy-MM-dd") + @"\doneIDs.txt";
        }

        public static string PreviousDoneIDsFile()
        {
            DateTime prevDate = SystemDate.AddDays(-1);
            return @"Logs\" + prevDate.ToString("yyyy-MM-dd") + @"\doneIDs.txt";
        }

        public static string PendingIDsFile()
        {
            return @"Logs\" + SystemDate.ToString("yyyy-MM-dd") + @"\pendingIDs.txt";
        }

        public static bool IsRefDateExist(string refDateFile)
        {
            return System.IO.File.Exists(refDateFile);
        }

        public static int PauseTime()
        {
            return 10000;
        }

        public static bool IsAcctNo(string folderName)
        {
            switch (folderName)
            {
                case "DONE":
                case "FOR_TRANSFER":
                case "DONE2":
                    return false;
                default:
                    if (folderName.Contains("-")) return false; else return true;
            }
        }

        public static int IsProgramRunning(string Program)
        {
            System.Diagnostics.Process[] p;
            p = System.Diagnostics.Process.GetProcessesByName(Program.Replace(".exe", "").Replace(".EXE", ""));

            return p.Length;
        }

        private static string encryptionKey = "@cCP@g1bIgPH3*";
        public static string EncryptData(string data)
        {
            if (data == "") return "";

            AllcardEncryptDecrypt.EncryptDecrypt enc = new AllcardEncryptDecrypt.EncryptDecrypt(encryptionKey);
            string encryptedData = enc.TripleDesEncryptText(data);
            enc = null;
            return encryptedData;
        }

        public static string DecryptData(string data)
        {
            if (data == "") return "";

            AllcardEncryptDecrypt.EncryptDecrypt dec = new AllcardEncryptDecrypt.EncryptDecrypt(encryptionKey);
            string decryptedData = dec.TripleDesDecryptText(data);
            dec = null;
            return decryptedData;
        }
    }
}
