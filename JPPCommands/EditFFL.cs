using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace JPPCommands
{
    public static partial class EditFFL
    {
        public static bool EditFFLOrLevels()
        {
            // Add comment to explain what following code does
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            ObjectId FFLToEditId = getFFLToEdit();
            if (FFLToEditId == null)
            {
                return false;
            }
            bool userEditing = true;
            while (userEditing)
            {
                // Ask user if he wants to edit the FFL or the levels
                PromptStringOptions strOptions = new PromptStringOptions("\nEdit FLL or level(F or L, X to finish)? ");
                PromptResult userChoice = acEditor.GetString(strOptions);
                if (userChoice.Status == PromptStatus.Cancel)
                    userEditing = false;
                else if (userChoice.Status == PromptStatus.Error)
                {
                    acEditor.WriteMessage("\nError selecting what to edit.");
                    return false;
                }
                else
                {
                    switch (userChoice.StringResult.ToUpper())
                    {
                        case "F":
                            // Call the edit FFL value function
                            if (!EditFFLValue(FFLToEditId))
                            {
                                return false;
                            }
                            break;
                        case "L":
                            if (!EditLevels(FFLToEditId))
                            {
                                return false;
                            }
                            break;
                        case "X":
                            userEditing = false;
                            break;
                        default:
                            acEditor.WriteMessage("\nPlease enter 'f' or 'l' or 'x' to finish.");
                            break;
                    }
                }
            }
            return true;


        }
        public static ObjectId getFFLToEdit()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            // Prompt user for FFL to edit
            bool ValidCommand = false;
            // string SubCommand = "";
            while (ValidCommand == false)
            {
                // Create the selection filter
                TypedValue[] selFilterList = new TypedValue[2];
                selFilterList[0] = new TypedValue(0, "INSERT");
                selFilterList[1] = new TypedValue(2, JPP_App_Config_Params.JPP_APP_NEW_BLOCK_PREFIX + "*");

                SelectionFilter selFilter = new SelectionFilter(selFilterList);
                PromptSelectionOptions selOptions = new PromptSelectionOptions();

                selOptions.MessageForAdding = "Select FFL to edit: ";
                selOptions.SinglePickInSpace = true;
                selOptions.SingleOnly = true;

                // Prompt user to select FFL to edit
                PromptSelectionResult selResult = acEditor.GetSelection(selOptions, selFilter);
                // Check if the ESC key has pressed
                if (selResult.Status == PromptStatus.Cancel)
                {
                    acEditor.WriteMessage("ESC key pressed!\n");
                    return ObjectId.Null;
                }
                else if (selResult.Status == PromptStatus.Error)
                {
                    acEditor.WriteMessage("\nError selecting FFL to edit.");
                    return ObjectId.Null;
                }
                if (selResult.Value.Count == 1)
                {
                    ObjectId[] FFLToEditId = selResult.Value.GetObjectIds();
                    return FFLToEditId[0];
                }
                acEditor.WriteMessage("\nPlease select only one FFL to edit.....");
            }
            return ObjectId.Null;
        }

        public static bool EditFFLValue(ObjectId fflToEditId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor acEditor = acDoc.Editor;

            PromptDoubleResult promptFFLDouble = acEditor.GetDouble("\nEnter the new FFL: ");
            if (promptFFLDouble.Status == PromptStatus.OK)
            {
                return EditFFLValue(fflToEditId, promptFFLDouble.Value);
            }
            else if (promptFFLDouble.Status == PromptStatus.Cancel)
            {
                acEditor.WriteMessage("\nEdit FFL cancelled.");
                return false;
            }
            else
            {
                acEditor.WriteMessage("\nError entering new FFL.");
                return false;
            }
        }

        public static bool EditFFLValue(ObjectId fflToEditId, double level)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    // Prompt user for new FFL
                    double? newFFL = 0.0;   // Declare here to ensure in scope for later
                    
                        BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockReference fflBlock = acTrans.GetObject(fflToEditId, OpenMode.ForRead) as BlockReference;
                        BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[fflBlock.Name], 
                                                                            OpenMode.ForWrite) as BlockTableRecord;
                        if (acBlkTblRec == null)
                        {
                            acEditor.WriteMessage("\nError FFL block not found.");
                            return false;
                        }
                        // Check there is only one instance of this block reference in the drawing
                        if (acBlkTblRec.GetBlockReferenceIds(false, true).Count != 1)
                        {
                            acEditor.WriteMessage("\nError more than one instance of the block reference.");
                            return false;
                        }

                        // Iterate around the object collection to find the polyline to retrieve the current FFL value
                        newFFL = level;
                        double? fflDiff = 0.0;
                        foreach (ObjectId objId in acBlkTblRec)
                        {
                            // Fetch the object
                            Object fflObj = acTrans.GetObject(objId, OpenMode.ForWrite);
                            double? currFFL = 0.0;
                            if (fflObj.GetType() == typeof(Polyline))
                            {
                                Polyline acPline = fflObj as Polyline;

                                // Check the outline has an extension dictionary
                                DBDictionary acExtDict = (DBDictionary)acTrans.GetObject(acPline.ExtensionDictionary, 
                                                                                                OpenMode.ForRead);
                                if (acExtDict == null)
                                {
                                    acEditor.WriteMessage("\nError cannot retrieve the extension dictionary of polyline.");
                                    return false;
                                }
                                currFFL = JPPUtils.getFFL(acPline.Id);
                                if (currFFL == null)
                                {
                                    acEditor.WriteMessage("\nError retrieving current FFL value.");
                                    return false;
                                }
                                fflDiff = newFFL - currFFL;
                                // Update FFL Xrecord
                                ResultBuffer resBuff = new ResultBuffer();
                                resBuff.Add(new TypedValue((int)DxfCode.ExtendedDataReal, newFFL));
                                // Overwrite the Xrecord with new data
                                if (!JPPUtils.addXrecord(objId, "FFL", resBuff))
                                {
                                    acEditor.WriteMessage("\nError updating Xrecord with new FFL value.");
                                    return false;
                                }
                                // Update the levels Xrecords to reflect the new ffl
                                for (int vertexIndex = 0; vertexIndex < acPline.NumberOfVertices; vertexIndex++)
                                {
                                    // Access Xrecord for the vertex and update the level value
                                    ObjectId vertexXrecId = acExtDict.GetAt("Vertex_" + vertexIndex.ToString());
                                    if (vertexXrecId == null)
                                    {
                                        acEditor.WriteMessage("\nError cannot retrieve vertex Xrecord");
                                        acTrans.Abort();
                                        return false;
                                    }
                                    Xrecord vertexXrec = acTrans.GetObject(vertexXrecId, OpenMode.ForWrite) as Xrecord;
                                    TypedValue[] vertexXrecData = vertexXrec.Data.AsArray();
                                    // Get the current level
                                    double currLevel = Convert.ToDouble(vertexXrecData[3].Value);
                                    ResultBuffer newXrecData = new ResultBuffer();
                                    newXrecData.Add(vertexXrecData[0]);
                                    newXrecData.Add(vertexXrecData[1]);
                                    newXrecData.Add(vertexXrecData[2]);
                                    newXrecData.Add(new TypedValue((int)DxfCode.ExtendedDataReal, currLevel + fflDiff));
                                    // Overwrite the data in the Xrecord with new data
                                    vertexXrec.Data = newXrecData;
                                }
                                break;
                            }
                        }
                        // Iterate around the MText objects of the block to update the FFL text and
                        // levels text
                        foreach (ObjectId objId in acBlkTblRec)
                        {
                            // Fetch the object
                            Object fflObj = acTrans.GetObject(objId, OpenMode.ForWrite);
                            if (fflObj.GetType() == typeof(MText))
                            {
                                MText fflMText = (MText)fflObj;
                                if (fflMText.Text.StartsWith("FFL"))
                                {
                                    double dblNewFFL = (double)newFFL;
                                    fflMText.Contents = "FFL " + dblNewFFL.ToString("N3");
                                }
                                else
                                {
                                    double currLevel = Convert.ToDouble(fflMText.Contents);
                                    double newLevel = currLevel + (double)fflDiff;
                                    fflMText.Contents = newLevel.ToString("N3");
                                }
                            }
                        }
                        // Update the graphics
                        fflBlock.UpgradeOpen();
                        fflBlock.RecordGraphicsModified(true);
                        acTrans.Commit();
                        return true;
                    
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError editing FFL value!\n");
                    return false;
                }
            }
            // return true;
        }

        public static ObjectIdCollection explodeFFL(BlockReference blockToExplode)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    // explode the block
                    DBObjectCollection fflEntities = new DBObjectCollection();
                    blockToExplode.Explode(fflEntities);

                    // Erase the block
                    blockToExplode.UpgradeOpen();
                    blockToExplode.Erase();

                    // Add the entities back into modelspace
                    BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                                        OpenMode.ForWrite) as BlockTableRecord;
                    ObjectIdCollection newEntsIDs = new ObjectIdCollection();
                    foreach (DBObject obj in fflEntities)
                    {
                        Entity newEnt = (Entity)obj;
                        acBlkTblRec.AppendEntity(newEnt);
                        acTrans.AddNewlyCreatedDBObject(newEnt, true);
                        newEntsIDs.Add(newEnt.Id);
                    }
                    acTrans.Commit();
                    return newEntsIDs;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError exploding FFL block!\n");
                    acTrans.Commit();
                    return null;
                }
            }
        }
    }
}
