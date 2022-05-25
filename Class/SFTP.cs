using System;
using System.Collections.Generic;
using WinSCP;
using System.IO;


namespace kyc_fileCounter
{
    class SFTP
    {
        private delegate void dlgtProcess();  
                
        private static SessionOptions sessionOptions()
        {
            SessionOptions session = new SessionOptions();
            session.Protocol = Protocol.Sftp;
            session.HostName = Program.config.SftpHost;
            session.UserName = Program.config.SftpUser;
            session.Password = Program.config.SftpPass;
            session.PortNumber = Program.config.SftpPort;
            session.SshHostKeyFingerprint = Program.config.SftpSshHostKeyFingerprint;

            if(Program.config.SftpPass!="") session.Password = Program.config.SftpPass;
            else session.SshPrivateKeyPath = Program.config.SftpKeyPath;

            return session;          
        }

        public static void SendSftp()
        {
            string errMsg = "";
            if (Program.config.SendToSftp == 1)
            {                
                //System.Text.StringBuilder sbDone = new System.Text.StringBuilder();
                int _TotalSftpTransfer = 0;

                //send zip files                    
                Program.LogToSystemLog("Sending files to sftp...");
                Program.LogToSystemLog("SynchronizeDirectories started...");

                if (!SynchronizeDirectories(Program.config.BankRepo, ref errMsg, ref _TotalSftpTransfer))
                {
                    Program.LogToErrorLog(Utilities.TimeStamp() + "RunBankProcess(): SynchronizeDirectories failed.Error " + errMsg);
                }
              
                Program.LogToSystemLog("Total file(s) uploaded: " + _TotalSftpTransfer.ToString("N0"));
            }
        }

     
        //public bool Upload_SFTP_Files(string path, bool IsZip, ref string errMsg)
        //{
        //    try
        //    {
        //        int intFileCount = Directory.GetFiles(SFTP_LOCALPATH).Length;

        //        if (intFileCount == 0)
        //        {
        //            errMsg = string.Format("[Upload] {0} is empty. No file to push.", SFTP_LOCALPATH);                  
        //            return false;
        //        }             

        //        using (Session session = new Session())
        //        {                                 
        //            session.DisableVersionCheck = true;

        //            session.Open(sessionOptions());

        //            // Upload files
        //            TransferOptions transferOptions = new TransferOptions();
        //            transferOptions.TransferMode = TransferMode.Binary;
        //            //transferOptions.ResumeSupport.State = TransferResumeSupportState.Smart;                  
                    
        //            //transferOptions.PreserveTimestamp = false;

        //            //Console.Write(AppDomain.CurrentDomain.BaseDirectory);
        //            string remotePath = SFTP_REMOTEPATH_ZIP;
        //            if (!IsZip) remotePath = SFTP_REMOTEPATH_PAGIBIGMEMU;

        //             TransferOperationResult transferResult = null;
        //            if (File.Exists(path))
        //            {
        //                {
        //                    if (!session.FileExists(remotePath + Path.GetFileName(path)))
        //                    {
        //                        transferResult = session.PutFiles(string.Format(@"{0}*", path), remotePath, false, transferOptions);
        //                    }

        //                    else
        //                    {
        //                        errMsg = string.Format("Upload_SFTP_Files(): Remote file exist " + Path.GetFileName(path));                               
        //                        return false;
        //                    }
        //                }
        //            }
        //              else
                    
        //                transferResult = session.PutFiles(string.Format(@"{0}\*", SFTP_LOCALPATH), remotePath, false, transferOptions);
                    

        //                // Throw on any error
        //                transferResult.Check();

        //                // Print results
        //                foreach (TransferEventArgs transfer in transferResult.Transfers)
        //                {
        //                    //Console.WriteLine(TimeStamp() + Path.GetFileName(transfer.FileName) + " transferred successfully");
        //                    //string strFilename = Path.GetFileName(transfer.FileName);
        //                    //File.Delete(transfer.FileName);
        //                }                        
        //            }

        //        //Console.WriteLine("Success sftp transfer " + path);
        //        //System.Threading.Thread.Sleep(100);

        //        return true;
                
        //    }                            
        //    catch (Exception ex)
        //    {
        //        errMsg = string.Format("Upload_SFTP_Files(): Runtime error {0}", ex.Message);
        //        Console.WriteLine(errMsg);
        //        //Utilities.WriteToRTB(errMsg, ref rtb, ref tssl);
        //        return false;
        //    }
        //}

        private static string BANK_REPO = "";
        private static System.Text.StringBuilder sbDone = new System.Text.StringBuilder();
        private static int TotalSftpTransfer;
        private static DAL.MsSql dal;

        private static bool SynchronizeDirectories(string bank_repo, ref string errMsg, ref int _TotalSftpTransfer)
        {
            try
            {
                if (dal == null) dal = new DAL.MsSql(Program.config.DbaseConStr);
                string forTransferFolder = bank_repo;                

                int intFileCount = Directory.GetFiles(forTransferFolder).Length;                             

                using (Session session = new Session())
                {
                    //session.DisableVersionCheck = true;

                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Binary;
                    transferOptions.FilePermissions = null;
                    transferOptions.PreserveTimestamp = false;
                    

                    // Will continuously report progress of synchronization
                    session.FileTransferred += FileTransferred;                    

                    // Connect
                    session.Open(sessionOptions());                    

                    // Synchronize files
                    SynchronizationResult synchronizationResult;                 
                    synchronizationResult = session.SynchronizeDirectories(SynchronizationMode.Remote, @forTransferFolder, Program.config.SftpRemotePath, false, false, SynchronizationCriteria.None, transferOptions);                    

                    // Throw on any error
                    synchronizationResult.Check();
                }

                _TotalSftpTransfer = TotalSftpTransfer;               

                return true;
            }
            catch (Exception ex)
            {
                errMsg = string.Format("SynchronizeDirectories(): Runtime error {0}", ex.Message);
                Console.WriteLine(errMsg);                
                return false;
            }
        }                

        private static void FileTransferred(object sender, TransferEventArgs e)
        {
            string msg = "";
            if (e.Error == null)
            {
                msg = string.Format("{0}Upload of {1} succeeded", Utilities.TimeStamp(), Path.GetFileName(e.FileName));
                Console.WriteLine(msg);
                Class.Log.SaveToSystemLog(msg);

                string[] arr = e.FileName.Split('\\');
                string sourceFolder = "";
                try
                {
                    sourceFolder = arr[arr.Length - 2];
                }
                catch { }

                Program.LogToSystemLog("Updating table sftp...");
                //INSERT INTO tbl_SFTP (Remark, SFTPTransferDate, DatePosted, TimePosted) VALUES (Remark, SFTPTransferDate, GETDATE(), GETDATE())
                if (Program.dal.ExecuteScalar("SELECT COUNT(*) FROM tbl_SFTP WHERE Remark='" + Path.GetFileName(e.FileName) + "'"))
                {
                    if ((int)Program.dal.ObjectResult == 0)
                    {
                        if (!Program.dal.ExecuteQuery("INSERT INTO tbl_SFTP (Type, Remark, SFTPTransferDate, DatePosted, TimePosted) VALUES ('FC','" + Path.GetFileName(e.FileName) + "', '" + DateTime.Now + "', GETDATE(), '" + DateTime.Now.TimeOfDay + "')"))
                        {
                            Console.WriteLine("Failed to insert to sftp " + Path.GetFileName(e.FileName) + ". " + Program.dal.ErrorMessage);
                            Class.Log.SaveToErrorLog("Failed to insert to sftp " + Path.GetFileName(e.FileName) + ". " + Program.dal.ErrorMessage);
                        }
                        else
                        {
                            if (Program.dal.ExecuteScalar("SELECT ISNULL(ID,0) FROM tbl_SFTP WHERE Remark='" + Path.GetFileName(e.FileName) + "'"))
                            {
                                if (Program.dal.ObjectResult != null)
                                {
                                    if ((int)Program.dal.ObjectResult > 0)
                                    {
                                        string doneIDs = "";
                                        if (System.IO.File.Exists(Utilities.DoneIDsFile(Utilities.SystemDate))) doneIDs = System.IO.File.ReadAllText(Utilities.DoneIDsFile(Convert.ToDateTime(Path.GetFileName(e.FileName).Substring(0,10))));
                                        if (!Program.dal.ExecuteQuery("UPDATE tbl_SFTP SET FileCntr_Id = " + Program.dal.ObjectResult.ToString() + " where id in (" + doneIDs + ")"))
                                        {
                                            Console.WriteLine("Failed to update sftp details for " + Path.GetFileName(e.FileName) + ". " + Program.dal.ErrorMessage);
                                            Class.Log.SaveToErrorLog("Failed to update sftp details for " + Path.GetFileName(e.FileName) + ". " + Program.dal.ErrorMessage);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }            

                TotalSftpTransfer += 1;
                System.Threading.Thread.Sleep(100);
            }
            else
            {
                msg = string.Format("{0}Upload of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.FileName), e.Error);
                Console.WriteLine(msg);
                Class.Log.SaveToErrorLog(msg);                
            }

            if (e.Chmod != null)
            {
                if (e.Chmod.Error == null)
                {
                    msg = string.Format("{0}Permissions of {1} set to {2}", Utilities.TimeStamp(), Path.GetFileName(e.Chmod.FileName), e.Chmod.FilePermissions);
                    Console.WriteLine(msg);
                    Class.Log.SaveToSystemLog(msg);
                }
                else
                {                    
                    msg = string.Format("{0}Setting permissions of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.Chmod.FileName), e.Chmod.Error);
                    Console.WriteLine(msg);
                    Class.Log.SaveToErrorLog(msg);
                }
            }
            else
            {
                //Console.WriteLine("{0}Permissions of {1} kept with their defaults", TimeStamp(), e.Destination);
            }

            if (e.Touch != null)
            {
                if (e.Touch.Error == null)
                {                    
                    msg = string.Format("{0}Timestamp of {1} set to {2}", Utilities.TimeStamp(), Path.GetFileName(e.Touch.FileName), e.Touch.LastWriteTime);
                    Console.WriteLine(msg);
                    Class.Log.SaveToSystemLog(msg);
                }
                else
                {                    
                    msg = string.Format("{0}Setting timestamp of {1} failed: {2}", Utilities.TimeStamp(), Path.GetFileName(e.Touch.FileName), e.Touch.Error);
                    Console.WriteLine(msg);
                    Class.Log.SaveToErrorLog(msg);
                }
            }
            else
            {
                // This should never happen during "local to remote" synchronization                
                msg = string.Format("{0}Timestamp of {1} kept with its default (current time)", Utilities.TimeStamp(), e.Destination);
                Console.WriteLine(msg);
                Class.Log.SaveToErrorLog(msg);
            }
        }


    }
}
