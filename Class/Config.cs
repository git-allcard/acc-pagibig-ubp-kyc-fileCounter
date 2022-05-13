using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kyc_fileCounter
{
    class Config
    {      
        public string BankRepo { get; set; }       
        public string DbaseConStr { get; set; }

        public string DbaseConStrSys { get; set; }
        public string SftpRemotePath { get; set; }
        public string SftpHost { get; set; }
        public string SftpUser { get; set; }
        public string SftpPass { get; set; }
        public string SftpKeyPath { get; set; }
        public int SftpPort { get; set; }
        public int SendToSftp { get; set; }
        public string SftpSshHostKeyFingerprint { get; set; }
        public string Cipher_Key { get; set; }
    }
}
