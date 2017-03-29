using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    class Authentication
    {
        public bool Authenticated()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "license.key";
            if (!File.Exists(path))
            {
                return false;
            }

            using (StreamReader sr = new StreamReader(File.OpenRead(path)))
            {
                string hash = sr.ReadToEnd();
                UnicodeEncoding encoding = new UnicodeEncoding();
                byte[] keyData = encoding.GetBytes(hash);

                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                RSAParameters param = new RSAParameters();                               
            }
            
            return false;
        }
    }
}
