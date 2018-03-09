﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.CivilStructures
{
    /// <summary>
    /// Class for storing of document level data, specific to Civil Structural modules
    /// </summary>
    class CivilStructureDocumentStore : DocumentStore
    {
        public SiteFoundations SiteFoundations { get; set; }        

        protected override void Save()
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction; //Could this potentially throw an error??

            SaveBinary(CSConstants.FoundationID, SiteFoundations);            
                        
            base.Save();
        }

        protected override void Load()
        {
            Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            Transaction tr = acCurDb.TransactionManager.TopTransaction;
            DBDictionary nod = (DBDictionary)tr.GetObject(acCurDb.NamedObjectsDictionaryId, OpenMode.ForWrite);

            SiteFoundations = LoadBinary<SiteFoundations>(CSConstants.FoundationID);
            
            if (SiteFoundations == null)
            {
                SiteFoundations = new SiteFoundations();
            }

            /*foreach (Plot p in Plots)
            {
                p.Update();
            }*/

            base.Load();
        }

    }
}