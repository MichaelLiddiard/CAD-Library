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
    public static partial class EditFFL
    {
        public static bool EditLevels(ObjectId fflToEditId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            bool userEditing = true;
            while (userEditing)
            {
                // Ask user if he wants to edit the level values or move levels
                PromptStringOptions strOptions = new PromptStringOptions("\nEdit, move or delete level (E, M, D or X to finish)? ");
                PromptResult userChoice = acEditor.GetString(strOptions);
                if (userChoice.Status == PromptStatus.Cancel)
                {
                    return true;
                }
                else if (userChoice.Status == PromptStatus.Error)
                {
                    acEditor.WriteMessage("\nError selecting what to edit.");
                    return false;
                }
                else
                {
                    switch (userChoice.StringResult.ToUpper())
                    {
                        case "E":
                            // Call the edit level value function
                            if (!EditLevelValue(fflToEditId))
                            {
                                return false;
                            }
                            break;
                        case "M":
                            if (!MoveLevelText(fflToEditId))
                            {
                                return false;
                            }
                            break;
                        case "D":
                            if (!DeleteLevelText(fflToEditId))
                            {
                                return false;
                            }
                            break;
                        case "X":
                            userEditing = false;
                            break;
                        default:
                            acEditor.WriteMessage("\nPlease enter 'E' or 'M' or 'X' to finish.");
                            break;
                    }
                }

            }
            return true;
        }

        private static bool DeleteLevelText(ObjectId fflToEditId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            // Loop so user can delete multiple levels
            bool deletingLevels = true;
            while (deletingLevels)
            {
                // Prompt the user to select the level to edit
                PromptNestedEntityOptions nestedEntOpt = new PromptNestedEntityOptions("\nPick the level to delete (spacebar to finish):");
                PromptNestedEntityResult nestedEntRes = acEditor.GetNestedEntity(nestedEntOpt);
                if (nestedEntRes.Status == PromptStatus.OK)
                {
                    using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
                    {

                        try
                        {
                            Entity ent = acTrans.GetObject(nestedEntRes.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent.GetType() == typeof(MText))
                            {
                                MText levelToDelete = ent as MText;
                                if (levelToDelete.Layer != StyleNames.JPP_APP_Levels_Layer)
                                {
                                    acEditor.WriteMessage("\nSelected text is on the wrong layer!");
                                    continue;
                                }
                                levelToDelete.UpgradeOpen();
                                levelToDelete.Erase();
                                // Update block graphics
                                BlockReference fflToEdit = acTrans.GetObject(fflToEditId, OpenMode.ForWrite) as BlockReference;
                                fflToEdit = acTrans.GetObject(fflToEditId, OpenMode.ForWrite) as BlockReference;
                                fflToEdit.RecordGraphicsModified(true);
                                acTrans.Commit();
                            }

                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception acException)
                        {
                            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                        ("The following exception was caught: \n" + acException.Message
                                                                + "\nError deleting level!\n");
                            acTrans.Abort();
                            return false;
                        }
                    }
                }
                if (nestedEntRes.Status == PromptStatus.Cancel)
                    deletingLevels = false;
            }
            return true;
        }

        private static bool EditLevelValue(ObjectId fflToEditId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            // Loop so user can edit multiple levels
            bool editingLevels = true;
            while (editingLevels)
            {
                // Prompt the user to select the level to edit
                PromptNestedEntityOptions nestedEntOpt = new PromptNestedEntityOptions("\nPick the level to edit (spacebar to finish):");
                PromptNestedEntityResult nestedEntRes = acEditor.GetNestedEntity(nestedEntOpt);
                if (nestedEntRes.Status == PromptStatus.OK)
                {
                    using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
                    {

                        try
                        {
                            Entity ent = acTrans.GetObject(nestedEntRes.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent.GetType() == typeof(MText))
                            {
                                MText levelToEdit = ent as MText;
                                if (levelToEdit.Layer != StyleNames.JPP_APP_Levels_Layer)
                                    acEditor.WriteMessage("\nSelected text is on the wrong layer!");
                                else
                                {
                                    // Fetch the Xdata for the MText - needed to update the vertex Xrecord on the outline
                                    Int32? vertexIndex = GetXdata(levelToEdit.Id);
                                    if (vertexIndex == null)
                                        acEditor.WriteMessage("\nError, unable to retrieve level vertex index!");
                                    else
                                    {
                                        // Prompt the user for the new level
                                        PromptDoubleResult newLevelValue = acEditor.GetDouble("\nEnter the new level: ");
                                        if (newLevelValue.Status == PromptStatus.OK)
                                        {
                                            double newLevel = newLevelValue.Value;
                                            // Call function to update the outline Xrecord of the block definition
                                            if (!UpdateOutline(fflToEditId, (Int32)vertexIndex, newLevel))
                                                acEditor.WriteMessage("\nError, unable to update outline with edited level!");
                                            // Update level text
                                            levelToEdit.UpgradeOpen();
                                            levelToEdit.Contents = newLevel.ToString("N3");
                                            levelToEdit.RecordGraphicsModified(true);
                                            // Update block graphics
                                            BlockReference fflToEdit = acTrans.GetObject(fflToEditId, OpenMode.ForWrite) as BlockReference;
                                            fflToEdit.RecordGraphicsModified(true);
                                        }
                                        else
                                            acEditor.WriteMessage("\nInvalid level entered!");
                                    }
                                    acTrans.Commit();
                                }
                            }
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception acException)
                        {
                            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                        ("The following exception was caught: \n" + acException.Message
                                                                + "\nError editing level!\n");
                            acTrans.Abort();
                            return false;
                        }
                    }
                }
                if (nestedEntRes.Status == PromptStatus.Cancel)
                    editingLevels = false;
            }
            return true;
        }

        private static Int32? GetXdata(ObjectId textObjId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            Int32? xdataValue = null;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    MText textObj = acTrans.GetObject(textObjId, OpenMode.ForRead) as MText;
                    ResultBuffer xdata = textObj.GetXDataForApplication("JPP_App");
                    if (xdata == null)
                    {
                        xdataValue = null;
                    }
                    TypedValue[] xdataArray = xdata.AsArray();
                    foreach (TypedValue typedValue in xdataArray)
                    {
                        switch ((DxfCode)typedValue.TypeCode)
                        {
                            case DxfCode.ExtendedDataRegAppName:
                                if (typedValue.Value.ToString() != "JPP_App")
                                    xdataValue = null;
                                break;
                            case DxfCode.ExtendedDataInteger32:
                                xdataValue = (Int32)typedValue.Value;
                                break;
                            default:
                                xdataValue = null;
                                break;
                        }
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                        + "\nError getting Xdata for level!\n");
                    xdataValue = null;
                }
                acTrans.Commit();
            }
            return xdataValue;
        }

        public static bool UpdateOutline(ObjectId blockRefId, int vertexIndex, double newLevel)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId,
                                                                    OpenMode.ForRead) as BlockTable;
                BlockReference blockReference = acTrans.GetObject(blockRefId,
                                                                    OpenMode.ForRead) as BlockReference;
                BlockTableRecord blockDefinition = acTrans.GetObject(acBlkTbl[blockReference.Name],
                                                                    OpenMode.ForWrite) as BlockTableRecord;
                if (blockDefinition == null)
                {
                    acEditor.WriteMessage("\nError cannot retrieve block definition to update level data.");
                    return false;
                }

                // Check there is only one instance of this block reference in the drawing
                if (blockDefinition.GetBlockReferenceIds(false, true).Count != 1)
                {
                    acEditor.WriteMessage("\nError more than one instance of the block reference.");
                    return false;
                }

                // Iterate around the object collection of the block definition to find the polyline
                foreach (ObjectId objId in blockDefinition)
                {
                    // Fetch the object
                    Object blockEnt = acTrans.GetObject(objId, OpenMode.ForWrite);
                    if (blockEnt.GetType() == typeof(Polyline))
                    {
                        Polyline acPline = blockEnt as Polyline;
                        // Retrieve the extension dictionary for the polyline
                        DBObject acDbObj = acTrans.GetObject(acPline.Id, OpenMode.ForRead);
                        DBDictionary acExtDict = (DBDictionary)acTrans.GetObject(acDbObj.ExtensionDictionary, OpenMode.ForRead);
                        if (acExtDict == null)
                        {
                            acEditor.WriteMessage("\nError cannot retrieve the extension dictionary of polyline.");
                            acTrans.Abort();
                            return false;
                        }
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
                        ResultBuffer newXrecData = new ResultBuffer();
                        newXrecData.Add(vertexXrecData[0]);
                        newXrecData.Add(vertexXrecData[1]);
                        newXrecData.Add(vertexXrecData[2]);
                        newXrecData.Add(new TypedValue((int)DxfCode.ExtendedDataReal, newLevel));
                        // Overwrite the data in the Xrecord with new data
                        vertexXrec.Data = newXrecData;
                    }
                }
                acTrans.Commit();
            }
            return true;
        }

        private static bool MoveLevelText(ObjectId fflToEditId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;


            // Loop so user can move multiple levels
            bool movingLevels = true;
            while (movingLevels)
            {
                // Prompt the user to select the level to move
                PromptNestedEntityOptions nestedEntOpt = new PromptNestedEntityOptions("\nPick the level to move (spacebar to finish):");
                PromptNestedEntityResult nestedEntRes = acEditor.GetNestedEntity(nestedEntOpt);
                if (nestedEntRes.Status == PromptStatus.OK)
                {
                    using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
                    {

                        try
                        {
                            Entity ent = acTrans.GetObject(nestedEntRes.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent.GetType() == typeof(MText))
                            {
                                MText textToEdit = ent as MText;

                                // All work will be done in the WCS so save the current UCS
                                // to restore later and set the UCS to WCS
                                Matrix3d CurrentUCS = acEditor.CurrentUserCoordinateSystem;
                                acEditor.CurrentUserCoordinateSystem = Matrix3d.Identity;

                                // Fetch the block reference for the outline
                                BlockReference fflToEdit = acTrans.GetObject(fflToEditId, OpenMode.ForWrite) as BlockReference;

                                // Calculate the corner coordinates. Text rotation will either be 0 to 90 degrees or
                                // 270 - 360 degrees. 
                                // What happens if text is not in these ranges?
                                // Point3d textLocation = textToEdit.Location.TransformBy(fflToEdit.BlockTransform);
                                Point3d cornerPoint = CalculateCornerPoint(textToEdit, fflToEdit);

                                // Save the current text rotation angle for use later
                                double oldTextAngle = textToEdit.Rotation;

                                // Create a new UCS basesd on the corner point and the text rotation. The new UCS
                                // origin will be at the corner point and the X-axis aligned with the text direction.
                                if (!SwitchUCS(cornerPoint, textToEdit.Rotation))
                                {
                                    acEditor.WriteMessage("\nError, unable to create UCS!");
                                    acTrans.Abort();
                                    // Restore current UCS
                                    acEditor.CurrentUserCoordinateSystem = CurrentUCS;
                                    return false;
                                }
                                // Prompt the user for the new position
                                Point3d newLocation = new Point3d();                // Declare here so in scope
                                AttachmentPoint newJustification = new AttachmentPoint();
                                double newAngle = 0.0;
                                PromptPointResult promptRes;
                                PromptPointOptions promptOpts = new PromptPointOptions("\nClick new text position: ");

                                promptRes = acEditor.GetPoint(promptOpts);
                                if (promptRes.Status != PromptStatus.OK)
                                {
                                    acEditor.WriteMessage("\nInvalid level text position picked - please try again.");
                                    acTrans.Abort();
                                    // Restore current UCS
                                    acEditor.CurrentUserCoordinateSystem = CurrentUCS;
                                    continue;
                                }
                                // If the text is justified TC or BC it's on a straight line so move to the other side, swap the
                                // justification and maintain the same rotation
                                if ((textToEdit.Attachment == AttachmentPoint.TopCenter) 
                                                    || (textToEdit.Attachment == AttachmentPoint.BottomCenter))
                                {
                                    newAngle = Constants.Deg_0;
                                    if (promptRes.Value.Y > 0)
                                    {
                                        newLocation = new Point3d(0.0, Constants.TextOffset, 0.0);
                                        newJustification = AttachmentPoint.BottomCenter;
                                    }
                                    else
                                    {
                                        newLocation = new Point3d(0.0, -Constants.TextOffset, 0.0);
                                        newJustification = AttachmentPoint.TopCenter;
                                    }
                                }
                                else
                                {
                                    // Ask user if text needs to be rotated.
                                    PromptKeywordOptions promptKeyOpts = new PromptKeywordOptions("\nRotate text [Yes/No]: ", "Yes No");
                                    promptKeyOpts.Keywords.Default = "No";
                                    PromptResult promptRotRes = acEditor.GetKeywords(promptKeyOpts);
                                    if (promptRotRes.Status != PromptStatus.OK)
                                    {
                                        acEditor.WriteMessage("\nInvalid option - please try again.");
                                        acTrans.Abort();
                                        // Restore current UCS
                                        acEditor.CurrentUserCoordinateSystem = CurrentUCS;
                                        continue;
                                    }

                                    // Calculate the new location point and text justification
                                    if (promptRes.Value.X < 0)
                                    {
                                        if (promptRes.Value.Y < 0)
                                        {
                                            newLocation = new Point3d(-Constants.TextOffset * Math.Cos(Constants.Deg_45),
                                                                        -Constants.TextOffset * Math.Cos(Constants.Deg_45),
                                                                        0.0);
                                            if (promptRotRes.StringResult == "Yes")
                                            {
                                                if ((oldTextAngle >= Constants.Deg_0) && (oldTextAngle <= Constants.Deg_90))
                                                {
                                                    newJustification = AttachmentPoint.TopLeft;
                                                    newAngle = Constants.Deg_270;       // The UCS is origin currently lies at the corner point
                                                                                        // and the x-axis at the text rotation
                                                }
                                                else
                                                {
                                                    newJustification = AttachmentPoint.BottomRight;
                                                    newAngle = Constants.Deg_90;
                                                }
                                            }
                                            else
                                            {
                                                newJustification = AttachmentPoint.TopRight;
                                                newAngle = Constants.Deg_0;
                                            }
                                        }
                                        else
                                        {
                                            newLocation = new Point3d(-Constants.TextOffset * Math.Cos(Constants.Deg_45),
                                                                        Constants.TextOffset * Math.Cos(Constants.Deg_45),
                                                                        0.0);
                                            if (promptRotRes.StringResult == "Yes")
                                            {
                                                if ((oldTextAngle >= Constants.Deg_0) && (oldTextAngle <= Constants.Deg_90))
                                                {
                                                    newJustification = AttachmentPoint.TopRight;
                                                    newAngle = Constants.Deg_270;
                                                }
                                                else
                                                {
                                                    newJustification = AttachmentPoint.BottomLeft;
                                                    newAngle = Constants.Deg_90;
                                                }
                                            }
                                            else
                                            {
                                                newJustification = AttachmentPoint.BottomRight;
                                                newAngle = Constants.Deg_0;
                                            }

                                        }
                                    }
                                    else if (promptRes.Value.X > 0)
                                    {

                                        if (promptRes.Value.Y < 0)
                                        {
                                            newLocation = new Point3d(Constants.TextOffset * Math.Cos(Constants.Deg_45),
                                                                        -Constants.TextOffset * Math.Sin(Constants.Deg_45),
                                                                            0.0);
                                            if (promptRotRes.StringResult == "Yes")
                                            {
                                                if ((oldTextAngle >= Constants.Deg_0) && (oldTextAngle <= Constants.Deg_90))
                                                {
                                                    newJustification = AttachmentPoint.BottomLeft;
                                                    newAngle = Constants.Deg_270;
                                                }
                                                else
                                                {
                                                    newJustification = AttachmentPoint.TopRight;
                                                    newAngle = Constants.Deg_90;
                                                }
                                            }
                                            else
                                            {
                                                newJustification = AttachmentPoint.TopLeft;
                                                newAngle = Constants.Deg_0;
                                            }
                                        }
                                        else
                                        {
                                            newLocation = new Point3d(Constants.TextOffset * Math.Cos(Constants.Deg_45),
                                                                        Constants.TextOffset * Math.Cos(Constants.Deg_45),
                                                                            0.0);
                                            if (promptRotRes.StringResult == "Yes")
                                            {
                                                if ((oldTextAngle >= Constants.Deg_0) && (oldTextAngle <= Constants.Deg_90))
                                                {
                                                    newJustification = AttachmentPoint.BottomRight;
                                                    newAngle = Constants.Deg_270;
                                                }
                                                else
                                                {
                                                    newJustification = AttachmentPoint.TopLeft;
                                                    newAngle = Constants.Deg_90;
                                                }
                                            }
                                            else
                                            {
                                                newJustification = AttachmentPoint.BottomLeft;
                                                newAngle = Constants.Deg_0;
                                            }

                                        }

                                    }
                                    else
                                    {
                                        acEditor.WriteMessage("\nInvalid point picked - please try again.");
                                        acTrans.Abort();
                                        // Restore current UCS
                                        acEditor.CurrentUserCoordinateSystem = CurrentUCS;
                                        continue;
                                    }
                                }
                                // Translate these points to the WCS                    
                                ViewportTableRecord acVportTblRec = acTrans.GetObject(acEditor.ActiveViewportId, OpenMode.ForWrite) as ViewportTableRecord;
                                Matrix3d jppMatrix = new Matrix3d();
                                jppMatrix = Matrix3d.AlignCoordinateSystem(Point3d.Origin,
                                                                           Vector3d.XAxis,
                                                                           Vector3d.YAxis,
                                                                           Vector3d.ZAxis,
                                                                           acVportTblRec.Ucs.Origin,
                                                                           acVportTblRec.Ucs.Xaxis,
                                                                           acVportTblRec.Ucs.Yaxis,
                                                                           acVportTblRec.Ucs.Zaxis);
                                Point3d wcsNewLoc = newLocation.TransformBy(jppMatrix);
                                // Transform the new location to coordinates relative to the block reference
                                wcsNewLoc = wcsNewLoc.TransformBy(fflToEdit.BlockTransform.Inverse());
                               // wcsNewLoc = new Point3d(wcsNewLoc.X - fflToEdit.BlockTransform.Translation.X,
                                //                        wcsNewLoc.Y - fflToEdit.BlockTransform.Translation.Y,
                                //                        wcsNewLoc.Z - fflToEdit.BlockTransform.Translation.Z);

                                // Update the text
                                textToEdit.UpgradeOpen();
                                textToEdit.Attachment = newJustification;
                                textToEdit.Location = wcsNewLoc;
                                textToEdit.Rotation = newAngle;
                                // Update block graphics
                                // BlockReference fflToEdit = acTrans.GetObject(fflToEditId, OpenMode.ForWrite) as BlockReference;
                                fflToEdit = acTrans.GetObject(fflToEditId, OpenMode.ForWrite) as BlockReference;
                                fflToEdit.RecordGraphicsModified(true);

                                acTrans.Commit();
                                // Restore current UCS
                                acEditor.CurrentUserCoordinateSystem = CurrentUCS;
                            }
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception acException)
                        {
                            Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                        ("The following exception was caught: \n" + acException.Message
                                                                + "\nError moving level!\n");
                            acTrans.Abort();
                            return false;
                        }
                    }
                }
                if (nestedEntRes.Status == PromptStatus.Cancel)
                    movingLevels = false;
            }
            return true;
        }

        private static Point3d CalculateCornerPoint(MText levelText, BlockReference fflBlockReference)
        {
            Point3d cornerPoint = new Point3d();
            // Update the levelText location with the block reference coordinates

            // Point3d transformedLocation = new Point3d(levelText.Location.X + fflBlockReference.BlockTransform.Translation.X,
            //                                              levelText.Location.Y + fflBlockReference.BlockTransform.Translation.Y,
            //                                             levelText.Location.Z + fflBlockReference.BlockTransform.Translation.Z);
            Point3d transformedLocation = levelText.Location.TransformBy(fflBlockReference.BlockTransform);
            if ((levelText.Rotation >= Constants.Deg_0) && (levelText.Rotation <= Constants.Deg_90))
            {
                switch (levelText.Attachment)
                {
                    case AttachmentPoint.BottomLeft:
                        cornerPoint = new Point3d(transformedLocation.X - Constants.TextOffset * Math.Cos(Constants.Deg_45 + levelText.Rotation),
                                                  transformedLocation.Y - Constants.TextOffset * Math.Sin(Constants.Deg_45 + levelText.Rotation),
                                                  0.0);
                        break;
                    case AttachmentPoint.BottomRight:
                        cornerPoint = new Point3d(transformedLocation.X + Constants.TextOffset * Math.Cos(Constants.Deg_45 - levelText.Rotation),
                                                  transformedLocation.Y - Constants.TextOffset * Math.Sin(Constants.Deg_45 - levelText.Rotation),
                                                  0.0);
                        break;
                    case AttachmentPoint.TopLeft:
                        cornerPoint = new Point3d(transformedLocation.X - Constants.TextOffset * Math.Cos(Constants.Deg_45 - levelText.Rotation),
                                                  transformedLocation.Y + Constants.TextOffset * Math.Sin(Constants.Deg_45 - levelText.Rotation),
                                                  0.0);
                        break;
                    case AttachmentPoint.TopRight:
                        cornerPoint = new Point3d(transformedLocation.X + Constants.TextOffset * Math.Cos(Constants.Deg_45 + levelText.Rotation),
                                                  transformedLocation.Y + Constants.TextOffset * Math.Sin(Constants.Deg_45 + levelText.Rotation),
                                                  0.0);
                        break;
                    case AttachmentPoint.BottomCenter:
                        cornerPoint = new Point3d(transformedLocation.X + Constants.TextOffset * Math.Cos(Constants.Deg_90 - levelText.Rotation),
                                                  transformedLocation.Y - Constants.TextOffset * Math.Sin(Constants.Deg_90 - levelText.Rotation),
                                                  0.0);
                        break;
                    case AttachmentPoint.TopCenter:
                        cornerPoint = new Point3d(transformedLocation.X - Constants.TextOffset * Math.Cos(Constants.Deg_90 - levelText.Rotation),
                                                  transformedLocation.Y + Constants.TextOffset * Math.Sin(Constants.Deg_90 - levelText.Rotation),
                                                  0.0);
                        break;
                    default:
                        // Error condition
                        break;
                }
                return cornerPoint;
            }
            else
            {
                switch (levelText.Attachment)
                {
                    case AttachmentPoint.BottomLeft:
                        cornerPoint = new Point3d(transformedLocation.X - Constants.TextOffset * Math.Cos(Constants.Deg_315 - levelText.Rotation),
                                                  transformedLocation.Y + Constants.TextOffset * Math.Sin(Constants.Deg_315 - levelText.Rotation),
                                                  0.0);
                        break;
                    case AttachmentPoint.BottomRight:
                        cornerPoint = new Point3d(transformedLocation.X - Constants.TextOffset * Math.Cos(levelText.Rotation - Constants.Deg_225),
                                                  transformedLocation.Y - Constants.TextOffset * Math.Sin(levelText.Rotation - Constants.Deg_225),
                                                  0.0);
                        break;
                    case AttachmentPoint.TopLeft:
                        cornerPoint = new Point3d(transformedLocation.X + Constants.TextOffset * Math.Cos(levelText.Rotation - Constants.Deg_225),
                                                  transformedLocation.Y + Constants.TextOffset * Math.Sin(levelText.Rotation - Constants.Deg_225),
                                                  0.0);
                        break;
                    case AttachmentPoint.TopRight:
                        cornerPoint = new Point3d(transformedLocation.X + Constants.TextOffset * Math.Cos(Constants.Deg_315 - levelText.Rotation),
                                                  transformedLocation.Y - Constants.TextOffset * Math.Sin(Constants.Deg_315 - levelText.Rotation),
                                                  0.0);
                        break;
                    case AttachmentPoint.BottomCenter:
                        cornerPoint = new Point3d(transformedLocation.X - Constants.TextOffset * Math.Cos(levelText.Rotation - Constants.Deg_270),
                                                  transformedLocation.Y - Constants.TextOffset * Math.Sin(levelText.Rotation - Constants.Deg_270),
                                                  0.0);
                        break;
                    case AttachmentPoint.TopCenter:
                        cornerPoint = new Point3d(transformedLocation.X + Constants.TextOffset * Math.Cos(levelText.Rotation - Constants.Deg_270),
                                                  transformedLocation.Y + Constants.TextOffset * Math.Sin(levelText.Rotation - Constants.Deg_270),
                                                  0.0);
                        break;
                    default:
                        // Error condition
                        break;
                }
                return cornerPoint;
            }
        }

        private static bool SwitchUCS(Point3d cornerCoords, double textAngle)
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    UcsTable acUCSTbl = acTrans.GetObject(acCurrDb.UcsTableId, OpenMode.ForRead) as UcsTable;
                    UcsTableRecord jppUCSTblRec;
                    // Check to see if JPP App UCS table record exists and create if not
                    if (acUCSTbl.Has("JPP_App_UCS") == false)
                    {
                        jppUCSTblRec = new UcsTableRecord();
                        jppUCSTblRec.Name = "JPP_App_UCS";

                        acUCSTbl.UpgradeOpen();
                        acUCSTbl.Add(jppUCSTblRec);
                        acTrans.AddNewlyCreatedDBObject(jppUCSTblRec, true);
                    }
                    else
                    {
                        jppUCSTblRec = acTrans.GetObject(acUCSTbl["JPP_App_UCS"], OpenMode.ForWrite) as UcsTableRecord;
                    }
                    jppUCSTblRec.Origin = cornerCoords;
                    jppUCSTblRec.XAxis = cornerCoords.GetVectorTo(new Point3d(cornerCoords.X + Math.Cos(textAngle),
                                                                                cornerCoords.Y + Math.Sin(textAngle),
                                                                                0.0));
                    jppUCSTblRec.YAxis = cornerCoords.GetVectorTo(new Point3d(cornerCoords.X - Math.Sin(textAngle),
                                                                                cornerCoords.Y + Math.Cos(textAngle),
                                                                                0.0));
                    // Open the active viewport
                    ViewportTableRecord acVportTblRec = acTrans.GetObject(acEditor.ActiveViewportId, OpenMode.ForWrite) as ViewportTableRecord;

                    // Display the UCS Icon as the origin of the current viewport
                    // acVportTblRec.IconAtOrigin = true;
                    // acVportTblRec.IconEnabled = true;

                    // Set the UCS current
                    acVportTblRec.SetUcs(jppUCSTblRec.ObjectId);
                    acEditor.UpdateTiledViewportsFromDatabase();

                    // Display the name of the current UCS
                    UcsTableRecord acUCSTblRecActive = acTrans.GetObject(acVportTblRec.UcsName, OpenMode.ForRead) as UcsTableRecord;
                    acEditor.WriteMessage("\nThe current UCS is: {0}", acUCSTblRecActive.Name);

                    // Save the new objects to the database
                    acTrans.Commit();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                        + "\nError creating UCS!");
                    acTrans.Abort();
                    return false;
                }
            }
        }
    }
}
