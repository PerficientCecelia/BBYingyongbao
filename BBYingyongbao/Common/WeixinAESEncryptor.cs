using BBYingyongbao.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BBYingyongbao.Common
{
    public static class WeixinAESEncryptor
    {
        public static string AESDecriptor(string encryptedDataPlain, string iv, string session_key)
        {
            if (encryptedDataPlain.Length > 0 && iv.Length > 0)
            {
                try
                {
                    byte[] encryptedData = Convert.FromBase64String(encryptedDataPlain); 
                    RijndaelManaged rijndael = new RijndaelManaged() {
                        Key= Convert.FromBase64String(session_key),
                        IV= Convert.FromBase64String(iv),
                        Mode= CipherMode.CBC,
                        Padding= PaddingMode.PKCS7
                    };
                    ICryptoTransform transform = rijndael.CreateDecryptor();
                    byte[] plainText = transform.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                    string result = Encoding.Default.GetString(plainText);
                    return result;
                }
                catch (Exception ex)
                {
                    string errorMessage = "exception while deciper the encripted data, message:" + ex.Message + " encryptedDataPlain: " + encryptedDataPlain + " iv:" + iv + " session_Key:" + session_key;
                    LoggerHelper.LogInfo(ex.GetType(), "error message: " + ex.Message + " encryptedDataPlain: " + encryptedDataPlain + " iv:" + iv + " session_Key:" + session_key);
                    return errorMessage;
                }

            }
            else {
                return "invalid params";
            }
        }
    }
}
