using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
    }
}
