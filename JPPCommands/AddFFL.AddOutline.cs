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
    public static partial class AddFFL
    {
        public static ObjectId CreateOutline()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            // All work will be done in the WCS so save the current UCS
            // to restore later and set the UCS to WCS
            Matrix3d CurrentUCS = acEditor.CurrentUserCoordinateSystem;
            acEditor.CurrentUserCoordinateSystem = Matrix3d.Identity;

            // Get the current color, for temp graphics
            // Color currCol = acCurrDb.Cecolor;
            Color drawColor = Color.FromColorIndex(ColorMethod.ByAci, 1);
            // Create a 3d point collection to store the vertices 
            Point3dCollection PickPts = new Point3dCollection();

            // Set up the selection options
            PromptPointOptions promptCornerPtOpts = new PromptPointOptions("\nClick on each corner (Spacebar when done): ");
            promptCornerPtOpts.AllowNone = true;

            // Get the start point for the polyline
            PromptPointResult promptResult = acEditor.GetPoint(promptCornerPtOpts);
            // Continue to add picked corner points to the polyline
            while (promptResult.Status == PromptStatus.OK)
            {
                // Add the selected point PickPts collection
                PickPts.Add(promptResult.Value);
                // Drag a temp line during selection of subsequent points
                promptCornerPtOpts.UseBasePoint = true;
                promptCornerPtOpts.BasePoint = promptResult.Value;
                promptResult = acEditor.GetPoint(promptCornerPtOpts);
                if (promptResult.Status == PromptStatus.OK)
                {
                    // For each point selected, draw a temporary segment
                    acEditor.DrawVector(PickPts[PickPts.Count - 1],     // start point
                                    promptResult.Value,                 // end point
                                    drawColor.ColorIndex,               // highlight colour
                                    //currCol.ColorIndex,               // current color
                                    false);                             // highlighted
                }
            }
            // Check user has not aborted adding the outline by pressing ESC
            if (promptResult.Status == PromptStatus.Cancel)
            {
                acEditor.Regen();
                return ObjectId.Null;
            }
                
            Polyline acPline = new Polyline(PickPts.Count);
            acPline.Layer = StyleNames.JPP_App_Outline_Layer;
            // The user has pressed SPACEBAR to exit the picking points loop
            if (promptResult.Status == PromptStatus.None)
            {
                foreach (Point3d pt in PickPts)
                {
                    // Alert user that picked point has elevation.
                    if (pt.Z != 0.0)
                        acEditor.WriteMessage("/nWarning: corner point has non-zero elevation. Elevation will be ignored.");
                    acPline.AddVertexAt(acPline.NumberOfVertices, new Point2d(pt.X, pt.Y), 0, 0, 0);
                }
                // If user has clicked the start point to close the polyline delete this point and
                // set polyline to closed
                if (acPline.EndPoint == acPline.StartPoint)
                    acPline.RemoveVertexAt(acPline.NumberOfVertices - 1);
                acPline.Closed = true;
            }
            ObjectId plineId = AddPolyline(acPline);
            return plineId;
        }

        public static bool FormatOutline(ObjectId plineToFormatId)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                try
                {
                    Polyline outline = acTrans.GetObject(plineToFormatId, OpenMode.ForWrite) as Polyline;
                    // To make placing of levels easier order the points in a clockwise order. First determine
                    // whether the points are already in clockwise order by calculating the area under each line segment.
                    // A negative result indicates the points are in anit-clockwise order so need reversing.
                    //
                    // Polyline area = Sum(0.5[(x2 - x1)(y2 + y1) + (x3 -x2)(Y3 + y2) +....+ (x1 - xn)(y1 + xn)])
                    //
                    double outlineArea = 0.0;
                    for (int index = 0; index < outline.NumberOfVertices; index++)
                    {
                        if (index == outline.NumberOfVertices - 1)                              // Last point so need to use first point
                            outlineArea += ((outline.GetPoint3dAt(0).X - outline.GetPoint3dAt(index).X)
                                                            * (outline.GetPoint3dAt(0).Y + outline.GetPoint3dAt(index).Y));
                        else
                            outlineArea += ((outline.GetPoint3dAt(index + 1).X - outline.GetPoint3dAt(index).X)
                                                            * (outline.GetPoint3dAt(index + 1).Y + outline.GetPoint3dAt(index).Y));
                    }
                    if (outlineArea >= 0.0)
                        acEditor.WriteMessage("\nPoints in clockwise order!");
                    else
                    {
                        outline.ReverseCurve();
                        acEditor.WriteMessage("\nPolyline order reversed.");
                    }
                    // Now find the reference point from which to iterate around the polyline to add the levels.
                    // First find the min point of the outline bounding box, then check each corner of the outline to
                    // find the bottom, leftmost point. Finally reorder the points so the reference point is the 
                    // first vertex of the polyline
                    Extents3d outlineExtents = outline.GeometricExtents;
                    Point3d outlineRefPoint = outline.GetPoint3dAt(0);
                    int refIndex = 0;
                    if (outlineRefPoint != outlineExtents.MinPoint)                             // Check if ref point aleady at bounding box min point 
                    {                                                                           // Iterate around the rest of the points if not      
                        for (int index = 0; index < outline.NumberOfVertices; index++)
                        {
                            if (outline.GetPoint3dAt(index).Y == outlineExtents.MinPoint.Y)     // If current point Y coord equals min Y coord of 
                            {                                                                   // bounding box then
                                if (outline.GetPoint3dAt(index).Y == outlineRefPoint.Y)         // check if ref point Y is already set to 
                                                                                                // Y coord of bounding box.
                                    if (outline.GetPoint3dAt(index).X <= outlineRefPoint.X)     // If yes check if X coord of current point is less 
                                    {                                                           // than X coord of ref point. 
                                        outlineRefPoint = outline.GetPoint3dAt(index);          // If yes update ref point to current point.
                                    }
                                outlineRefPoint = outline.GetPoint3dAt(index);                  // Else first time current point Y coord equals
                                refIndex = index;                                               // min Y coord so set ref point to current point
                            }
                        }
                    }
                    acEditor.WriteMessage("\nThe reference point is: " + outlineRefPoint.X + ", " + outlineRefPoint.Y);
                    // Check if ref point aleady at bounding box min point. If it is do nothing.
                    outlineRefPoint = outline.GetPoint3dAt(0);
                    if (outlineRefPoint != outlineExtents.MinPoint)
                    {
                        Point2dCollection tempArray = new Point2dCollection(outline.NumberOfVertices);
                        for (int index = 0; index < outline.NumberOfVertices; index++)
                            tempArray.Add(outline.GetPoint2dAt(index));
                        for (int index = 0; index < tempArray.Count; index++)
                        {
                            outline.RemoveVertexAt(index);
                            if ((index + refIndex) < tempArray.Count)
                                outline.AddVertexAt((index), tempArray[refIndex + index], 0, 0, 0);
                            else
                                outline.AddVertexAt((index), tempArray[index - (tempArray.Count - refIndex)], 0, 0, 0);
                        }
                    }
                    acTrans.Commit();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                        + "\nError getting outline from database!\n");
                    acTrans.Commit();
                    return false;
                }
            }             
        }

        private static ObjectId AddPolyline(Polyline plineToAdd)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                ObjectId outlineId = new ObjectId();
                try
                {
                    BlockTable acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForWrite) as BlockTableRecord;
                    // Add the new object to the block table record and the transaction
                    outlineId = acBlkTblRec.AppendEntity(plineToAdd);
                    acTrans.AddNewlyCreatedDBObject(plineToAdd, true);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                        + "\nError adding outline!\n");
                }
                acTrans.Commit();
                return outlineId;
            }
        }
    }
}
