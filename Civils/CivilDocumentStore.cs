using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils
{
    public class CivilDocumentStore : DocumentStore
    {
        public ObservableCollection<Plot> Plots { get; set; }

        public int GroundBearingPressure { get; set;}

        public float DefaultWidth { get; set; }

        protected override void Save()
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction;

            SaveBinary("JPP_Plot", Plots);

            Xrecord siteXRecord = new Xrecord();
            ResultBuffer siteRb = new ResultBuffer();

            siteRb.Add(new TypedValue((int)DxfCode.Int32, GroundBearingPressure));
            siteRb.Add(new TypedValue((int)DxfCode.Text, DefaultWidth));

            siteXRecord.Data = siteRb;
            DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);
            nod.SetAt("JPP_Site", siteXRecord);
            tr.AddNewlyCreatedDBObject(siteXRecord, true);

            base.Save();
        }

        protected override void Load()
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction;
            DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

            Plots = LoadBinary<ObservableCollection<Plot>>("JPP_Plot");
            if (Plots == null)
            {
                Plots = new ObservableCollection<Plot>();
            }

            foreach(Plot p in Plots)
            {
                p.Rebuild();
            }

            if (nod.Contains("JPP_Site"))
            {

                ObjectId plotId = nod.GetAt("JPP_Site");
                Xrecord plotXRecord = (Xrecord)tr.GetObject(plotId, OpenMode.ForRead);
                var buffers = plotXRecord.Data.AsArray();
                GroundBearingPressure = (int)buffers[0].Value;
                DefaultWidth = float.Parse((string)buffers[1].Value);
            }
            else
            {
                GroundBearingPressure = 100;
                DefaultWidth = 0.6f;
            }

            base.Load();
        }

    }
}
