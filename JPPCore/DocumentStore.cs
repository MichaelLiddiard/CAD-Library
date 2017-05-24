using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JPP.Core
{
    public class DocumentStore
    {
        public DocumentStore()
        {
            //Load the data
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            //Attach to current doc
            acDoc.BeginDocumentClose += AcDoc_BeginDocumentClose;
            acDoc.Database.BeginSave += Database_BeginSave;

            Load();
        }

        private void Database_BeginSave(object sender, DatabaseIOEventArgs e)
        {
            Save();
        }

        private void AcDoc_BeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
        {
            Save();
        }

        protected virtual void Save()
        {
            //Doesnt have nay default fields to save
        }

        protected virtual void Load()
        {
            //Doesnt have any default fields to load
        }

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
                    }
                }
            }
            catch (Exception e)
            {
                Application.ShowAlertDialog("Error saving - " + e.Message);
            }
        }

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
                    }
                }
            }
            catch (Exception e)
            {
                Application.ShowAlertDialog("Error saving - " + e.Message);
            }
        }

        private void SaveBinary(string key, object binaryObject)
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction;

            // Find the NOD in the database
            DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

            // We use Xrecord class to store data in Dictionaries
            Xrecord plotXRecord = new Xrecord();

            XmlSerializer xml = new XmlSerializer(typeof(BinaryHelper));
            BinaryHelper bh = new BinaryHelper() { t = binaryObject.GetType(), o = binaryObject };

            //BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            xml.Serialize(ms, bh);
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
                //dataString = dataString.Replace('\0', '\a');
                TypedValue tv = new TypedValue((int)DxfCode.Text, dataString);
                rb.Add(tv);
            }

            plotXRecord.Data = rb;

            // Create the entry in the Named Object Dictionary
            nod.SetAt(key, plotXRecord);
            tr.AddNewlyCreatedDBObject(plotXRecord, true);
        }

        public object LoadBinary(string Key)
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
                    //message = message.Replace('\a', '\0');
                    data = Encoding.ASCII.GetBytes(message);
                    ms.Write(data, 0, data.Length);
                }
                ms.Position = 0;
                //System.Diagnostics.Debug.Print("===== OUR DATA: " + value.TypeCode.ToString() + ". " + value.Value.ToString());
                XmlSerializer xml = new XmlSerializer(typeof(BinaryHelper));

                try
                {
                    string s = Encoding.ASCII.GetString(ms.ToArray());
                    BinaryHelper bh = (BinaryHelper)xml.Deserialize(ms);
                    return bh.o;           
                }
                catch (Exception e)
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                return null;
            }
        }

        //public ObservableCollection<Plot> Plots { get; set; }

        //public int GroundBearingPressure { get; set;}

        //public float DefaultWidth { get; set; }
    }

    struct BinaryHelper
    {
        public Type t;
        public object o;
    }
}
