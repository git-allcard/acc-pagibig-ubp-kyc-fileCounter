using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using kyc_fileCounter.Class;

namespace kyc_fileCounter
{
    internal class Program
    {
        #region constructors

        private static string APP_NAME = "kyc_fileCounter";
        private static int processIntervalSeconds = 120;

        private delegate void dlgtProcess(DateTime dtmReportDate);

        private static System.Threading.Thread _bankThread;

        private static string configFile = AppDomain.CurrentDomain.BaseDirectory + "config";
        //private static string refDateFile = AppDomain.CurrentDomain.BaseDirectory + "refDates";
        public static Config config = null;
        public static DAL.MsSql dal = null;
        public static DAL.MsSql dalSys = null;

        private static bool IsBankProcessReady = true;
        private static string reportFolder = AppDomain.CurrentDomain.BaseDirectory + "Reports";

        private static System.Data.DataTable dtBankFiles = null;

        #endregion
        static void Main()
        {
            if (Utilities.IsProgramRunning(APP_NAME) > 1) return;

            short intRetry = 1;
            Utilities.SystemDate = DateTime.Now.Date;
            //Utilities.SystemDate = Convert.ToDateTime("2021-12-01");

            while (!Init())
            {
                if (intRetry == 5)
                {
                    System.Threading.Thread.Sleep(Utilities.PauseTime());
                    Environment.Exit(0);
                }
                System.Threading.Thread.Sleep(5000);
                intRetry += 1;
            }
            
            //if (!System.IO.Directory.Exists(reportFolder))Directory.CreateDirectory(reportFolder);

            StartThread();
        }

        private static bool Init()
        {
            try
            {
                LogToSystemLog("Checking config...");
                if (!File.Exists(configFile))
                {
                    LogToErrorLog("Init(): Config file is missing");
                    sbEmail.AppendLine(Utilities.TimeStamp() + "Init(): Config file is missing");
                    return false;
                }

                try
                {
                    config = new Config();
                    var configData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Config>>(File.ReadAllText(configFile));
                    config = configData[0];

                    Utilities.ConStr = config.DbaseConStr;
                    Utilities.ConStrSys = config.DbaseConStrSys;

                    dal = new DAL.MsSql(Utilities.ConStr);
                    dalSys = new DAL.MsSql(Utilities.ConStrSys);

                    //check dbase connection
                    if (!dal.IsConnectionOK(Utilities.ConStr))
                    {
                        LogToErrorLog("Init(): Connection to database failed. " + dal.ErrorMessage);
                        sbEmail.AppendLine(Utilities.TimeStamp() + "Init(): Connection to database failed. " + dal.ErrorMessage);
                        return false;
                    }

                    //check dbase connection
                    if (!dalSys.IsConnectionOK(Utilities.ConStrSys))
                    {
                        LogToErrorLog("Init(): Connection to database sys failed. " + dalSys.ErrorMessage);
                        sbEmail.AppendLine(Utilities.TimeStamp() + "Init(): Connection to database sys failed. " + dalSys.ErrorMessage);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogToErrorLog("Init(): Error reading config file. Runtime catched error " + ex.Message);
                    sbEmail.AppendLine(Utilities.TimeStamp() + "Init(): Error reading config file. Runtime catched error " + ex.Message);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogToErrorLog("Init(): Runtime catched error " + ex.Message);
                return false;
            }
        }

        private static void StartThread()
        {
            System.Threading.Thread objNewThread = new System.Threading.Thread(BankThread);
            objNewThread.Start();
            _bankThread = objNewThread;
        }

        private static void BankThread()
        {
            try
            {
                while (true)
                {
                    if (IsBankProcessReady)
                    {
                        IsBankProcessReady = false;
                        dlgtProcess _delegate = new dlgtProcess(RunBankProcess);

                        Console.WriteLine(Utilities.TimeStamp() + "Processing " + Utilities.SystemDate.ToShortDateString() + "...");
                        _delegate.Invoke(Utilities.SystemDate);
                        _delegate = null;                                          

                        //System.Threading.Thread.Sleep(processIntervalSeconds * 1000); // 60000 = 1minute                        
                        Console.WriteLine(Utilities.TimeStamp() + "Application will close in " + Utilities.PauseTime().ToString() + " seconds.");
                        System.Threading.Thread.Sleep(Utilities.PauseTime());
                        Environment.Exit(0);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToErrorLog("ProgramThread(): Runtime catched error " + ex.Message);
            }
        }

        private static System.Text.StringBuilder sbEmail = new System.Text.StringBuilder();

        private static void RunBankProcess(DateTime dtmReportDate)
        {
            if (dal == null) dal = new DAL.MsSql(Utilities.ConStr);
            if (dalSys == null) dalSys = new DAL.MsSql(Utilities.ConStrSys);

            string startTime = Utilities.TimeStamp();
            string endTime = Utilities.TimeStamp();

            LogToSystemLog("RunBankProcess started...");

            sbEmail.AppendLine(startTime + "Process started");

            DateTime dtmLast = dtmReportDate;
            string strLastProcessTime = dtmLast.ToString();
            string strNextProcessTime = dtmLast.AddSeconds(processIntervalSeconds).ToString();  

            LogToSystemLog("Extracting data from sftp table...");
            //sbEmail.AppendLine(startTime + "Data extraction from database with report date " + dtmLast.Date.ToShortDateString());
            if (GetTransferredRecordsFromSFTP(dtmLast))
            {
                
            }

            System.Text.StringBuilder sbReport = new System.Text.StringBuilder();

            //sftp
            SFTP.SendSftp();

            endTime = Utilities.TimeStamp();

            //log
            LogToSystemLog("Start: " + startTime);
            LogToSystemLog("End: " + endTime);
            LogToSystemLog("End of process");           

            IsBankProcessReady = true;
        }

        private static bool IsDAOTxn(string mid, ref string acctNo)
        {
            if (dalSys.SelectQuery(string.Format("select Pagibig_ID, Account_No, cast(Stamp as date) as datePost from UBP_Savings_Account where Pagibig_ID = '{0}' order by id", mid)))
            {
                if (dalSys.TableResult.Rows.Count > 0)
                {
                    acctNo = DAOEncDec.Decrypt(dalSys.TableResult.Rows[0]["Account_No"].ToString(), 12);
                    return true; 
                }
                else return false;
            }
            else
            {
                LogToErrorLog("IsDAOTxn query failed . " + dalSys.ErrorMessage);
                sbEmail.AppendLine(Utilities.TimeStamp() + "IsDAOTxn query failed . " + dalSys.ErrorMessage);
                return false;
            } 
        }

        private static string GetRecardAcctNo(string mid, string cardNo)
        {
            if (dal.SelectQuery(string.Format("select PagIBIGID, CardNo, AccountNumber from tbl_DCS_Card_Account where PagIBIGID = '{0}' order by id", mid)))
            {
                string acctNo = "";
                bool isCardNoMatched = false;

                foreach (System.Data.DataRow rw in dal.TableResult.Rows)
                {
                    string recardCardNo = rw["CardNo"].ToString();
                    if (rw["AccountNumber"].ToString() != "********0000") acctNo = rw["AccountNumber"].ToString();
                    
                    //if (rw["CardNo"].ToString() == cardNo) isCardNoMatched = true;
                    if (recardCardNo.Substring(recardCardNo.Length - 4) == cardNo.Substring(cardNo.Length - 4)) isCardNoMatched = true;                    
                }

                if (isCardNoMatched) return acctNo;
                else return "";
            }
            else
            {
                LogToErrorLog("select tbl_DCS_Card_Account query failed . " + dal.ErrorMessage);
                sbEmail.AppendLine(Utilities.TimeStamp() + "select tbl_DCS_Card_Account query failed . " + dal.ErrorMessage);
                return "";
            }
        }

        private static bool GetTransferredRecordsFromSFTP(DateTime dtmReportDate)
        {
            try
            {                
                //string doneIDsFile = Utilities.DoneIDsFile(dtmReportDate);                

                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT dbo.tbl_SFTP.ID, dbo.tbl_SFTP.PagIBIGID, dbo.tbl_SFTP.GUID, dbo.tbl_SFTP.Remark, dbo.tbl_SFTP.PagIbigMemConsoDate, ");
                sb.Append("dbo.tbl_SFTP.SFTPTransferDate, dbo.tbl_Member.RefNum, dbo.tbl_Member.Member_FirstName, ");
                sb.Append("dbo.tbl_Member.Member_MiddleName, dbo.tbl_Member.Member_LastName, dbo.tbl_Member.KioskID, dbo.tbl_branch.Branch ");
                sb.Append("FROM dbo.tbl_SFTP LEFT OUTER JOIN ");       
                //sb.Append("dbo.tbl_Member ON dbo.tbl_SFTP.DatePosted = dbo.tbl_Member.ApplicationDate AND dbo.tbl_SFTP.PagIBIGID = dbo.tbl_Member.PagIBIGID INNER JOIN ");
                sb.Append("dbo.tbl_Member ON dbo.tbl_SFTP.PagIBIGID = dbo.tbl_Member.PagIBIGID INNER JOIN ");
                sb.Append("dbo.tbl_branch ON dbo.tbl_Member.requesting_branchcode = dbo.tbl_branch.requesting_branchcode ");
                sb.Append(String.Format("WHERE (dbo.tbl_SFTP.SFTPTransferDate BETWEEN '{0} 00:00:00' AND '{0} 23:59:59') AND (dbo.tbl_SFTP.Type = 'TXT') AND FileCntr_Id IS NULL ", dtmReportDate.ToShortDateString()));
                sb.Append(String.Format("AND (dbo.tbl_Member.EntryDate BETWEEN '{0} 00:00:00' AND '{0} 23:59:59') ", dtmReportDate.ToShortDateString()));

                if (dal.SelectQuery(sb.ToString()))                
                {                    
                    dtBankFiles = dal.TableResult;
                    if (dtBankFiles.DefaultView.Count == 0) LogToSystemLog("Table is empty");
                    else
                    {
                        LogToSystemLog("Extracted data: " + dtBankFiles.DefaultView.Count.ToString("N0"));
                        sbEmail.AppendLine("Extracted data: " + dtBankFiles.DefaultView.Count.ToString("N0"));
                        int intRecord = 1;
                        StringBuilder sbList = new StringBuilder();
                        StringBuilder sbDone = new StringBuilder();
                        foreach (System.Data.DataRow rw in dtBankFiles.Rows)
                        {

                            try
                            {
                                string mid = rw["PagIBIGID"].ToString();
                                string middleName = "";
                                string guid = "";
                                if (rw["Member_MiddleName"] != null) middleName = rw["Member_MiddleName"].ToString();
                                if (rw["GUID"] != null) guid = Utilities.DecryptData(rw["GUID"].ToString());

                                //string acctNo = string.Concat("********", guid.Substring(8, 4));
                                string acctNo = string.Concat("********", guid.Substring(guid.Length - 4));
                                string daoAcctNo = "";
                                bool isDao = IsDAOTxn(mid, ref daoAcctNo);

                                if (guid.Length > 12)
                                {
                                    //recard here                                  
                                    if (!isDao) guid = GetRecardAcctNo(mid, acctNo);
                                    else guid = daoAcctNo;
                                }
                                else
                                {
                                    if (!isDao) guid = acctNo;
                                }
                                                              
                                //File Content: 1 MID, LAST NAME, FIRST NAME, ACCOUNT NAME, KIOSK, BRANCH, SENT STAMP
                                sbList.Append(string.Format("{0} {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}{9}", intRecord.ToString(), rw["PagIBIGID"].ToString(), rw["Member_LastName"].ToString(), rw["Member_FirstName"].ToString(), middleName, guid, rw["KioskID"].ToString(), rw["Branch"].ToString(), rw["SFTPTransferDate"].ToString(), "\r"));
                                
                                if (sbDone.ToString() == "") sbDone.Append(rw["ID"].ToString());
                                else sbDone.Append("," + rw["ID"].ToString());
                            }
                            catch (Exception ex)
                            {
                                LogToErrorLog("Failed in SFTP ID " + rw["ID"].ToString() + ". Runtime error " + ex.Message);
                            }


                            System.Threading.Thread.Sleep(100);
                            intRecord += 1;
                        }

                        //string reportDateFolder = System.IO.Path.Combine(reportFolder, Utilities.SystemDate.ToString("yyyy-MM-dd"));
                        string reportDateFolder = System.IO.Path.Combine(config.BankRepo, Utilities.SystemDate.ToString("yyyy-MM-dd"));
                        string fileReport = string.Concat(System.IO.Path.Combine(reportDateFolder, Utilities.SystemDate.ToString("yyyy-MM-dd")), "_kyc_", (intRecord - 1).ToString(), ".txt");
                        if (!System.IO.Directory.Exists(reportDateFolder)) Directory.CreateDirectory(reportDateFolder);
                        File.WriteAllText(fileReport, sbList.ToString());

                        string doneIDs = "";
                        if (System.IO.File.Exists(Utilities.DoneIDsFile(Utilities.SystemDate))) doneIDs = System.IO.File.ReadAllText(Utilities.DoneIDsFile(Utilities.SystemDate));
                        if (doneIDs != "") 
                        { 
                            System.IO.File.Copy(Utilities.DoneIDsFile(Utilities.SystemDate), Utilities.DoneIDsFile(Utilities.SystemDate).Replace(".txt", String.Concat("_",System.DateTime.Now.ToString("hhmmss"),".txt")));
                            System.IO.File.Delete(Utilities.DoneIDsFile(Utilities.SystemDate));
                            //Class.Log.SaveToDoneIDs("," + sbDone.ToString(), Utilities.SystemDate);
                        }

                        Class.Log.SaveToDoneIDs(Path.GetFileNameWithoutExtension(sbDone.ToString()), Utilities.SystemDate);
                    }
                }
                else
                {
                    LogToErrorLog("SelectQuery failed . " + dal.ErrorMessage);
                    sbEmail.AppendLine(Utilities.TimeStamp() + "SelectQuery failed . " + dal.ErrorMessage);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogToErrorLog("GetTransferredRecordsFromSFTP(): " + ex.Message);
                sbEmail.AppendLine(Utilities.TimeStamp() + "GetTransferredRecordsFromSFTP(): " + ex.Message);
                return false;
            }
            //finally { dal = null; }
        }

        private static bool DeleteFile(string strFile)
        {
            try
            {
                File.Delete(strFile);

                return true;
            }
            catch (Exception ex)
            {
                LogToErrorLog("DeleteFile(): Runtime catched error " + ex.Message);
                return false;
            }
        }

        public static void LogToSystemLog(string logDesc)
        {
            Console.WriteLine(Utilities.TimeStamp() + logDesc);
            Log.SaveToSystemLog(Utilities.TimeStamp() + logDesc);
        }

        public static void LogToErrorLog(string logDesc)
        {
            Console.WriteLine(Utilities.TimeStamp() + logDesc);
            Log.SaveToErrorLog(Utilities.TimeStamp() + logDesc);
        }
    }
}
