using Autodesk.AutoCAD.DatabaseServices;
using JPP.Core;
using System.Collections.ObjectModel;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

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
            
            base.Load();
        }

    }
}
