using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

[assembly: ExtensionApplication(typeof(JPP.CivilStructures.Main))]

namespace JPP.CivilStructures
{
    class Main : IExtensionApplication
    {
        public const string FoundationLayer = "JPP_Foundations";
        public const string FoundationTextLayer = "JPP_FoundationText";

        UIPanelToggle foundationUI;

        public static void LoadBlocks()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            using (Database OpenDb = new Database(false, true))
            {
                string path = Assembly.GetExecutingAssembly().Location;
                path = path.Replace("Structures.dll", "");
                doc.Editor.WriteMessage(path);
                OpenDb.ReadDwgFile(path + "StructuralBlocks.dwg", System.IO.FileShare.ReadWrite, true, "");

                ObjectIdCollection ids = new ObjectIdCollection();
                using (Transaction tr = OpenDb.TransactionManager.StartTransaction())
                {
                    //For example, Get the block by name "TEST"
                    BlockTable bt;
                    bt = (BlockTable)tr.GetObject(OpenDb.BlockTableId, OpenMode.ForRead);

                    if (bt.Has("FormationTag"))
                    {
                        ids.Add(bt["FormationTag"]);
                    }
                    tr.Commit();
                }

                //if found, add the block
                if (ids.Count != 0)
                {
                    //get the current drawing database
                    Database destdb = doc.Database;

                    IdMapping iMap = new IdMapping();
                    destdb.WblockCloneObjects(ids, destdb.BlockTableId, iMap, DuplicateRecordCloning.Ignore, false);
                }
            }
        }

        public static void CreateStructuralLayers()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable acLyrTbl;
                acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForWrite) as LayerTable;

                if (!acLyrTbl.Has(FoundationLayer))
                {
                    using (LayerTableRecord acLyrTblRec = new LayerTableRecord())
                    {
                        // Assign the layer the ACI color 3 and a name
                        acLyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 6);
                        acLyrTblRec.Name = FoundationLayer;

                        // Append the new layer to the Layer table and the transaction
                        acLyrTbl.Add(acLyrTblRec);
                        acTrans.AddNewlyCreatedDBObject(acLyrTblRec, true);
                    }
                }

                if (!acLyrTbl.Has(FoundationTextLayer))
                {
                    using (LayerTableRecord acLyrTblRec = new LayerTableRecord())
                    {
                        // Assign the layer the ACI color 3 and a name
                        acLyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 2);
                        acLyrTblRec.Name = FoundationTextLayer;

                        // Append the new layer to the Layer table and the transaction
                        acLyrTbl.Add(acLyrTblRec);
                        acTrans.AddNewlyCreatedDBObject(acLyrTblRec, true);
                    }
                }

                // Save the changes and dispose of the transaction
                acTrans.Commit();
            }
        }

        public void Initialize()
        {
            RibbonControl rc = Autodesk.Windows.ComponentManager.Ribbon;
            RibbonTab JPPTab = rc.FindTab("JPPCORE_JPP_TAB");
            if (JPPTab == null)
            {
                JPPTab = JPP.Core.JPPMain.CreateTab();
            }

            RibbonPanel Panel = new RibbonPanel();
            RibbonPanelSource source = new RibbonPanelSource();
            RibbonRowPanel StructureRow = new RibbonRowPanel();            

            source.Title = "Civil Structures";

            Dictionary<string, UserControl> ucs = new Dictionary<string, UserControl>();
            ucs.Add("Site Settings", new SiteFoundationControl());
            foundationUI = new UIPanelToggle(StructureRow, JPP.CivilStructures.Properties.Resources.spade, "Foundations", new Guid("6735aef0-a297-4a39-830b-8971a452a83d"), ucs);

            //Build the UI hierarchy
            source.Items.Add(StructureRow);
            Panel.Source = source;

            JPPTab.Panels.Add(Panel);
                        
        }

        public void Terminate()
        {
            
        }
    }
}
