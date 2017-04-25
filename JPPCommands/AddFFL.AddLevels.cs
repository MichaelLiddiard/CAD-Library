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
        public static bool AddLevels(ObjectId outline)
        {
            // Before adding the levels text access points need to be added. The user is prompted to click on the access point 
            // and then asked if this access point is at FFL or 150mm below.
            //
            // The new vertex is added to the building outline, a new Xrecord created for the vertex and added to
            // the extension dictionary. The names of Xrecords for the existing vertices beyond the added vertex are
            // updated to reflect the vertex's new position.
            //
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Editor acEditor = acDoc.Editor;
            // Prompt user to click on access points.
            bool addingAccessPoints = true;
            string accessLevel = "";
            PromptPointOptions promptAccessPtOpts = new PromptPointOptions("\nClick access point or Spacebar when done: ");
            promptAccessPtOpts.AllowNone = true;
            // Set up prompt string
            PromptStringOptions promptQuestionOpts = new PromptStringOptions("\nIs this access point at FFL (Y/N)?");
            // Loop while adding access points
            while (addingAccessPoints)
            {
                PromptPointResult promptResult = acEditor.GetPoint(promptAccessPtOpts);
                if (promptResult.Status == PromptStatus.OK)
                {
                    // Prompt user for access point level
                    PromptResult promptStringResult = acEditor.GetString(promptQuestionOpts);
                    switch (promptStringResult.StringResult.ToUpper())
                    {
                        case "Y":
                            accessLevel = "At_FFL";
                            break;
                        case "N":
                            accessLevel = "At_150_Below";
                            break;
                        default:
                            acEditor.WriteMessage("\nInvalid input. Access point level set at FFL!");
                            accessLevel = "At_FFL";
                            break;
                    }
                    bool accessPointAdded = addAccessPoint(promptResult.Value, outline, accessLevel);
                    if (accessPointAdded)
                    {
                        acEditor.WriteMessage("\nAccess Point added!");
                    }
                }
                else if (promptResult.Status == PromptStatus.None)
                {
                    addingAccessPoints = false;
                }
                else
                {
                    acEditor.WriteMessage("\nError adding access point!");
                    return false;
                }
            }
            return true;
        }


        private static bool addAccessPoint(Point3d accessPoint, ObjectId dbObjectId, string level)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                // Add the access point to the 'buildingOutline' polyline by iterating around the polyline
                // comparing the distance of each vertex from the start point to the distance of new
                // point form the start point. When the new point distance is less than the current
                // vertex add the new point.
                //
                // NOTE: this requires that the newpoint lies on the 'buildingOutline' polyline.
                //
                try
                {
                    int accessPointIndex = 0;
                    // Fetch the polyline
                    Polyline buildingOutline = acTrans.GetObject(dbObjectId, OpenMode.ForWrite) as Polyline;

                    for (int index = 0; index <= buildingOutline.NumberOfVertices; index++)
                    {
                        // Check whether index is equal to the number of vertices. If it is the access
                        // point is lies on the segment from the last point of the building outline to the
                        // first point of the building outline so compare the distance to the new point against
                        // the length of the building outline
                        if (index == buildingOutline.NumberOfVertices)
                        {
                            if (buildingOutline.GetDistAtPoint(accessPoint) < buildingOutline.Length)
                            {
                                buildingOutline.AddVertexAt(index, new Point2d(accessPoint.X, accessPoint.Y), 0, 0, 0);
                                accessPointIndex = index;
                                break;                              // Make sure loop is not executed again
                            }
                            else
                            {
                                // Error condition
                            }
                        }
                        // Else check distance along polyline of new point against each vertex (corner) to find
                        // segment of  outline where access point lies.
                        else
                        {
                            if (buildingOutline.GetDistAtPoint(accessPoint)
                                    < buildingOutline.GetDistAtPoint(buildingOutline.GetPoint3dAt(index)))
                            {
                                buildingOutline.AddVertexAt(index, new Point2d(accessPoint.X, accessPoint.Y), 0, 0, 0);
                                accessPointIndex = index;
                                break;                      // Found segment to add 
                            }
                        }
                    }
                    // SHOULD add a check in the above to trap case where the added point is a corner. Check with JPP on how to handle this.

                    // Now have the index of the new vertex so update the Xrecord name for each vertex beyond the added
                    // vertex before adding Xrecord for new vertex. First check that new vertex doesn't lie between the
                    // first and last vertices
                    // if (accessPointIndex < buildingOutline.NumberOfVertices - 2)        // Not the last vertex
                    if (accessPointIndex < buildingOutline.NumberOfVertices - 1)        // Not the last vertex
                    {
                        // Update name Xrecords for those beyond the new vertex
                        for (int index = buildingOutline.NumberOfVertices - 1; index > accessPointIndex; index--)
                        {
                            string newKey = "Vertex_" + index.ToString();
                            string oldKey = "Vertex_" + (index - 1).ToString();
                            bool accessPointAdded = JPPUtils.renameXrecord(dbObjectId, oldKey, newKey, true);
                        }
                    }
                    // Fetch the FFL double to calculte value of level
                    // Add the new Xrecord
                    ResultBuffer xrecVertexData = new ResultBuffer();
                    xrecVertexData.Add(new TypedValue((int)DxfCode.ExtendedDataInteger16, accessPointIndex));
                    xrecVertexData.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Access_Point"));
                    xrecVertexData.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, level));
                    // Need to calculate the actual level and therefore need the FFL
                    double? FFLDouble = JPPUtils.getFFL(dbObjectId);
                    // Add the level based on the FFL and whether the access point is at FFL or 150mm below FFL
                    if (level == "At_FFL")
                        xrecVertexData.Add(new TypedValue((int)DxfCode.ExtendedDataReal, FFLDouble));
                    else
                        xrecVertexData.Add(new TypedValue((int)DxfCode.ExtendedDataReal, (FFLDouble - 0.15)));
                    bool success = JPPUtils.addXrecord(dbObjectId, ("Vertex_" + accessPointIndex.ToString()), xrecVertexData);
                    acTrans.Commit();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                     + "\nError adding access point!\n");
                    acTrans.Commit();
                    return false;
                }
            }
        }
    }
}
