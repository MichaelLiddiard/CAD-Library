using Autodesk.AutoCAD.ApplicationServices;
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
        public static Authentication Current
        {
            get
            {
                if(_Current == null)
                {
                    _Current = new Authentication();
                }
                return _Current;
            }
        }

        private static Authentication _Current;

        private bool? _Authenticated;

        public bool Authenticated()
        {
            if(_Authenticated == null)
            {
                _Authenticated = CheckLicense();
            }

            return (bool)_Authenticated;
        }

        private bool CheckLicense()
        {
#if DEBUG
            Application.ShowAlertDialog("Running in debug mode, no authentication required.");
            return true;
#else
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
#endif
        }
    }
}
