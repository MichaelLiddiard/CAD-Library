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
    public class JPPCommandsInitialisation
    {
        public static bool JPPCommandsInitialise()
        {
            // Check whether JPP App Text Style exists and add if not
            if (!setJPPTextStyle())
                return false;
            // Check whether JPP App layers exist and add if not
            if (!setJPPLayers())
                return false;
            // Check the JPP App Configuration Dictionary exists and create if not
            if (!configJPPApp())
                return false;
            if (!AddRegAppId())
                return false;
            return true;
        }

        public static bool setJPPTextStyle()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    // Check JPP text style exists and create if not
                    TextStyleTable acTxtStyleTbl = acTrans.GetObject(acCurrDb.TextStyleTableId,
                                                                            OpenMode.ForRead, false) as TextStyleTable;
                    TextStyleTableRecord jppTxtStyleRec = new TextStyleTableRecord();
                    if (!acTxtStyleTbl.Has(StyleNames.JPP_App_Text_Style))
                    {
                        // JPP text style doesn't exist so create
                        acTxtStyleTbl.UpgradeOpen();
                        jppTxtStyleRec.Name = StyleNames.JPP_App_Text_Style;
                        jppTxtStyleRec.TextSize = 0.0;
                        jppTxtStyleRec.ObliquingAngle = 0.0;
                        // jppTxtStyleRec.Font = new FontDescriptor("Arial", false, false, 0, 0);
                        jppTxtStyleRec.FileName = "Arial";
                        acTxtStyleTbl.Add(jppTxtStyleRec);
                        acTrans.AddNewlyCreatedDBObject(jppTxtStyleRec, true);
                    }
                    jppTxtStyleRec = acTrans.GetObject(acTxtStyleTbl[StyleNames.JPP_App_Text_Style], OpenMode.ForRead) as TextStyleTableRecord;
                    acCurrDb.Textstyle = jppTxtStyleRec.ObjectId;
                    acTrans.Commit();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError setting JPP Text Style!\n");
                    acTrans.Commit();
                    return false;
                }
            }
        }

        public static bool setJPPLayers()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable acLayerTable = acTrans.GetObject(acCurrDb.LayerTableId, OpenMode.ForWrite) as LayerTable;
                    if (acLayerTable.Has(StyleNames.JPP_APP_FFLs_Layer) == false)
                    {
                        LayerTableRecord acLayerTableRecLevels = new LayerTableRecord();
                        acLayerTableRecLevels.Color = Color.FromColorIndex(ColorMethod.ByAci, 2);
                        acLayerTableRecLevels.Name = StyleNames.JPP_APP_FFLs_Layer;
                        acLayerTable.Add(acLayerTableRecLevels);
                        acTrans.AddNewlyCreatedDBObject(acLayerTableRecLevels, true);
                    }
                    if (acLayerTable.Has(StyleNames.JPP_APP_Levels_Layer) == false)
                    {
                        LayerTableRecord acLayerTableRecLevels = new LayerTableRecord();
                        acLayerTableRecLevels.Color = Color.FromColorIndex(ColorMethod.ByAci, 2);
                        acLayerTableRecLevels.Name = StyleNames.JPP_APP_Levels_Layer;
                        acLayerTable.Add(acLayerTableRecLevels);
                        acTrans.AddNewlyCreatedDBObject(acLayerTableRecLevels, true);
                    }
                    if (acLayerTable.Has(StyleNames.JPP_App_Outline_Layer) == false)
                    {
                        LayerTableRecord acLayerTableRecOutline = new LayerTableRecord();
                        acLayerTableRecOutline.Color = Color.FromColorIndex(ColorMethod.ByAci, 2);
                        acLayerTableRecOutline.Name = StyleNames.JPP_App_Outline_Layer;
                        acLayerTable.Add(acLayerTableRecOutline);
                        acTrans.AddNewlyCreatedDBObject(acLayerTableRecOutline, true);
                    }
                    if (acLayerTable.Has(StyleNames.JPP_App_Exposed_Brick_Layer) == false)
                    {
                        LayerTableRecord acLayerTableRecOutline = new LayerTableRecord();
                        acLayerTableRecOutline.Color = Color.FromColorIndex(ColorMethod.ByAci, 2);
                        acLayerTableRecOutline.Name = StyleNames.JPP_App_Exposed_Brick_Layer;
                        acLayerTable.Add(acLayerTableRecOutline);
                        acTrans.AddNewlyCreatedDBObject(acLayerTableRecOutline, true);
                    }
                    if (acLayerTable.Has(StyleNames.JPP_App_Tanking_Layer) == false)
                    {
                        LayerTableRecord acLayerTableRecOutline = new LayerTableRecord();
                        acLayerTableRecOutline.Color = Color.FromColorIndex(ColorMethod.ByAci, 2);
                        acLayerTableRecOutline.Name = StyleNames.JPP_App_Tanking_Layer;
                        acLayerTable.Add(acLayerTableRecOutline);
                        acTrans.AddNewlyCreatedDBObject(acLayerTableRecOutline, true);
                    }
                    acTrans.Commit();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError adding JPP App layers!\n");
                    acTrans.Commit();
                    return false;
                }
            }
        }

        public static bool configJPPApp()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    // Check if dictionary exists. If not create the dictionary and add the Xrecord..
                    //
                    // name:        Next_Group_Index
                    // Integer:     n
                    //
                    DBDictionary nod = acTrans.GetObject(acCurrDb.NamedObjectsDictionaryId, OpenMode.ForWrite) as DBDictionary;
                    // Check if JPP App data entry already exists
                    DBDictionary jppAppDict = new DBDictionary();
                    if (!nod.Contains(JPP_App_Config_Params.JPP_APP_CONFIG_DATA))
                    {
                        // Create the JPP App Dictionary
                        nod.SetAt(JPP_App_Config_Params.JPP_APP_CONFIG_DATA, jppAppDict);
                        acTrans.AddNewlyCreatedDBObject(jppAppDict, true);                      
                        // JPP App data entry doesn't exist so add
                        // Create the Xrecord
                        Xrecord xrecNextBlockIndex = new Xrecord();
                        xrecNextBlockIndex.Data = new ResultBuffer(
                                                    new TypedValue((int)DxfCode.Int16, 0));      // Initial index is 0
                        // Create the NOD entry
                        jppAppDict.SetAt(JPP_App_Config_Params.JPP_APP_NEXT_BLOCK_INDEX, xrecNextBlockIndex);
                        acTrans.AddNewlyCreatedDBObject(xrecNextBlockIndex, true);

                        // Debug code
                        ObjectId debugJPPAppConfigDictId = nod.GetAt(JPP_App_Config_Params.JPP_APP_CONFIG_DATA);
                        DBDictionary debugJPPAppConfigDict = acTrans.GetObject(debugJPPAppConfigDictId, OpenMode.ForRead) as DBDictionary;
                        ObjectId xrecId = debugJPPAppConfigDict.GetAt(JPP_App_Config_Params.JPP_APP_NEXT_BLOCK_INDEX);
                        Xrecord debugXrec = acTrans.GetObject(xrecId, OpenMode.ForRead) as Xrecord;
                       
                        acTrans.Commit();
                    }
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError adding JPP App configuration dictionary!\n");
                    acTrans.Commit();
                    return false;
                }
            }
        }

        public static bool AddRegAppId()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    RegAppTable acRegAppTbl = acTrans.GetObject(acCurrDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                    if (!acRegAppTbl.Has(JPP_App_Config_Params.JPP_APP_NAME))
                    {
                        using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                        {
                            acRegAppTblRec.Name = JPP_App_Config_Params.JPP_APP_NAME;
                            acRegAppTbl.UpgradeOpen();
                            acRegAppTbl.Add(acRegAppTblRec);
                            acTrans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                        }
                        acTrans.Commit();
                    }
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError registering JPP App!");
                    acTrans.Commit();
                    return false;
                }
            }
        }
    }
}
   
