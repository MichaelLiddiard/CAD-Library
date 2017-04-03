using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: CommandClass(typeof(JPP.Civils.Xref))]

namespace JPP.Civils
{    
    class Xref
    {
        [CommandMethod("ImportAsXref", CommandFlags.Session)]
        public static void ImportAsXref()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

            //Make all the drawing changes

            using (DocumentLock dl = acDoc.LockDocument())
            {
                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {

                    //Get all model space drawing objects
                    TypedValue[] tv = new TypedValue[1];
                    tv.SetValue(new TypedValue(67, 0), 0);
                    SelectionFilter sf = new SelectionFilter(tv);
                    PromptSelectionResult psr = acDoc.Editor.SelectAll(sf);

                    //Lengthy operations so show progress bar
                    ProgressMeter pm = new ProgressMeter();
                    pm.Start("Updating objects...");
                    pm.SetLimit(psr.Value.Count);

                    foreach (SelectedObject so in psr.Value)
                    {
                        //For each object set its color, transparency, lineweight and linetype to ByLayer
                        Entity obj = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                        obj.ColorIndex = 256;
                        obj.LinetypeId = acDoc.Database.Celtype;
                        obj.LineWeight = acDoc.Database.Celweight;

                        pm.MeterProgress();
                        System.Windows.Forms.Application.DoEvents();

                        //Adjust Z values

                    }
                    pm.Stop();
                                        
                    //Operate over all blocks
                    BlockTable blkTable = (BlockTable)tr.GetObject(acDoc.Database.BlockTableId, OpenMode.ForRead);
                    int blockCount = 0;
                    foreach (ObjectId id in blkTable)
                    {
                        blockCount++;
                    }

                    pm.Start("Updating blocks...");
                    pm.SetLimit(blockCount);
                    foreach (ObjectId id in blkTable)
                    {
                        BlockTableRecord btRecord = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        if (!btRecord.IsLayout)
                        {
                            foreach(ObjectId childId in btRecord)
                            {
                                //For each object set its color, transparency, lineweight and linetype to ByLayer
                                Entity obj = tr.GetObject(childId, OpenMode.ForWrite) as Entity;
                                obj.ColorIndex = 256;
                                obj.LinetypeId = acDoc.Database.Celtype;
                                obj.LineWeight = acDoc.Database.Celweight;

                                System.Windows.Forms.Application.DoEvents();

                                //Adjust Z values
                            }
                        }
                        pm.MeterProgress();
                    }


                    Byte alpha = (Byte)(255 * (1));
                    Transparency trans = new Transparency(alpha);

                    //Iterate over all layer and set them to color 8, 0 transparency and continuous linetype
                    // Open the Layer table for read
                    LayerTable acLyrTbl = tr.GetObject(acDoc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    int layerCount = 0;
                    foreach (ObjectId id in acLyrTbl)
                    {
                        layerCount++;
                    }
                    pm.SetLimit(layerCount);
                    pm.Start("Updating layers...");
                    foreach (ObjectId id in acLyrTbl)
                    {
                        LayerTableRecord ltr = tr.GetObject(id, OpenMode.ForWrite) as LayerTableRecord;
                        ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByColor, 8);
                        ltr.LinetypeObjectId = acDoc.Database.ContinuousLinetype;
                        ltr.Transparency = trans;

                        pm.MeterProgress();
                    }
                    pm.Stop();
                    //Change all text to Romans

                    tr.Commit();
                }

                //Prompt for the save location
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Drawing File|*.dwg";
                sfd.Title = "Save drawing as";
                sfd.ShowDialog();
                if (sfd.FileName != "")
                {
                    acDoc.Database.SaveAs(sfd.FileName, Autodesk.AutoCAD.DatabaseServices.DwgVersion.Current);
                }                
            }

            //Run the cleanup commands
            //Core.Utilities.Purge();

            acDoc.Database.Audit(true, false);

            //Close the original file as its no longer needed
            acDoc.CloseAndDiscard();
        }        
    }
}
