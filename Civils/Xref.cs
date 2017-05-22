using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
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
            bool Aborted = false;
            SaveFileDialog sfd;
            ObjectId SurveyTextStyle;

            //Make all the drawing changes
            using (DocumentLock dl = acDoc.LockDocument())
            {
                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {
                    //Remove all sheets
                    ObjectId newLayout = LayoutManager.Current.CreateLayout("Paper");
                    LayoutManager.Current.SetCurrentLayoutId(newLayout);
                    DBDictionary lays = tr.GetObject(acDoc.Database.LayoutDictionaryId, OpenMode.ForWrite) as DBDictionary;                    
                    foreach (DBDictionaryEntry item in lays)
                    {
                        string layoutName = item.Key;
                        if (layoutName != "Model" && layoutName != "Paper")
                        {
                            LayoutManager.Current.DeleteLayout(layoutName); // Delete layout.
                        }
                    }

                    //Get or create the survey text style
                    TextStyleTable tst = tr.GetObject(acDoc.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                    if(!tst.Has("JPP_Survey"))
                    {
                        tst.UpgradeOpen();
                        TextStyleTableRecord tstr = new TextStyleTableRecord();
                        tstr.FileName = "romans.shx";
                        tstr.Name = "JPP_Survey";
                        tst.Add(tstr);
                        tr.AddNewlyCreatedDBObject(tstr, true);
                    }

                    SurveyTextStyle = tst["JPP_Survey"];

                    //Get all model space drawing objects
                    TypedValue[] tv = new TypedValue[1];
                    tv.SetValue(new TypedValue(67, 0), 0);
                    SelectionFilter sf = new SelectionFilter(tv);
                    PromptSelectionResult psr = acDoc.Editor.SelectAll(sf);

                    //Lengthy operations so show progress bar
                    ProgressMeter pm = new ProgressMeter();

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

                    pm = new ProgressMeter();
                    pm.Start("Updating layers...");
                    pm.SetLimit(layerCount);
                    foreach (ObjectId id in acLyrTbl)
                    {
                        LayerTableRecord ltr = tr.GetObject(id, OpenMode.ForWrite) as LayerTableRecord;
                        ltr.IsLocked = false;
                        ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByColor, 8);
                        ltr.LinetypeObjectId = acDoc.Database.ContinuousLinetype;
                        ltr.LineWeight = acDoc.Database.Celweight;
                        ltr.Transparency = trans;

                        pm.MeterProgress();
                        System.Windows.Forms.Application.DoEvents();
                    }
                    pm.Stop();

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

                        if(obj is Polyline)
                        {
                            Polyline pl = obj as Polyline;
                            pl.Elevation = 0;                            
                        }
                        if (obj is Polyline2d)
                        {
                            Polyline2d pl = obj as Polyline2d;
                            pl.Elevation = 0;
                        }
                        if (obj is Polyline3d)
                        {
                            Polyline3d pl3d = obj as Polyline3d;
                            foreach (ObjectId id in pl3d)
                            {
                                PolylineVertex3d plv3d = tr.GetObject(id, OpenMode.ForWrite) as PolylineVertex3d;
                                Point3d p3d = plv3d.Position;
                                plv3d.Position = new Point3d(p3d.X, p3d.Y, 0);
                            }
                        }

                        //Change all text to Romans
                        if (obj is DBText)
                        {
                            DBText text = obj as DBText;
                            text.Position = new Point3d(text.Position.X, text.Position.Y, 0);
                            text.Height = 0.4;
                            text.TextStyleId = SurveyTextStyle;
                        }
                        if (obj is MText)
                        {
                            MText text = obj as MText;
                            text.Location = new Point3d(text.Location.X, text.Location.Y, 0);
                            text.Height = 0.4;
                            text.TextStyleId = SurveyTextStyle;
                        }

                    }
                    pm.Stop();

                    //Operate over all blocks
                    BlockTable blkTable = (BlockTable)tr.GetObject(acDoc.Database.BlockTableId, OpenMode.ForRead);
                    int blockCount = 0;
                    foreach (ObjectId id in blkTable)
                    {
                        blockCount++;
                    }

                    pm = new ProgressMeter();
                    pm.Start("Updating blocks...");
                    pm.SetLimit(blockCount);
                    foreach (ObjectId id in blkTable)
                    {
                        BlockTableRecord btRecord = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        if (!btRecord.IsLayout)
                        {
                            foreach (ObjectId childId in btRecord)
                            {
                                //For each object set its color, transparency, lineweight and linetype to ByLayer
                                Entity obj = tr.GetObject(childId, OpenMode.ForWrite) as Entity;
                                obj.ColorIndex = 256;
                                obj.LinetypeId = acDoc.Database.Celtype;
                                obj.LineWeight = acDoc.Database.Celweight;


                                //Adjust Z values
                            }
                        }
                        pm.MeterProgress();
                        System.Windows.Forms.Application.DoEvents();
                    }

                    pm.Stop();                 

                    //Run the cleanup commands
                    Core.Utilities.Purge();

                    acDoc.Database.Audit(true, false);

                    //Prompt for the save location
                    sfd = new SaveFileDialog();
                    sfd.Filter = "Drawing File|*.dwg";
                    sfd.Title = "Save drawing as";
                    sfd.ShowDialog();
                    if (sfd.FileName != "")
                    {
                        tr.Commit();
                        Aborted = false; 
                    }
                    else
                    {
                        tr.Abort();
                        Aborted = true;
                    }                    
                }
                
            }           

            if(!Aborted)
            {
                acDoc.Database.SaveAs(sfd.FileName, Autodesk.AutoCAD.DatabaseServices.DwgVersion.Current);

                //Close the original file as its no longer needed
                acDoc.CloseAndDiscard();
            }            
        }        
    }
}
