using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BBYingyongbao.Common
{
    public static class MD5Encriptor
    {
        public static string Encriptor(string input) {
            using (var md5 = MD5.Create())
            {
                MD5 mdk = new MD5CryptoServiceProvider();     
                byte[] bytIn = Encoding.UTF8.GetBytes(input.Trim());
           
                byte[] iv = { 8, 7, 6, 5, 4, 3, 2, 1 };  
                byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };//定义密钥     

                SymmetricAlgorithm cryptService = new TripleDESCryptoServiceProvider();
                cryptService.Key = key;
                cryptService.IV = iv;
                ICryptoTransform encrypto = cryptService.CreateEncryptor();
 
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                CryptoStream cs = new CryptoStream(ms, encrypto, CryptoStreamMode.Write);
                cs.Write(bytIn, 0, bytIn.Length);
                cs.FlushFinalBlock();
                return System.Convert.ToBase64String(ms.ToArray());
            }
        }

        public static string MD5Decryptor(string input) {
            byte[] bytIn = System.Convert.FromBase64String(input);
            //给出解密的密钥和偏移量，密钥和偏移量必须与加密时的密钥和偏移量相同     
            byte[] iv = { 8, 7, 6, 5, 4, 3, 2, 1 };
            byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };//定义密钥      
            SymmetricAlgorithm mobjCryptoService = new TripleDESCryptoServiceProvider();
            mobjCryptoService.Key = key;
            mobjCryptoService.IV = iv;
            //实例流进行解密     
            System.IO.MemoryStream ms = new System.IO.MemoryStream(bytIn, 0, bytIn.Length);
            ICryptoTransform encrypto = mobjCryptoService.CreateDecryptor();
            CryptoStream cs = new CryptoStream(ms, encrypto, CryptoStreamMode.Read);
            StreamReader strd = new StreamReader(cs, Encoding.Default);
            return strd.ReadToEnd();
        }
    }
}
