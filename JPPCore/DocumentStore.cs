using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace JPP.Core
{
    //TODO: Rework to use reflection, no need to override save and load?
    /// <summary>
    /// Class for storing of document level data
    /// </summary>
    public class DocumentStore
    {
        #region Constructor and Fields
        /// <summary>
        /// Create a new document store
        /// </summary>
        public DocumentStore()
        {
            //Get a reference to the active document
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            //Attach to current doc to allow for automatically persisting the database
            acDoc.BeginDocumentClose += AcDoc_BeginDocumentClose;
            acDoc.Database.BeginSave += Database_BeginSave;

            LoadWrapper();
        }
        #endregion

        #region Save and Load Methods
        /// <summary>
        /// Save all fields in class
        /// </summary>
        protected virtual void Save()
        {
            //Doesnt have nay default fields to save
        }


        /// <summary>
        /// Load all fields in class
        /// </summary>
        protected virtual void Load()
        {
            //Doesnt have any default fields to load
        }

        /// <summary>
        /// Wrapper around the save method to ensure a transaction is active when called
        /// </summary>
        private void SaveWrapper()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            try
            {
                using (DocumentLock dl = acDoc.LockDocument())
                {
                    using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                    {
                        Save();
                        tr.Commit();                        
                    }
                }
            }
            catch (Exception e)
            {
                Application.ShowAlertDialog("Error saving - " + e.Message);
            }
        }

        /// <summary>
        /// Wrapper around the load method to ensure a transaction is active when called
        /// </summary>
        private void LoadWrapper()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            try
            {
                using (DocumentLock dl = acDoc.LockDocument())
                {
                    using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                    {
                        Load();
                        tr.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                Application.ShowAlertDialog("Error saving - " + e.Message);
            }
        }

        private void Database_BeginSave(object sender, DatabaseIOEventArgs e)
        {
            SaveWrapper();
        }

        private void AcDoc_BeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
        {
            SaveWrapper();
        }

        #endregion

        #region Binary Methods
        protected void SaveBinary(string key, object binaryObject)
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction;

            // Find the NOD in the database
            DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

            // We use Xrecord class to store data in Dictionaries
            Xrecord plotXRecord = new Xrecord();

            XmlSerializer xml = new XmlSerializer(binaryObject.GetType());
            MemoryStream ms = new MemoryStream();
            xml.Serialize(ms, binaryObject);
            string s = Encoding.ASCII.GetString(ms.ToArray());

            byte[] data = new byte[512];
            int moreData = 1;
            ResultBuffer rb = new ResultBuffer();
            ms.Position = 0;
            while (moreData > 0)
            {
                data = new byte[512];
                moreData = ms.Read(data, 0, data.Length);
                string dataString = Encoding.ASCII.GetString(data);
                TypedValue tv = new TypedValue((int)DxfCode.Text, dataString);
                rb.Add(tv);
            }

            plotXRecord.Data = rb;

            // Create the entry in the Named Object Dictionary
            nod.SetAt(key, plotXRecord);
            tr.AddNewlyCreatedDBObject(plotXRecord, true);
        }

        protected T LoadBinary<T>(string Key)
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction;

            // Find the NOD in the database
            DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

            if (nod.Contains(Key))
            {
                ObjectId plotId = nod.GetAt(Key);
                Xrecord plotXRecord = (Xrecord)tr.GetObject(plotId, OpenMode.ForRead);
                MemoryStream ms = new MemoryStream();
                foreach (TypedValue value in plotXRecord.Data)
                {
                    byte[] data = new byte[512];

                    string message = (string)value.Value;
                    data = Encoding.ASCII.GetBytes(message);
                    ms.Write(data, 0, data.Length);
                }
                ms.Position = 0;
                XmlSerializer xml = new XmlSerializer(typeof(T));

                try
                {
                    string s = Encoding.ASCII.GetString(ms.ToArray());
                    return (T) xml.Deserialize(ms);     
                }
                catch (Exception e)
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                return default(T);
            }
        }
        #endregion
    }
}
