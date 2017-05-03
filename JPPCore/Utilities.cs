using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JPP.Core
{
    public class Utilities
    {
        public static BitmapImage LoadImage(Bitmap image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();            
            return bi;
        }

        public static void Purge()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
                        
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                bool toBePurged = true;

                while (toBePurged)
                {
                    // Create the list of objects to "purge"
                    ObjectIdCollection collection = new ObjectIdCollection();

                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layer in lt)
                    {
                        collection.Add(layer);
                    }

                    LinetypeTable ltt = tr.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                    foreach (ObjectId linetype in ltt)
                    {
                        collection.Add(linetype);
                    }

                    TextStyleTable tst = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                    foreach (ObjectId text in tst)
                    {
                        collection.Add(text);
                    }

                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    foreach (ObjectId block in bt)
                    {
                        collection.Add(block);
                    }

                    DBDictionary tsd = tr.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
                    foreach (DBDictionaryEntry ts in tsd)
                    {                        
                        collection.Add(ts.Value);
                    }

                    // Call the Purge function to filter the list
                    db.Purge(collection);

                    if (collection.Count > 0)
                    {
                        // Erase each of the objects we've been allowed to
                        foreach (ObjectId id in collection)
                        {
                            DBObject obj = tr.GetObject(id, OpenMode.ForWrite);
                            obj.Erase();
                        }
                    } else
                    {
                        toBePurged = false;
                    }
                }

                tr.Commit();
            }
        }

        public const string FoundationLayer = "JPP_Foundations";
        public const string FoundationTextLayer = "JPP_FoundationText";

        public static void LoadBlocks()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            using (Database OpenDb = new Database(false, true))
            {
                string path = Assembly.GetExecutingAssembly().Location;
                path = path.Replace("JPPCore.dll", "");
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
                        acLyrTblRec.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 6);
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
                        acLyrTblRec.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 2);
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
    }
}
