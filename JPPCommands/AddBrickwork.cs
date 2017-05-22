using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Windows;

namespace JPPCommands
{
    class AddBrickwork
    {
        public static bool ExposedAndTanking()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            // Fetch the block reference ID for the FFL block to add exposed brickwork
            ObjectId blockId = FetchFFLBlockID();
            if (blockId == ObjectId.Null)
            {
                acEditor.WriteMessage("\nError, unable to retrieve FFL Block ID!");
                return false;
            }

            if(!Brickwork(true, blockId))
                return false;
            if(!Brickwork(false, blockId))
                return false;
            return true;
        }


        private static bool Brickwork(bool exposed, ObjectId fflBlockId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            // Find the outline polyline object
            Polyline outline = FetchOutline(fflBlockId);
            if (outline == null)
            {
                acEditor.WriteMessage("\nError, unable to retrieve FFL Block ID!");
                return false;
            }

            // Retrieve the extension dictionary for the outline

            DBDictionary blockOutlineExtDict = JPPUtils.getOutlineExtensionDictionary(outline);
            if (blockOutlineExtDict == null)
            {
                acEditor.WriteMessage("\nError, unable to retrieve extension dictionary of outline!");
                return false;
            }
            // Retrieve the level at each vertex from the Xrecords in the outline extenstion dictionary
            double[] levels = new double[outline.NumberOfVertices];        // Array to store the levels
            for (int index = 0; index < outline.NumberOfVertices; index++)
            {
                ResultBuffer rbLevel = JPPUtils.getXrecord(blockOutlineExtDict.Id, "Vertex_" + index.ToString());
                TypedValue[] xrecData = rbLevel.AsArray();
                levels[index] = Convert.ToDouble(xrecData[3].Value);
            }

            // Retrieve the FFL
            ResultBuffer rbFFL = JPPUtils.getXrecord(blockOutlineExtDict.Id, "FFL");
            double FFL = Convert.ToDouble(rbFFL.AsArray()[0].Value);

            // Create the offset polyline
            DBObjectCollection offsetOutlineObjects = outline.GetOffsetCurves(0.300);
            if ((offsetOutlineObjects.Count != 1) || (offsetOutlineObjects[0].GetType() != typeof(Polyline)))
            {
                acEditor.WriteMessage("\nError, unable to create offset outline!");
                return false;
            }
            Polyline offsetOutline = offsetOutlineObjects[0] as Polyline;

            // For each vertex of the outline polyline check the level against the FFL. If it is 
            // less the FFL - 0.15 then there is exposed brickwork, if it is greater than FFL - 0.015
            // tanking is required.
            // At the first occurence the coordinates of the previous vertex need to be saved 
            double threshold = FFL - 0.15;
            int startVertex = 0;

            // Check if the level of vertex 0 is above of below threshold. If it is iterate anticlockwise
            // around the outline until a vertex with a level at the threshold is found. This
            // defines the start point to work round the outline.
            if ((levels[startVertex] < threshold) || (levels[startVertex] > FFL))
            {
                for (int index = outline.NumberOfVertices - 1; index >= 0; index--)
                    if ((levels[index] > threshold) || (levels[index] < threshold))
                    {
                        // Found the start point for this run of exposed or tanked brickwork
                        startVertex = index;
                        break;
                    }
            }
            Point2dCollection hatchBoundaryPoints = new Point2dCollection();
            Point2dCollection offsetVertices = new Point2dCollection();
            List<int> cornerTextIndex = new List<int>();
            bool creatingHatchBoundary = false;
            for (int index = 0; index < outline.NumberOfVertices; index++)
            {
                int vertexIndex;
                if (index < (outline.NumberOfVertices - startVertex))
                    vertexIndex = index + startVertex;
                else
                    vertexIndex = index - (outline.NumberOfVertices - startVertex);
                if ((exposed && levels[vertexIndex] < threshold) ||
                        (!exposed && (!IsAccessPoint(outline, vertexIndex)) && levels[vertexIndex] > threshold))
                {
                    if (hatchBoundaryPoints.Count == 0)           // First vertex above or below the threshold
                    {
                        // First point of the hatch boundary will be the previous vertex.
                        // Also, check to see if vertex is an access point. If it is then the hatch boundary point needs to be moved
                        // to accommodate this. It is assumed the access point will be 900mm wide.
                        Point2d firstVertex = new Point2d();
                        if (vertexIndex == 0)
                            firstVertex = CheckForAccessPoint(outline, outline.NumberOfVertices - 1, true);
                        else
                            firstVertex = CheckForAccessPoint(outline, vertexIndex - 1, true);
                        hatchBoundaryPoints.Add(firstVertex);
                        creatingHatchBoundary = true;
                    }
                    hatchBoundaryPoints.Add(outline.GetPoint2dAt(vertexIndex));
                    offsetVertices.Add(offsetOutline.GetPoint2dAt(vertexIndex));
                    // Check if this vertex is a corner by comparing the angle of the 1st vector and 2nd vector
                    if (IsCorner(outline, vertexIndex))
                    {
                        cornerTextIndex.Add(vertexIndex);
                        cornerTextIndex.Add(Convert.ToInt32(Math.Ceiling((threshold - levels[vertexIndex]) / 0.075)));
                    }
                }
                else if (creatingHatchBoundary)                  // At the end of an exposed brickwork run
                {
                    Point2d lastVertex = CheckForAccessPoint(outline, vertexIndex, false);
                    hatchBoundaryPoints.Add(lastVertex);
                    // Now need to close the hatch boundary
                    for (int index2 = offsetVertices.Count - 1; index2 >= 0; index2--)
                    {
                        hatchBoundaryPoints.Add(offsetVertices[index2]);
                    }
                    // Call the function to add the hatch
                    if (!AddHatch(hatchBoundaryPoints, fflBlockId, exposed))
                    {
                        acEditor.WriteMessage("\nError, unable to exposed brick or tanking hatch!");
                        return false;
                    }
                    // Call function to add text
                    if (!AddCourseText(cornerTextIndex, offsetOutline, fflBlockId, exposed))
                    {
                        acEditor.WriteMessage("\nError, unable to exposed brick or tanking text!");
                        return false;
                    }
                    hatchBoundaryPoints.Clear();
                    offsetVertices.Clear();
                    cornerTextIndex.Clear();
                    creatingHatchBoundary = false;
                }
            }
            return true;
        }


        private static ObjectId FetchFFLBlockID()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            // Create the selection filter for the FFL block
            TypedValue[] selFilterList = new TypedValue[2];
            selFilterList[0] = new TypedValue(0, "INSERT");
            selFilterList[1] = new TypedValue(2, "JPP_App_Outline*");

            SelectionFilter selFilter = new SelectionFilter(selFilterList);

            // Set prompt options and message
            PromptSelectionOptions selOptions = new PromptSelectionOptions();
            selOptions.MessageForAdding = "Select FFL block: ";
            selOptions.SinglePickInSpace = true;
            selOptions.SingleOnly = true;

            // Prompt user to select FFL add exposed brickwork
            PromptSelectionResult selResult = acEditor.GetSelection(selOptions, selFilter);
            ObjectId[] FFLToEditId = selResult.Value.GetObjectIds();
            ObjectId blockId = FFLToEditId[0];

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockReference fflBlock = acTrans.GetObject(blockId, OpenMode.ForRead) as BlockReference;
                    BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[fflBlock.Name],
                                                                         OpenMode.ForRead) as BlockTableRecord;
                    if (acBlkTblRec == null)
                    {
                        acEditor.WriteMessage("\nError FFL block not found.");
                        acTrans.Commit();
                        return ObjectId.Null;
                    }
                    // Check there is only one instance of this block reference in the drawing
                    if (acBlkTblRec.GetBlockReferenceIds(false, true).Count != 1)
                    {
                        acEditor.WriteMessage("\nError more than one instance of the block reference.");
                        acTrans.Commit();
                        return ObjectId.Null;
                    }
                    acTrans.Commit();
                    return fflBlock.ObjectId;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                        + "\nError retrieving FFL Block Id!\n");
                    acTrans.Abort();
                    return ObjectId.Null;
                }

            }
        }

        private static Polyline FetchOutline(ObjectId fflBlockId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;


            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockReference fflBlock = acTrans.GetObject(fflBlockId, OpenMode.ForRead) as BlockReference;
                    BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[fflBlock.Name],
                                                                                OpenMode.ForRead) as BlockTableRecord;
                    Polyline acPline = new Polyline();
                    foreach (ObjectId objId in acBlkTblRec)
                    {
                        // Fetch the object
                        Object fflObj = acTrans.GetObject(objId, OpenMode.ForWrite);
                        if (fflObj.GetType() != typeof(Polyline))
                            continue;
                        acPline = fflObj as Polyline;
                        break;
                    }
                    acTrans.Commit();
                    return acPline;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                        + "\nError retrieving outline polyline!\n");
                    acTrans.Abort();
                    return null;
                }
            }
        }

        private static bool IsAccessPoint(Polyline acPline, int index)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;
            try
            {
                DBDictionary outlineExtDict = JPPUtils.getOutlineExtensionDictionary(acPline);

                // Retrieve the Xrecord
                ResultBuffer rbLevel = JPPUtils.getXrecord(outlineExtDict.Id, "Vertex_" + index.ToString());
                TypedValue[] xrecData = rbLevel.AsArray();
                if (xrecData[1].Value.ToString() == "Access_Point")
                    return true;
                else
                    return false;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acException)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                            ("The following exception was caught: \n" + acException.Message
                                                 + "\nError checking for access point\n");
                return true;
            }

        }


        private static Point2d CheckForAccessPoint(Polyline acPline, int index, bool atStart)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            int nextVertexIndex = 0;
            int previousVertexIndex = 0;
            Point2d newPoint = new Point2d();
            try
            {
                // This function determines whether the point passed to the function is an access point; if it
                // is then an offset to allow for the access point opening is calculated and the offset point coordinates
                // returned. The direction of the offset is applied in depends on whether this is the start or end
                // of the hatch boundary. 
                // If it's the start of the hatch boundary the offset is applied in the direction of the vector from 
                // this point to the next vertex.
                // If it's the end of the hatch boundary the offset is applied in the direction of a vector from this 
                // to the previous point.
                // First check if point is an access point as there's nothing to do if not.
                // Fetch the extension dictionary 
                DBDictionary outlineExtDict = JPPUtils.getOutlineExtensionDictionary(acPline);

                // Retrieve the Xrecord
                ResultBuffer rbLevel = JPPUtils.getXrecord(outlineExtDict.Id, "Vertex_" + index.ToString());
                TypedValue[] xrecData = rbLevel.AsArray();
                if (xrecData[1].Value.ToString() != "Access_Point")
                    return (newPoint = acPline.GetPoint2dAt(index));

                // First check if the point is the first vertex of the outline previous or next vertex index.

                if (atStart)
                {
                    if (index == acPline.NumberOfVertices - 1)
                        nextVertexIndex = 0;
                    else
                        nextVertexIndex = index + 1;
                    // Calculate new point based on the vector angle
                    double vectorAngle = acPline.GetPoint2dAt(index).GetVectorTo(acPline.GetPoint2dAt(nextVertexIndex)).Angle;
                    newPoint = new Point2d(acPline.GetPoint2dAt(index).X + (Constants.Access_Point_Width / 2) * Math.Cos(vectorAngle),
                                               acPline.GetPoint2dAt(index).Y + (Constants.Access_Point_Width / 2) * Math.Sin(vectorAngle));
                }
                else
                {
                    if (index == 0)
                        previousVertexIndex = acPline.NumberOfVertices - 1;
                    else
                        previousVertexIndex = index - 1;
                    // Calculate new point based on the vector angle
                    double vectorAngle = acPline.GetPoint2dAt(previousVertexIndex).GetVectorTo(acPline.GetPoint2dAt(index)).Angle;
                    newPoint = new Point2d(acPline.GetPoint2dAt(index).X - (Constants.Access_Point_Width / 2) * Math.Cos(vectorAngle),
                                           acPline.GetPoint2dAt(index).Y - (Constants.Access_Point_Width / 2) * Math.Sin(vectorAngle));
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acException)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                            ("The following exception was caught: \n" + acException.Message
                                                 + "\nError accessing vertex Xrecord\n");
                newPoint = acPline.GetPoint2dAt(index);
            }
            return newPoint;
        }

        private static bool IsCorner(Polyline acPline, Int32 vertex)
        {
            // Get angle of vector to vertex and from vertex
            Int32 previousVertex = 0;
            Int32 nextVertex = 0;
            if (vertex == 0)
                previousVertex = acPline.NumberOfVertices - 1;
            else
                previousVertex = vertex - 1;
            if (vertex == acPline.NumberOfVertices - 1)
                nextVertex = 0;
            else
                nextVertex = vertex + 1;
            double angle1 = acPline.GetPoint2dAt(previousVertex).GetVectorTo(acPline.GetPoint2dAt(vertex)).Angle;
            double angle2 = acPline.GetPoint2dAt(vertex).GetVectorTo(acPline.GetPoint2dAt(nextVertex)).Angle;
            double cornerAngle = angle1 - angle2;
            if ((cornerAngle >= Constants.Deg_90 - Constants.Deg_1)
                            && (cornerAngle <= Constants.Deg_90 + Constants.Deg_1)) return true;
            if ((cornerAngle >= -Constants.Deg_270 - Constants.Deg_1)
                            && (cornerAngle <= -Constants.Deg_270 + Constants.Deg_1)) return true;
            return false;
        }



        private static bool AddHatch(Point2dCollection boundaryPoints, ObjectId fflBlockId, bool isExposed)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            // Lose this line in the real command
            bool success = JPPCommandsInitialisation.setJPPLayers();

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    Polyline hatchBoundary = new Polyline();

                    // need to transform the outline by the block reference transform
                    BlockReference fflBlock = acTrans.GetObject(fflBlockId, OpenMode.ForRead) as BlockReference;

                    for (int index = 0; index < boundaryPoints.Count; index++)
                    {
                        Point3d nextVertex = new Point3d(boundaryPoints[index].X,
                                                         boundaryPoints[index].Y,
                                                         0.0).TransformBy(fflBlock.BlockTransform);
                        hatchBoundary.AddVertexAt(index, new Point2d(nextVertex.X, nextVertex.Y), 0, 0, 0);
                    }
                    hatchBoundary.Closed = true;
                    if (isExposed)
                        hatchBoundary.Layer = StyleNames.JPP_App_Exposed_Brick_Layer;
                    else
                        hatchBoundary.Layer = StyleNames.JPP_App_Tanking_Layer;

                    // Add the hatch boundary to modelspace
                    BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                                                OpenMode.ForWrite) as BlockTableRecord;
                    acBlkTblRec.AppendEntity(hatchBoundary);
                    acTrans.AddNewlyCreatedDBObject(hatchBoundary, true);

                    // Add the hatch boundry to an object Id collection 
                    ObjectIdCollection acObjIdColl = new ObjectIdCollection();
                    acObjIdColl.Add(hatchBoundary.Id);

                    // Set the hatch properties
                    using (Hatch exposedHatch = new Hatch())
                    {
                        acBlkTblRec.AppendEntity(exposedHatch);
                        acTrans.AddNewlyCreatedDBObject(exposedHatch, true);

                        // Set the hatch properties
                        exposedHatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                        if (isExposed)
                        {
                            exposedHatch.Layer = StyleNames.JPP_App_Exposed_Brick_Layer;
                            exposedHatch.BackgroundColor = Color.FromColorIndex(ColorMethod.ByAci, 80);
                        }
                        else
                        {
                            exposedHatch.Layer = StyleNames.JPP_App_Tanking_Layer;
                            exposedHatch.BackgroundColor = Color.FromColorIndex(ColorMethod.ByAci, 130);
                        }
                        exposedHatch.PatternScale = 0.1;
                        exposedHatch.PatternSpace = 0.1;
                        exposedHatch.PatternAngle = Constants.Deg_45;
                        exposedHatch.Associative = true;
                        exposedHatch.Annotative = AnnotativeStates.False;
                        exposedHatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
                        exposedHatch.EvaluateHatch(true);
                    }

                    acTrans.Commit();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError adding hatch!\n");
                    acTrans.Commit();
                    return false;
                }

            }
        }

        private static bool AddCourseText(List<int> pointsInfo, Polyline acPline, ObjectId fflBlockId, bool isExposed)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;
            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    Polyline hatchBoundary = new Polyline();

                    // Need to transform the text by the block reference transform so retrive the block reference
                    BlockReference fflBlock = acTrans.GetObject(fflBlockId, OpenMode.ForRead) as BlockReference;

                    int[] textData = pointsInfo.ToArray();
                    // The textData array contians pairs of values vertex index, number of courses. Check
                    // array has an even number of elements to avoid an error
                    if (textData.Count() % 2 != 0)
                    {
                        acEditor.WriteMessage("\nError, odd number of elements in course text array!");
                        return false;
                    }
                    // Iterate through the array to add the corner text


                    for (int index = 0; index < textData.Count(); index = index + 2)
                    {
                        int nextVertex = 0;
                        MText courseText = new MText();
                        if (textData[index] == acPline.NumberOfVertices - 1)
                            nextVertex = 0;
                        else
                            nextVertex = textData[index] + 1;
                        // Get the angle of the vector to the next vertex
                        double angle = acPline.GetPoint2dAt(textData[index]).GetVectorTo(acPline.GetPoint2dAt(nextVertex)).Angle;
                        if ((angle >= Constants.Deg_0) && (angle < Constants.Deg_90))
                        {
                            courseText.Rotation = angle;
                            courseText.Location = new Point3d(acPline.GetPoint2dAt(textData[index]).X + Constants.TextOffset * Math.Cos(angle - Constants.Deg_45),
                                                              acPline.GetPoint2dAt(textData[index]).Y + Constants.TextOffset * Math.Sin(angle - Constants.Deg_45),
                                                              0.0);
                            courseText.Attachment = AttachmentPoint.TopLeft;
                        }
                        if ((angle >= Constants.Deg_90) && (angle < Constants.Deg_180))
                        {
                            courseText.Rotation = angle - Constants.Deg_90;
                            courseText.Location = new Point3d(acPline.GetPoint2dAt(textData[index]).X + Constants.TextOffset * Math.Cos(angle - Constants.Deg_45),
                                                              acPline.GetPoint2dAt(textData[index]).Y + Constants.TextOffset * Math.Sin(angle - Constants.Deg_45),
                                                              0.0);
                            courseText.Attachment = AttachmentPoint.BottomLeft;
                        }
                        if ((angle >= Constants.Deg_180) && (angle < Constants.Deg_270))
                        {
                            courseText.Rotation = angle - Constants.Deg_180;
                            courseText.Location = new Point3d(acPline.GetPoint2dAt(textData[index]).X + Constants.TextOffset * Math.Cos(angle - Constants.Deg_45),
                                                              acPline.GetPoint2dAt(textData[index]).Y + Constants.TextOffset * Math.Sin(angle - Constants.Deg_45),
                                                              0.0);
                            courseText.Attachment = AttachmentPoint.BottomRight;
                        }
                        if ((angle >= Constants.Deg_270) && (angle < Constants.Deg_360))
                        {
                            courseText.Rotation = angle;
                            courseText.Location = new Point3d(acPline.GetPoint2dAt(textData[index]).X + Constants.TextOffset * Math.Cos(angle - Constants.Deg_45),
                                                              acPline.GetPoint2dAt(textData[index]).Y + Constants.TextOffset * Math.Sin(angle - Constants.Deg_45),
                                                              0.0);
                            courseText.Attachment = AttachmentPoint.TopLeft;
                        }

                        // Reterieve the JPP App text style
                        TextStyleTable acTextStyleTable;
                        acTextStyleTable = acTrans.GetObject(acCurrDb.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                        TextStyleTableRecord acTxtStyleRecord;
                        acTxtStyleRecord = acTrans.GetObject(acTextStyleTable[StyleNames.JPP_App_Text_Style],
                                                                        OpenMode.ForRead) as TextStyleTableRecord;
                        courseText.Layer = StyleNames.JPP_App_Exposed_Brick_Layer;
                        courseText.TextStyleId = acTxtStyleRecord.ObjectId;
                        courseText.Width = 2.5;
                        courseText.Height = 0.6;
                        courseText.TextHeight = 0.4;
                        if (isExposed)
                        {
                            courseText.Color = Color.FromColorIndex(ColorMethod.ByAci, 80);
                            courseText.Contents = textData[index + 1].ToString() + "C";
                        }
                        else
                        {
                            courseText.Color = Color.FromColorIndex(ColorMethod.ByAci, 130);
                            courseText.Contents = "Tank " + Math.Abs((0.075 * textData[index + 1])).ToString("N3") + "m";
                        }

                        // Transform etxt location by the blcok trasnform
                        courseText.Location = courseText.Location.TransformBy(fflBlock.BlockTransform);

                        BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                                                    OpenMode.ForWrite) as BlockTableRecord;
                        acBlkTblRec.AppendEntity(courseText);
                        acTrans.AddNewlyCreatedDBObject(courseText, true);
                    }
                    acTrans.Commit();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError adding course text!\n");
                    acTrans.Commit();
                    return false;
                }

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
    }
}
