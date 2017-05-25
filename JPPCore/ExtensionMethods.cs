using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    public static class ExtensionMethods
    {
        private static Dictionary<string, DocumentStore> Stores = new Dictionary<string, DocumentStore>();

        public static T GetDocumentStore<T>(this Document doc) where T:DocumentStore
        {
            if(Stores.ContainsKey(doc.Name + typeof(T)))
            {                
                return (T)Stores[doc.Name + typeof(T)];
            } else
            {
                T ds = (T)Activator.CreateInstance(typeof(T));
                Stores.Add(doc.Name + typeof(T), ds);
                doc.BeginDocumentClose += Doc_BeginDocumentClose;
                return ds;
            }
        }

        private static void Doc_BeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
        {
            var currentStores = (from s in Stores where s.Key.Contains(((Document)sender).Name) select s.Key).ToArray();            
            foreach (string s in currentStores)
            {
                Stores.Remove(s);
            }
        }
    }
}
