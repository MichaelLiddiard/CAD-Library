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
        public static bool AddText(ObjectId outlineId, ObjectIdCollection FFLObjIdCollection)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                Polyline buildingOutline = acTrans.GetObject(outlineId, OpenMode.ForRead) as Polyline;
                // Get the extension dictionary. This contains the Xrecords for each vertex. Each Xrecord contains
                // information about the type of level and the level value.
                ObjectId extDictId = buildingOutline.ExtensionDictionary;
                if (extDictId == ObjectId.Null)
                {
                    acEditor.WriteMessage("\nError adding text, cannot open extension dictionary");
                    return false;
                }
                // At each vertex the angle of the segment into the vertex and the angle of the segment
                // out of the vertex define the level text position. To define the vectors (segments) the 
                // coordinates of the previous vertex, current vertext and next vertex are needed.
                int currVertexMinusOne, currVertex, currVertexPlusOne;
                double textRotation = 0.0;
                bool FFLAdded = false;
                bool textAdded = false;
                bool pointAdded = false;
                // currVertex = 0;
                // Iterate around the vertices to add the levels
                for (int index = 0; index < buildingOutline.NumberOfVertices; index++)
                {
                    currVertex = index;
                    // Fetch the level for the current vertex
                    ResultBuffer xRecVertex = JPPUtils.getXrecord(extDictId, ("Vertex_" + currVertex.ToString()));
                    if (xRecVertex == null)
                    {
                        acEditor.WriteMessage("\nError adding text, cannot get Xrecord.");
                        return false;
                    }
                    TypedValue[] xrecVertexData = xRecVertex.AsArray();
                    if ((int)xrecVertexData[3].TypeCode != (int)(DxfCode.ExtendedDataReal))
                    {
                        // Not real data so invalid Xrecord data
                    }
                    double dbleLevel = (double)xrecVertexData[3].Value;
                    // Format the double to a string with three decimal places, e.g. "12.234"
                    string strLevel = dbleLevel.ToString("N3");
                    // Define indices to the preious and next vertices
                    if (currVertex == 0)
                        currVertexMinusOne = buildingOutline.NumberOfVertices - 1;
                    else
                        currVertexMinusOne = currVertex - 1;
                    if (currVertex == buildingOutline.NumberOfVertices - 1)
                        currVertexPlusOne = 0;
                    else
                        currVertexPlusOne = currVertex + 1;
                    // Get the vertices
                    Point2dCollection vertices = new Point2dCollection();
                    vertices.Add(buildingOutline.GetPoint2dAt(currVertexMinusOne));
                    vertices.Add(buildingOutline.GetPoint2dAt(currVertex));
                    vertices.Add(buildingOutline.GetPoint2dAt(currVertexPlusOne));
                    // Get the angle to the x-axis of the vectors to and from the current vertex
                    double firstVectorAngle = Math.Round(vertices[0].GetVectorTo(vertices[1]).Angle, 5);
                    double secondVectorAngle = Math.Round(vertices[1].GetVectorTo(vertices[2]).Angle, 5);

                    // Calculate the length of each vector
                    double firstVectorLength = Math.Round(vertices[0].GetDistanceTo(vertices[1]), 5);
                    double secondVectorLength = Math.Round(vertices[1].GetDistanceTo(vertices[2]), 5);

                    string attachPoint = "";
                    Point3d textInsPoint = new Point3d();
                    // Work out whether the direction of the second vector is plus or minus 90 degrees, or in the same
                    // direction to determine the position and rotation of the level text. Angles lie in the range
                    // 0 <= angle < 360 degrees 
                    double firstVectorAnglePlus90, firstVectorAngleMinus90;
                    Angle nextVectorAngle;

                    // If the first and second vector angles are not the same then check plus and minus
                    // 90 angles are in range
                    firstVectorAnglePlus90 = firstVectorAngle + Constants.Deg_90;
                    if (firstVectorAnglePlus90 >= Constants.Deg_360)
                        firstVectorAnglePlus90 = firstVectorAnglePlus90 - Constants.Deg_360;

                    firstVectorAngleMinus90 = firstVectorAngle - Constants.Deg_90;
                    if (firstVectorAngleMinus90 < 0.0)
                        firstVectorAngleMinus90 = firstVectorAngleMinus90 + Constants.Deg_360;

                    if ((secondVectorAngle >= (firstVectorAngleMinus90 - Constants.Deg_1))
                            && (secondVectorAngle <= (firstVectorAngleMinus90 + Constants.Deg_1)))
                        nextVectorAngle = Angle.Minus_90_Degrees;

                    else if ((secondVectorAngle >= (firstVectorAnglePlus90 - Constants.Deg_1))
                                && (secondVectorAngle <= (firstVectorAnglePlus90 + Constants.Deg_1)))
                        nextVectorAngle = Angle.Plus_90_Degrees;

                    else if ((secondVectorAngle >= (firstVectorAngle - Constants.Deg_1))
                                && (secondVectorAngle <= (firstVectorAngle + Constants.Deg_1)))
                        nextVectorAngle = Angle.Plus_180_Degrees;

                    else
                        continue;              // not a corner or access point so move to next vertex

                    // Check for small vectors where adding level text will result in overlapping
                    // text.
                    if ((nextVectorAngle != Angle.Plus_180_Degrees) && (firstVectorLength < 2.5) && (secondVectorLength < 2.5))
                        continue;


                    if (firstVectorAngle >= Constants.Deg_0 && firstVectorAngle < Constants.Deg_60)
                    {
                        switch (nextVectorAngle)
                        {
                            case Angle.Minus_90_Degrees:
                                // If the first vector length is less than 2 * levels text box length then
                                // attach point BL and text rotation is 2nd vector angle
                                if (firstVectorLength < 5.1)
                                {
                                    attachPoint = "BL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle;
                                }
                                else
                                {
                                    attachPoint = "BR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_135),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_135),
                                                                0.0);
                                    textRotation = firstVectorAngle;
                                }
                                break;

                            case Angle.Plus_90_Degrees:
                                if (firstVectorLength < 5.1)
                                {
                                    attachPoint = "BR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle + Constants.Deg_180;
                                }
                                else
                                {
                                    attachPoint = "TR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_225),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_225),
                                                                0.0);
                                    textRotation = firstVectorAngle;
                                }
                                break;

                            case Angle.Plus_180_Degrees:
                                attachPoint = "TC";
                                textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_90),
                                                            buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_90),
                                                            0.0);
                                textRotation = firstVectorAngle;
                                break;
                        }
                    }
                    if (firstVectorAngle >= Constants.Deg_60 && firstVectorAngle < Constants.Deg_90)
                    {
                        switch (nextVectorAngle)
                        {
                            case Angle.Minus_90_Degrees:
                                if (secondVectorLength < 5.1)
                                {
                                    attachPoint = "BR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_45),
                                                                0.0);
                                    textRotation = firstVectorAngle;
                                }
                                else
                                {
                                    attachPoint = "BL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle;
                                }
                                break;

                            case Angle.Plus_90_Degrees:
                                if(secondVectorLength < 5.1)
                                {
                                    attachPoint = "TR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_225),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_225),
                                                                0.0);
                                    textRotation = firstVectorAngle;
                                }
                                else
                                {
                                    attachPoint = "BR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle;
                                }
                                break;

                            case Angle.Plus_180_Degrees:
                                attachPoint = "TC";
                                textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_90),
                                                            buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_90),
                                                            0.0);
                                textRotation = firstVectorAngle;
                                break;
                        }
                    }
                    if (firstVectorAngle >= Constants.Deg_90 && firstVectorAngle < Constants.Deg_150)
                    {
                        switch (nextVectorAngle)
                        {
                            case Angle.Minus_90_Degrees:
                                if(firstVectorLength < 5.1)
                                {
                                    attachPoint = "TL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_135),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_135),
                                                                0.0);
                                    textRotation = firstVectorAngle + Constants.Deg_180;
                                }
                                else
                                {
                                    attachPoint = "BL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle;
                                }
                                break;

                            case Angle.Plus_90_Degrees:
                                if (secondVectorLength < 5.1)
                                {
                                    attachPoint = "BL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = firstVectorAngle + Constants.Deg_180;
                                }
                                else
                                {
                                    attachPoint = "BR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_45),
                                                                     buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_45),
                                                                           0.0);
                                    textRotation = firstVectorAngle - Constants.Deg_90;
                                }
                                break;
                            case Angle.Plus_180_Degrees:
                                attachPoint = "BC";
                                textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_90),
                                                            buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_90),
                                                            0.0);
                                textRotation = firstVectorAngle + Constants.Deg_180;
                                break;
                        }


                    }
                    if (firstVectorAngle >= Constants.Deg_150 && firstVectorAngle < Constants.Deg_180)
                    {
                        switch (nextVectorAngle)
                        {
                            case Angle.Minus_90_Degrees:
                                if (firstVectorLength < 5.1)
                                {
                                    attachPoint = "BL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle;
                                }
                                else
                                {
                                    attachPoint = "TL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_135),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_135),
                                                                0.0);
                                    textRotation = firstVectorAngle + Constants.Deg_180;
                                }
                                break;

                            case Angle.Plus_90_Degrees:
                                if (firstVectorLength < 5.1)
                                {
                                    attachPoint = "BR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_45),
                                                                     buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_45),
                                                                           0.0);
                                    textRotation = firstVectorAngle + Constants.Deg_90;
                                }
                                else
                                {
                                    attachPoint = "BL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = firstVectorAngle + Constants.Deg_180;
                                }
                                break;

                            case Angle.Plus_180_Degrees:
                                attachPoint = "TC";
                                textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_90),
                                                            buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_90),
                                                            0.0);
                                textRotation = firstVectorAngle + Constants.Deg_180;
                                break;
                        }
                    }
                    if (firstVectorAngle >= Constants.Deg_180 && firstVectorAngle < Constants.Deg_240)
                    {
                        switch (nextVectorAngle)
                        {
                            case Angle.Minus_90_Degrees:
                                if(firstVectorLength < 5.1)
                                {
                                    attachPoint = "TR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = firstVectorAngle + Constants.Deg_90;
                                }
                                else
                                {
                                    attachPoint = "TL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_135),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_135),
                                                                0.0);
                                    textRotation = firstVectorAngle - Constants.Deg_180;
                                }
                                break;

                            case Angle.Plus_90_Degrees:
                                if (firstVectorLength > 5.1)
                                {
                                    attachPoint = "TL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_45),
                                                                0.0);
                                    textRotation = firstVectorAngle + Constants.Deg_90;
                                }
                                else
                                {
                                    attachPoint = "BL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_135),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_135),
                                                                0.0);
                                    textRotation = firstVectorAngle - Constants.Deg_180;
                                }
                                break;

                            case Angle.Plus_180_Degrees:
                                attachPoint = "TC";
                                textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_90),
                                                            buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_90),
                                                            0.0);
                                textRotation = firstVectorAngle - Constants.Deg_180;
                                break;
                        }
                    }
                    if (firstVectorAngle >= Constants.Deg_240 && firstVectorAngle < Constants.Deg_270)
                    {
                        switch (nextVectorAngle)
                        {
                            case Angle.Minus_90_Degrees:
                                if(secondVectorLength < 5.1)
                                {
                                    attachPoint = "TL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_135),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_135),
                                                                0.0);
                                    textRotation = firstVectorAngle - Constants.Deg_180;
                                }
                                else
                                {
                                    attachPoint = "TR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = firstVectorAngle + Constants.Deg_90;
                                }
                                break;

                            case Angle.Plus_90_Degrees:
                                if (secondVectorLength < 5.1)
                                {
                                    attachPoint = "BL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_135),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_135),
                                                                0.0);
                                    textRotation = firstVectorAngle - Constants.Deg_180;
                                }
                                else
                                {
                                    attachPoint = "TL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_45),
                                                                0.0);
                                    textRotation = firstVectorAngle + Constants.Deg_90;
                                }
                                break;

                            case Angle.Plus_180_Degrees:
                                attachPoint = "TC";
                                textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_90),
                                                            buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_90),
                                                            0.0);
                                textRotation = firstVectorAngle;
                                break;
                        }
                    }
                    if (firstVectorAngle >= Constants.Deg_270 && firstVectorAngle < Constants.Deg_330)
                    {
                        switch (nextVectorAngle)
                        {
                            case Angle.Minus_90_Degrees:
                                if (secondVectorLength < 5.1)
                                {
                                    attachPoint = "BR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_225),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_225),
                                                                0.0);
                                    textRotation = firstVectorAngle;
                                }
                                else
                                {
                                    attachPoint = "TR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle - Constants.Deg_180;
                                }
                                break;

                            case Angle.Plus_90_Degrees:
                                
                                if (secondVectorLength < 5.1)
                                {
                                    attachPoint = "TR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_135),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_135),
                                                                0.0);
                                    textRotation = firstVectorAngle;
                                }
                                else
                                {
                                    attachPoint = "TL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle;
                                }
                                break;

                            case Angle.Plus_180_Degrees:
                                attachPoint = "TC";
                                textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_90),
                                                            buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_90),
                                                            0.0);
                                textRotation = firstVectorAngle;
                                break;
                        }
                    }
                    if (firstVectorAngle >= Constants.Deg_330 && firstVectorAngle < Constants.Deg_360)
                    {
                        switch (nextVectorAngle)
                        {
                            case Angle.Minus_90_Degrees:
                                if (firstVectorLength < 5.1)
                                {
                                    attachPoint = "TR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle - Constants.Deg_180;
                                }
                                else
                                {
                                    attachPoint = "BR";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_225),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_225),
                                                                0.0);
                                    textRotation = firstVectorAngle;
                                }
                                break;

                            case Angle.Plus_90_Degrees:
                                attachPoint = "TR";
                                if(firstVectorLength < 5.1)
                                {
                                    attachPoint = "TL";
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle + Constants.Deg_45),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle + Constants.Deg_45),
                                                                0.0);
                                    textRotation = secondVectorAngle;
                                }
                                else
                                {
                                    textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_135),
                                                                buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_135),
                                                                0.0);
                                    textRotation = firstVectorAngle;
                                }
                                break;

                            case Angle.Plus_180_Degrees:
                                attachPoint = "TC";
                                textInsPoint = new Point3d(buildingOutline.GetPoint3dAt(currVertex).X + Constants.TextOffset * Math.Cos(firstVectorAngle - Constants.Deg_90),
                                                            buildingOutline.GetPoint3dAt(currVertex).Y + Constants.TextOffset * Math.Sin(firstVectorAngle - Constants.Deg_90),
                                                            0.0);
                                textRotation = firstVectorAngle;
                                break;
                        }
                    }
                    // Add FFL on first iteration
                    if (index == 0)
                        FFLAdded = addFFL(outlineId, FFLObjIdCollection);
                    // Add text
                    textAdded = addText(textInsPoint, attachPoint, textRotation, strLevel, FFLObjIdCollection, currVertex);
                    // Add the point
                    pointAdded = addPoint(buildingOutline.GetPoint3dAt(currVertex), textRotation, FFLObjIdCollection);
                    // Add FFL text
                    if (FFLAdded == false || textAdded == false || pointAdded == false)
                        break;
                }
                acTrans.Commit();
                if (FFLAdded == false || textAdded == false || pointAdded == false)
                {
                    acEditor.WriteMessage("\nError adding text.");
                    return false;
                }
                return true;
            }
        }

        private static bool addFFL(ObjectId outlineId, ObjectIdCollection objectIdCollection)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;

            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                Polyline outline = acTrans.GetObject(outlineId, OpenMode.ForRead) as Polyline;
                // Get the extension dictionary
                ObjectId extDictId = outline.ExtensionDictionary;
                if (extDictId == ObjectId.Null)
                {
                    acEditor.WriteMessage("\nError cannot open extension dictionary.");
                    return false;
                }
                // Fetch the FFL value. This is stored in a Xrecord, key "FFL", in the extension dictionary.
                ResultBuffer xrecFFL = JPPUtils.getXrecord(extDictId, "FFL");
                if (xrecFFL == null)
                {
                    acEditor.WriteMessage("\nError cannot open Xrecord.");
                    return false;
                }
                TypedValue[] xrecFFLData = xrecFFL.AsArray();
                if ((int)xrecFFLData[0].TypeCode != (int)(DxfCode.ExtendedDataReal))
                {
                    acEditor.WriteMessage("\nError invalid data.");
                    return false;
                }
                var dbleFFL = (double)xrecFFLData[0].Value;
                // Format the double to a string with three decimal places, e.g. "12.234"
                string strFFL = dbleFFL.ToString("N3");
                string textToAdd = "FFL " + strFFL;
                // Also need the angle of the first segment of the building outline to the x-axis
                // in order to place the text at the right angle. This is stored in a Xrecord of the polyline.
                //
                ResultBuffer xRecAngle = JPPUtils.getXrecord(extDictId, "Rotation");
                if (xRecAngle == null)
                {
                    // Error occcured
                }
                TypedValue[] xrecAngle = xRecAngle.AsArray();
                if ((int)xrecAngle[0].TypeCode != (int)(DxfCode.ExtendedDataReal))
                {
                    // Not real data so invalid Xrecord data
                }
                // Check the length of the final vector. and make sure it's long enough to accommodate the 
                // FFL text. If so, rotate the FFL text clockwise 90 degrees, if not rotate 180 degrees.
                double lastVectorLen = outline.GetPoint2dAt(0).GetDistanceTo(outline.GetPoint2dAt(outline.NumberOfVertices - 1));
                double dbleAngle = 0.0;
                if (lastVectorLen < 5.1)
                    dbleAngle = (double)xrecAngle[0].Value - Constants.Deg_180;
                else
                    dbleAngle = (double)xrecAngle[0].Value - Constants.Deg_90;

                /* if (dbleAngle > Math.PI/2)
                {
                    dbleAngle = dbleAngle - Math.PI / 2;
                } */
                // Final information required for the FFL Text is its insertion point. Initally this will
                // be at the centre of the building outline bounding box.
                Extents3d outlineExtents = outline.GeometricExtents;
                Point3d textInsPt = new Point3d(outlineExtents.MinPoint.X + (outlineExtents.MaxPoint.X - outlineExtents.MinPoint.X) / 2,
                                                outlineExtents.MinPoint.Y + (outlineExtents.MaxPoint.Y - outlineExtents.MinPoint.Y) / 2,
                                                0.0);

                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                                OpenMode.ForWrite) as BlockTableRecord;
                TextStyleTable acTextStyleTable;
                acTextStyleTable = acTrans.GetObject(acCurrDb.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                TextStyleTableRecord acTxtStyleRecord;
                acTxtStyleRecord = acTrans.GetObject(acTextStyleTable[StyleNames.JPP_App_Text_Style],
                                                                OpenMode.ForRead) as TextStyleTableRecord;
                using (MText acMtext = new MText())
                {
                    acMtext.Layer = StyleNames.JPP_APP_FFLs_Layer;
                    acMtext.TextStyleId = acTxtStyleRecord.ObjectId;
                    acMtext.Location = textInsPt;
                    acMtext.Width = 6.25;
                    acMtext.Height = 0.75;
                    acMtext.TextHeight = 0.7;
                    acMtext.Rotation = dbleAngle;
                    acMtext.Contents = textToAdd;
                    acMtext.Attachment = AttachmentPoint.MiddleCenter;
                    acBlkTblRec.AppendEntity(acMtext);
                    // Add the new MTextobject id to the object ids collection
                    objectIdCollection.Add(acMtext.ObjectId);
                    acTrans.AddNewlyCreatedDBObject(acMtext, true);
                }
                acTrans.Commit();
                return true;
            }
        }
 
        private static bool addText(Point3d insPoint, string attPoint, double rotation, string contents, 
                                        ObjectIdCollection objectIdCollection, int vertexIndex)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;
            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                                OpenMode.ForWrite) as BlockTableRecord;
                TextStyleTable acTextStyleTable;
                acTextStyleTable  = acTrans.GetObject(acCurrDb.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                TextStyleTableRecord acTxtStyleRecord;
                acTxtStyleRecord = acTrans.GetObject(acTextStyleTable[StyleNames.JPP_App_Text_Style], 
                                                                OpenMode.ForRead) as TextStyleTableRecord;
                using (MText acMtext = new MText())
                {
                    try
                    {
                        acMtext.Layer = StyleNames.JPP_APP_Levels_Layer;
                        acMtext.TextStyleId = acTxtStyleRecord.ObjectId;
                        acMtext.Location = insPoint;
                        acMtext.Width = 2.5;
                        acMtext.Height = 0.6;
                        acMtext.TextHeight = 0.4;
                        acMtext.Rotation = rotation;
                        acMtext.Contents = contents;
                        if (attPoint == "TL")
                            acMtext.Attachment = AttachmentPoint.TopLeft;
                        else if (attPoint == "TC")
                            acMtext.Attachment = AttachmentPoint.TopCenter;
                        else if (attPoint == "TR")
                            acMtext.Attachment = AttachmentPoint.TopRight;
                        else if (attPoint == "BL")
                            acMtext.Attachment = AttachmentPoint.BottomLeft;
                        else if (attPoint == "BC")
                            acMtext.Attachment = AttachmentPoint.BottomCenter;
                        else if (attPoint == "BR")
                            acMtext.Attachment = AttachmentPoint.BottomRight;
                        else
                            acMtext.Attachment = AttachmentPoint.MiddleCenter;
                        acBlkTblRec.AppendEntity(acMtext);
                        acTrans.AddNewlyCreatedDBObject(acMtext, true);
                        objectIdCollection.Add(acMtext.ObjectId);
                        // Add the Xdata
                        RegAppTable acRegAppTbl = acTrans.GetObject(acCurrDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                        ResultBuffer resultBuff = new ResultBuffer();
                        resultBuff.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, JPP_App_Config_Params.JPP_APP_NAME));
                        resultBuff.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, vertexIndex));
                        acMtext.XData = resultBuff;

                        acTrans.Commit();
                        return true;
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception acException)
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                    ("The following exception was caught: \n" + acException.Message
                                                            + "\nError adding MText data!\n");
                        acTrans.Dispose();
                        acTrans.Commit();
                        return false;
                    }
                }
            }
        }

        private static bool addPoint(Point3d insPt, double vectorAngle, ObjectIdCollection objectIdCollection)
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database acCurrDb = acDoc.Database;
            Editor acEditor = acDoc.Editor;
            using (Transaction acTrans = acCurrDb.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurrDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                                OpenMode.ForWrite) as BlockTableRecord;
                bool pointAdded = false;
                try
                {
                    // 'Normalise' the vector angle to be in the range 0 <= vectorAngle <= 90
                    if (vectorAngle <= Constants.Deg_180)
                        vectorAngle -= Constants.Deg_90;
                    else if (vectorAngle <= Constants.Deg_270)
                        vectorAngle -= Constants.Deg_180;
                    else
                        vectorAngle -= Constants.Deg_270;

                    double sinVectorAngle = Math.Sin(vectorAngle);
                    double cosVectorAngle = Math.Cos(vectorAngle);
                    double halfLineLen = Constants.JPP_App_Pt_Len/2.0;
                    // Define the lines
                    Line acLine1 = new Line(new Point3d(insPt.X - (halfLineLen * cosVectorAngle), insPt.Y - (halfLineLen * sinVectorAngle), 0.0),
                                           new Point3d(insPt.X + (halfLineLen * cosVectorAngle), insPt.Y + (halfLineLen * sinVectorAngle), 0.0));
                    acLine1.Layer = StyleNames.JPP_APP_Levels_Layer;
                    Line acLine2 = new Line(new Point3d(insPt.X + (halfLineLen * sinVectorAngle), insPt.Y - (halfLineLen * cosVectorAngle), 0.0),
                                           new Point3d(insPt.X - (halfLineLen * sinVectorAngle), insPt.Y + (halfLineLen * cosVectorAngle), 0.0));
                    acLine2.Layer = StyleNames.JPP_APP_Levels_Layer;

                    /* if (vectorAngle <= Constants.Deg_90)
                    {
                        Line acLine1 = new Line(new Point3d(insPt.X - (halfLineLen * cosVectorAngle), insPt.Y - (halfLineLen * sinVectorAngle), 0.0),
                                               new Point3d(insPt.X + (halfLineLen * cosVectorAngle), insPt.Y + (halfLineLen * sinVectorAngle), 0.0));
                        Line acLine2 = new Line(new Point3d(insPt.X + (halfLineLen * sinVectorAngle), insPt.Y - (halfLineLen * cosVectorAngle), 0.0),
                                               new Point3d(insPt.X - (halfLineLen * sinVectorAngle), insPt.Y + (halfLineLen * cosVectorAngle), 0.0));
                    }
                    else if (vectorAngle <= Constants.Deg_180)
                    {
                        Line acLine1 = new Line(new Point3d(insPt.X - (halfLineLen * sinVectorAngle), insPt.Y + (halfLineLen * cosVectorAngle), 0.0),
                                               new Point3d(insPt.X + (halfLineLen * sinVectorAngle), insPt.Y - (halfLineLen * cosVectorAngle), 0.0));
                        Line acLine2 = new Line(new Point3d(insPt.X + (halfLineLen * cosVectorAngle), insPt.Y - (halfLineLen * sinVectorAngle), 0.0),
                                               new Point3d(insPt.X - (halfLineLen * cosVectorAngle), insPt.Y + (halfLineLen * sinVectorAngle), 0.0));
                    }
                    else if (vectorAngle <= Constants.Deg_270)
                    {
                        Line acLine1 = new Line(new Point3d(insPt.X - (halfLineLen * sinVectorAngle), insPt.Y + (halfLineLen * cosVectorAngle), 0.0),
                                               new Point3d(insPt.X + (halfLineLen * sinVectorAngle), insPt.Y - (halfLineLen * cosVectorAngle), 0.0));
                        Line acLine2 = new Line(new Point3d(insPt.X + (halfLineLen * cosVectorAngle), insPt.Y + (halfLineLen * sinVectorAngle), 0.0),
                                               new Point3d(insPt.X - (halfLineLen * cosVectorAngle), insPt.Y - (halfLineLen * sinVectorAngle), 0.0));
                    }
                    else 
                    {
                        Line acLine1 = new Line(new Point3d(insPt.X + (halfLineLen * sinVectorAngle), insPt.Y - (halfLineLen * cosVectorAngle), 0.0),
                                               new Point3d(insPt.X - (halfLineLen * sinVectorAngle), insPt.Y + (halfLineLen * cosVectorAngle), 0.0));
                        Line acLine2 = new Line(new Point3d(insPt.X - (halfLineLen * cosVectorAngle), insPt.Y + (halfLineLen * sinVectorAngle), 0.0),
                                               new Point3d(insPt.X + (halfLineLen * cosVectorAngle), insPt.Y - (halfLineLen * sinVectorAngle), 0.0));
                    } */

                    acBlkTblRec.AppendEntity(acLine1);
                    objectIdCollection.Add(acLine1.ObjectId);
                    acTrans.AddNewlyCreatedDBObject(acLine1, true);
                    acBlkTblRec.AppendEntity(acLine2);
                    objectIdCollection.Add(acLine2.ObjectId);
                    acTrans.AddNewlyCreatedDBObject(acLine2, true);
                    pointAdded = true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception acException)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog
                                                ("The following exception was caught: \n" + acException.Message
                                                        + "\nError adding point.\n");
                }
                acTrans.Commit();
                if (!pointAdded)
                    return false;
                return true;
            }

        }
    }
}
