using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JPP.Core
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Stores that are currently loaded into memory
        /// </summary>
        private static Dictionary<string, DocumentStore> Stores = new Dictionary<string, DocumentStore>();

        /// <summary>
        /// Retrieve the document store to access embedded data in the specified document
        /// </summary>
        /// <typeparam name="T">Type of store to retrieve</typeparam>
        /// <param name="doc">The document for which to retrieve embedded data</param>
        /// <returns>The requested document store. If none is found a new instance is created</returns>
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

        /// <summary>
        /// Retrieve the document store to access embedded data in the specified document
        /// </summary>
        /// <typeparam name="T">Type of store to retrieve</typeparam>
        /// <returns>The requested document store. If none is found a new instance is created</returns>
        public static T GetDocumentStore<T>(this Database db) where T:DocumentStore
        {
            T ds = (T)Activator.CreateInstance(typeof(T));
            return ds;
        }

        private static void Doc_BeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
        {
            //When a document closes remove it from the store
            var currentStores = (from s in Stores where s.Key.Contains(((Document)sender).Name) select s.Key).ToArray();            
            foreach (string s in currentStores)
            {
                Stores.Remove(s);
            }
        }

        public static Arc GetArc(this CircularArc3d curve)
        {
            return new Arc(curve.Center, curve.Radius, curve.StartAngle, curve.EndAngle);
        }
    }
}
