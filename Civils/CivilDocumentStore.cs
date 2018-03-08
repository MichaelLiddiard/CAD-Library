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
    /// <summary>
    /// Class for storing of document level data, specific to Civil modules
    /// </summary>
    public class CivilDocumentStore : DocumentStore
    {
        /// <summary>
        /// List of plots in the current drawing
        /// </summary>
        public ObservableCollection<Plot> Plots { get; set; }

        /// <summary>
        /// List of plot types in the current drawing
        /// </summary>
        public ObservableCollection<PlotType> PlotTypes { get; set; }

        /// <summary>
        /// Safe ground bearing pressure in kN/m2
        /// </summary>
        public int GroundBearingPressure { get; set;}

        /// <summary>
        /// Default width for all foundations
        /// </summary>
        public float DefaultWidth { get; set; }

        protected override void Save()
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction; //Could this potentially throw an error??

            SaveBinary(Constants.PlotID, Plots);
            SaveBinary(Constants.PlotTypeID, PlotTypes);

            using (Xrecord siteXRecord = new Xrecord())
            {
                using (ResultBuffer siteRb = new ResultBuffer())
                {

                    siteRb.Add(new TypedValue((int)DxfCode.Int32, GroundBearingPressure));
                    siteRb.Add(new TypedValue((int)DxfCode.Text, DefaultWidth));

                    siteXRecord.Data = siteRb;
                    DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);
                    nod.SetAt(Constants.SiteID, siteXRecord);
                    tr.AddNewlyCreatedDBObject(siteXRecord, true);
                }
            }
            base.Save();
        }

        protected override void Load()
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction;
            DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

            Plots = LoadBinary<ObservableCollection<Plot>>(Constants.PlotID);
            PlotTypes = LoadBinary<ObservableCollection<PlotType>>(Constants.PlotTypeID);
            if (Plots == null)
            {
                Plots = new ObservableCollection<Plot>();
                PlotTypes = new ObservableCollection<PlotType>();
            }

            foreach(Plot p in Plots)
            {
                p.Update();
                //TODO: Check this!!!
            }

            if (nod.Contains(Constants.SiteID))
            {

                ObjectId plotId = nod.GetAt(Constants.SiteID);
                using (Xrecord plotXRecord = (Xrecord)tr.GetObject(plotId, OpenMode.ForRead))
                {
                    var buffers = plotXRecord.Data.AsArray();
                    GroundBearingPressure = (int)buffers[0].Value;
                    DefaultWidth = float.Parse((string)buffers[1].Value);
                }
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
