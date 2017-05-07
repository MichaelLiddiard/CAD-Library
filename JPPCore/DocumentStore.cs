using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    public class DocumentStore
    {
        public static DocumentStore Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new DocumentStore();
                }
                return _Current;
            }
        }

        static DocumentStore _Current;

        public DocumentStore()
        {
            //Load the data
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            //Attach to current doc
            acDoc.BeginDocumentClose += AcDoc_BeginDocumentClose;
            acDoc.Database.BeginSave += Database_BeginSave;

            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                // Find the NOD in the database
                DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

                if (nod.Contains("JPP_Plot"))
                {
                    ObjectId plotId = nod.GetAt("JPP_Plot");
                    Xrecord plotXRecord = (Xrecord)tr.GetObject(plotId, OpenMode.ForRead);
                    MemoryStream ms = new MemoryStream();
                    foreach (TypedValue value in plotXRecord.Data)
                    {
                        byte[] data = new byte[127];

                        string message = (string)value.Value;
                        message = message.Replace('\a', '\0');
                        data = Encoding.ASCII.GetBytes(message);
                        ms.Write(data, 0, data.Length);
                    }
                    ms.Position = 0;
                    //System.Diagnostics.Debug.Print("===== OUR DATA: " + value.TypeCode.ToString() + ". " + value.Value.ToString());
                    BinaryFormatter bf = new BinaryFormatter();
                    
                    Plots = bf.Deserialize(ms) as Dictionary<string, Plot>;
                } else
                {
                    Plots = new Dictionary<string, Plot>();
                }

                tr.Commit();
            }
        }

        private void Database_BeginSave(object sender, DatabaseIOEventArgs e)
        {
            Save();
        }

        private void AcDoc_BeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
        {
            Save();
        }

        public void Save()
        {
            //Load the data
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (DocumentLock dl = acDoc.LockDocument())
            {
                using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
                {
                    // Find the NOD in the database
                    DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

                    // We use Xrecord class to store data in Dictionaries
                    Xrecord plotXRecord = new Xrecord();
                    BinaryFormatter bf = new BinaryFormatter();
                    MemoryStream ms = new MemoryStream();                    
                    bf.Serialize(ms, Plots);

                    string dataS = Encoding.ASCII.GetString(ms.ToArray());
                    //dataS = dataS.Replace('\0', '\a');

                    var obj = bf.Deserialize(new MemoryStream(Encoding.ASCII.GetBytes(dataS)));

                    byte[] data = new byte[512];
                    int moreData = 1;
                    ResultBuffer rb = new ResultBuffer();
                    ms.Position = 0;
                    while (moreData > 0)
                    {
                        moreData = ms.Read(data, 0, data.Length);
                        string dataString = Encoding.ASCII.GetString(data);
                        dataString = dataString.Replace('\0', '\a');
                        TypedValue tv2 = new TypedValue((int)DxfCode.Text, dataString);
                        rb.Add(tv2);
                    }

                    plotXRecord.Data = rb;

                    // Create the entry in the Named Object Dictionary
                    nod.SetAt("JPP_Plot", plotXRecord);
                    tr.AddNewlyCreatedDBObject(plotXRecord, true);

                    tr.Commit();
                }
            }
        }

        public Dictionary<string, Plot> Plots { get; set; }
    }
}
