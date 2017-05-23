using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

namespace JPPCommands
{
    public class JPP_Utility_Commands
    {
        [CommandMethod("SetOutlineCount")]
        public static void SetOutlineCount()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {

                    // Get object id of the JPP_App_Config_Data dictionary
                    DBDictionary nod = acTrans.GetObject(acCurrDb.NamedObjectsDictionaryId, OpenMode.ForWrite) as DBDictionary;
                    ObjectId JPPAppConfigDataId = nod.GetAt(JPP_App_Config_Params.JPP_APP_CONFIG_DATA);

                    if (JPPAppConfigDataId == null)
                    {
                        acEditor.WriteMessage("\nJPP App dictionary doesn't exist in this drawing. Please run the FF command.");
                        return;
                    }

                    DBDictionary JPPAppConfigData = acTrans.GetObject(JPPAppConfigDataId, OpenMode.ForRead) as DBDictionary;
                    ObjectId xrecId = JPPAppConfigData.GetAt(JPP_App_Config_Params.JPP_APP_NEXT_BLOCK_INDEX);
                    Xrecord xrec = acTrans.GetObject(xrecId, OpenMode.ForRead) as Xrecord;
                    TypedValue[] xrecData = xrec.Data.AsArray();
                    Int16 nextOutlineIndex = Convert.ToInt16(xrecData[0].Value);

                    // Now iterate around all the outline blocks in the drawing to find the highest index.
                    BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    Int16 maxOutlineIndex = 0;
                    foreach (ObjectId acBlkTblRecId in acBlkTbl)
                    {
                        BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTblRecId, 
                                                                            OpenMode.ForRead) as BlockTableRecord;

                        if (acBlkTblRec.Name.StartsWith(JPP_App_Config_Params.JPP_APP_NEW_BLOCK_PREFIX))
                        {
                            string indexStr = acBlkTblRec.Name.Substring(JPP_App_Config_Params.JPP_APP_NEW_BLOCK_PREFIX.Length);
                            Int16 index = Convert.ToInt16(indexStr);
                            if (index > maxOutlineIndex)
                                maxOutlineIndex = index;
                        }
                    }
                    // Compare the values of the max outline index and the next outline index. If the
                    // next outline index != max outline index + 1 then set the next outline index to
                    // max outline index + 1
                    if (nextOutlineIndex != maxOutlineIndex + 1)
                    {
                        Xrecord newXrec = new Xrecord();
                        newXrec.Data = new ResultBuffer(new TypedValue((int)DxfCode.Int16, (maxOutlineIndex + 1)));
                        JPPAppConfigData.UpgradeOpen();
                        JPPAppConfigData.SetAt(JPP_App_Config_Params.JPP_APP_NEXT_BLOCK_INDEX, newXrec);
                        acTrans.AddNewlyCreatedDBObject(newXrec, true);
                        acTrans.Commit();
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(
                                                        "The outline count has been set to " 
                                                            + Convert.ToString(maxOutlineIndex + 1));
                    }
                    else
                    {
                        acTrans.Dispose();
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("The outline count is correct.");
                    }

                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError creating setting outline count!\n");
                    acTrans.Abort();
                    return;
                }

            }


        }
    }
}
