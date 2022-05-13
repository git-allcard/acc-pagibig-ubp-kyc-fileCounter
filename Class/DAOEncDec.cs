using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace kyc_fileCounter.Class
{
    class DAOEncDec
    {
        //private string Cipher_Key_SIT = "dc3cdfb50cf19cbab0906d03e4c22d66";
        //private string Cipher_Key_PROD = "1ae292629c4214440508cd472ff0fdd4";     

        public static string Decrypt(string Value, int Length)
        {
            try
            {
                var plainText = "";

                var Arr = Value.Split(':');
                var IV = Convert.FromBase64String(Arr[0]);
                var TX = Convert.FromBase64String(Arr[1]);
                var KY = Encoding.UTF8.GetBytes(Program.config.Cipher_Key);
                var aes = Aes.Create();

                aes.Key = KY;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.Zeros;

                var decipher = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream(TX))
                {
                    using (var cs = new CryptoStream(ms, decipher, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            plainText = sr.ReadToEnd();
                        }
                    }
                }

                return plainText.Substring(0, Length);
            }
            catch (Exception ex)
            {
                Program.LogToErrorLog("DAO Decrypt(): Decryption failed for " + Value + ". Error " + ex.Message);
                return "";
            }
        }
    }
}
