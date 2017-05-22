using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Windows;

namespace JPPCommands
{
    public static class JPPUtils
    {
        public static bool addExtensionDictionary(ObjectId dbObjectId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    DBObject acDbObj = acTrans.GetObject(dbObjectId, OpenMode.ForRead);
                    // ObjectId extDictId = acDbObj.ExtensionDictionary;
                    // Check if object has an extension dictionary and add if not
                    if (acDbObj.ExtensionDictionary == ObjectId.Null)
                    {
                        acDbObj.UpgradeOpen();
                        acDbObj.CreateExtensionDictionary();
                        //extDictId = acDbObj.ExtensionDictionary;
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError adding extension dictionary!\n");
                    acTrans.Commit();
                    return false;
                }
                acTrans.Commit();
            }
            return true;
        }

        public static DBDictionary getOutlineExtensionDictionary(Polyline acPline)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    DBDictionary extDict = acTrans.GetObject(acPline.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
                    if (extDict == null)
                    {
                        acEditor.WriteMessage("\nError, outline does not have an extension dictionary!");
                        acTrans.Commit();
                        return null;
                    }
                    else
                    {
                        acTrans.Commit();
                        return extDict;
                    }

                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError retrieving extension dictionary!\n");
                    acTrans.Commit();
                    return null;
                }
            }
        }


        public static bool addXrecord(ObjectId dbObjectId, string key, ResultBuffer data)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    DBObject acDbObj = acTrans.GetObject(dbObjectId, OpenMode.ForRead);
                    DBDictionary acExtDict = (DBDictionary)acTrans.GetObject(acDbObj.ExtensionDictionary, OpenMode.ForWrite);
                    Xrecord xrecFFL = new Xrecord();
                    // ResultBuffer xrecData = new ResultBuffer();
                    xrecFFL.Data = data;
                    acExtDict.SetAt(key, xrecFFL);
                    acTrans.AddNewlyCreatedDBObject(xrecFFL, true);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError adding Xrecord!\n");
                    acTrans.Dispose();
                    acTrans.Commit();
                    return false;
                }
                acTrans.Commit();
            }
            return true;
        }

        public static bool renameXrecord(ObjectId acDbObjId, string oldXrecKey, string newXrecKey, bool deleteOldKey)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    DBObject acDbObj = acTrans.GetObject(acDbObjId, OpenMode.ForRead);
                    DBDictionary acExtDict = (DBDictionary)acTrans.GetObject(acDbObj.ExtensionDictionary, OpenMode.ForWrite);
                    ObjectId oldXrecId = acExtDict.GetAt(oldXrecKey);
                    Xrecord oldXrec = acTrans.GetObject(oldXrecId, OpenMode.ForRead) as Xrecord;
                    ResultBuffer oldXrecData = oldXrec.Data;
                    Xrecord newXrec = new Xrecord();
                    newXrec.Data = oldXrecData;
                    acExtDict.SetAt(newXrecKey, newXrec);
                    acTrans.AddNewlyCreatedDBObject(newXrec, true);
                    if (deleteOldKey)
                    {
                        acExtDict.Remove(oldXrecId);
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError renaming Xrecord!\n");
                    // acTrans.Dispose();
                    acTrans.Commit();
                    return false;
                }
                acTrans.Commit();
            }
            return true;
        }

        public static double? getFFL(ObjectId acDbObjId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;
            double? FFLAsDouble = 0.0;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    DBObject acDbObj = acTrans.GetObject(acDbObjId, OpenMode.ForRead);
                    DBDictionary acExtDict = (DBDictionary)acTrans.GetObject(acDbObj.ExtensionDictionary, OpenMode.ForRead);
                    ObjectId fflXrecId = acExtDict.GetAt("FFL");
                    if (fflXrecId == ObjectId.Null)                  // FFL Xrecord does not exist
                    {
                       FFLAsDouble = null;
                    }
                    else
                    {
                        Xrecord fflXrec = acTrans.GetObject(fflXrecId, OpenMode.ForRead) as Xrecord;
                        TypedValue[] fflXrecData = fflXrec.Data.AsArray();
                        FFLAsDouble = Convert.ToDouble(fflXrecData[0].Value);
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError getting FFL!\n");
                }
                acTrans.Commit();
                return FFLAsDouble;
            }
        }

        public static ResultBuffer getXrecord(ObjectId extDictObjectId, string key)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    DBDictionary extDict = acTrans.GetObject(extDictObjectId, OpenMode.ForWrite) as DBDictionary;
                    ObjectId xRecId = extDict.GetAt(key);
                    Xrecord xrec = acTrans.GetObject(xRecId, OpenMode.ForRead) as Xrecord;
                    acTrans.Commit();
                    return xrec.Data;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError retrieving Xrecord data!\n");
                    acTrans.Commit();
                    return null;
                }
            }
        }

        public static ObjectIdCollection createObjectIdCollection(ObjectId firstObjectId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    ObjectIdCollection FFLObjectIds = new ObjectIdCollection();
                    // Add the outline object Id
                    FFLObjectIds.Add(firstObjectId);
                    acTrans.Commit();
                    return FFLObjectIds;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError creating object collection!\n");
                    acTrans.Abort();
                    return null;
                }
            }
        }

        public static bool CreateNewGroup(ObjectIdCollection FFLObjectIdCollection)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    Group newGroup = new Group("JPP_APP_Group", true);
                    // Generate the new group name based on the 'Next_Group_Index' Xrecord. First
                    // retrieve the next group record
                    DBDictionary nod = acTrans.GetObject(HostApplicationServices.WorkingDatabase.NamedObjectsDictionaryId,
                                                                    OpenMode.ForRead) as DBDictionary;
                    ObjectId JPPAppConfigDictId = nod.GetAt(JPP_App_Config_Params.JPP_APP_CONFIG_DATA);
                    DBDictionary JPPAppConfigDict = acTrans.GetObject(JPPAppConfigDictId, OpenMode.ForRead) as DBDictionary;
                    if (JPPAppConfigDict == null)
                    {
                        acEditor.WriteMessage("\nError cannot retrieve the JPP App Configuration Dictionary");
                        return false;
                    }
                    ResultBuffer resBuff = getXrecord(JPPAppConfigDictId, JPP_App_Config_Params.JPP_APP_NEXT_GROUP_INDEX);
                    TypedValue[] xRecData = resBuff.AsArray();
                    string newGroupName = "Outline_Group_" + xRecData[0].Value.ToString();
                    DBDictionary groupDictionary = (DBDictionary)acTrans.GetObject(acCurrDb.GroupDictionaryId, OpenMode.ForRead);
                    // Check the group name doesn't exist
                    if (groupDictionary.Contains(newGroupName))
                    {
                        acEditor.WriteMessage("\nError new group already exists.");
                        return false;
                    }
                    groupDictionary.UpgradeOpen();
                    // Add the new group to the dictionary
                    groupDictionary.SetAt(newGroupName, newGroup);
                    acTrans.AddNewlyCreatedDBObject(newGroup, true);
                    // Open the modelspace block table
                    BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                                    OpenMode.ForWrite) as BlockTableRecord;
                    newGroup.InsertAt(0, FFLObjectIdCollection);
                    // Increment Next_Group_Index and write back to the Xrecord
                    ResultBuffer resBuffupdated = new ResultBuffer();
                    resBuffupdated.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32,
                                                        Convert.ToInt32(xRecData[0].Value) + 1));
                    Xrecord updatedXrec = new Xrecord();
                    updatedXrec.Data = resBuffupdated;
                    JPPAppConfigDict.SetAt(JPP_App_Config_Params.JPP_APP_NEXT_GROUP_INDEX, updatedXrec);
                    acTrans.Commit();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError creating new group!\n");
                    acTrans.Abort();
                    return false;
                }
            }
        }

        public static void EraseEntity(ObjectId entityToEraseId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    Entity entityToErase = acTrans.GetObject(entityToEraseId, OpenMode.ForWrite) as Entity;
                    entityToErase.Erase(true);
                    acTrans.Commit();
                    return;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError erasing object!\n");
                    acTrans.Abort();
                    return;
                }
            }
        }
        public static void EraseObjectsCollection(ObjectIdCollection objectIdCollection)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    foreach (ObjectId acObjectID in objectIdCollection)
                    {
                        DBObject acDbObject = acTrans.GetObject(acObjectID, OpenMode.ForWrite);
                        acDbObject.Erase();
                    }
                    acTrans.Commit();
                    acEditor.Regen();
                    return;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError erasing objects!\n");
                    acTrans.Abort();
                    return;
                }

            }
        }

        public static bool CreateNewBlock(ObjectIdCollection FFLObjectIdCollection)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    // Generate the new block name based on the JPP_APP_NEW_BLOCK_PREFIX and the
                    // Xrecord Next_Block_Index stored in the JPP_App_Config_Data dictionary. 
                    // 
                    // Retrieve the "Next_Block_Index"
                    //
                    // Get object id of the JPP_App_Config_Data dictionary
                    DBDictionary nod = acTrans.GetObject(acCurrDb.NamedObjectsDictionaryId, OpenMode.ForWrite) as DBDictionary;
                    ObjectId JPPAppConfigDataId = nod.GetAt(JPP_App_Config_Params.JPP_APP_CONFIG_DATA);
                    DBDictionary JPPAppConfigData = acTrans.GetObject(JPPAppConfigDataId, OpenMode.ForRead) as DBDictionary;
                    ObjectId xrecId = JPPAppConfigData.GetAt(JPP_App_Config_Params.JPP_APP_NEXT_BLOCK_INDEX);
                    Xrecord xrec = acTrans.GetObject(xrecId, OpenMode.ForRead) as Xrecord;
                    TypedValue[] xrecData = xrec.Data.AsArray();
                    string newBlockName = JPP_App_Config_Params.JPP_APP_NEW_BLOCK_PREFIX + xrecData[0].Value.ToString();
                     
                    // The base point for the new block reference will be vertex 0 of the polyline 
                    // so find its coordinates.
                    Point3d blockRefPoint = new Point3d();
                    foreach (ObjectId objectId in FFLObjectIdCollection)
                    {
                        Entity entToAdd = acTrans.GetObject(objectId, OpenMode.ForRead) as Entity;
                        // newBlockTblRec.AppendEntity(entToAdd);
                        // acTrans.AddNewlyCreatedDBObject(entToAdd, true);
                        if (entToAdd is Polyline)
                        {
                            Polyline acPline = entToAdd as Polyline;
                            blockRefPoint = acPline.GetPoint3dAt(0);
                        }
                    }

                    ObjectId newBlockId = ObjectId.Null;
                    using (Transaction acTrans2 = acCurrDb.TransactionManager.StartTransaction())
                    {
                        BlockTable acBlkTbl2 = acTrans2.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        // Check the block name doesn't exist
                        if (acBlkTbl2.Has(newBlockName))
                        {
                            acEditor.WriteMessage("\nError new block already exists.");
                            return false;
                        }
                        acBlkTbl2.UpgradeOpen();
                        BlockTableRecord newBlockTblRec = new BlockTableRecord();
                        newBlockTblRec.Name = newBlockName;
                        newBlockTblRec.Origin = blockRefPoint;
                        acBlkTbl2.Add(newBlockTblRec);
                        acTrans2.AddNewlyCreatedDBObject(newBlockTblRec, true);
                        newBlockId = acBlkTbl2[newBlockName];
                        acTrans2.Commit();
                    }
                    // Copy the entities to the block using deepclone
                    IdMapping acMapping = new IdMapping();
                    acCurrDb.DeepCloneObjects(FFLObjectIdCollection, newBlockId, acMapping, false);
                    
                    // Erase the objects used to create the block
                    EraseObjectsCollection(FFLObjectIdCollection);
                    
                    // Add a block reference to model space. 
                    BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                                    OpenMode.ForWrite) as BlockTableRecord;
                    BlockReference newBlockRef = new BlockReference(blockRefPoint, newBlockId);
                    acBlkTblRec.AppendEntity(newBlockRef);
                    acTrans.AddNewlyCreatedDBObject(newBlockRef, true);
                    
                    // Increment Next_Block_Index and write back to the Xrecord
                    Xrecord updatedXrec = new Xrecord();
                    updatedXrec.Data = new ResultBuffer(new TypedValue((int)DxfCode.Int16, 
                                                            Convert.ToInt16(xrecData[0].Value) + (Int16)1));
                    JPPAppConfigData.UpgradeOpen();       // Open the JPP App Config Data dictionary for write
                    JPPAppConfigData.SetAt(JPP_App_Config_Params.JPP_APP_NEXT_BLOCK_INDEX, updatedXrec);
                    acTrans.AddNewlyCreatedDBObject(updatedXrec, true);
                    acTrans.Commit();
                    // acEditor.Regen();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError creating new block!\n");
                    acTrans.Abort();
                    return false;
                }
            }
        }
    }
}
