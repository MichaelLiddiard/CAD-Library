using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.BoundaryRepresentation;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: CommandClass(typeof(JPP.Civils.DrainageNetwork))]

namespace JPP.Civils
{
    class DrainageNetwork
    {
        public List<DrainageNode> InputNodes;
        public DrainageNode Outfall;
        public SparseGridArray<byte> Costs;

        public bool DebugGraphics = true;

        private Curve boundary;

        public DrainageNetwork()
        {
            InputNodes = new List<DrainageNode>();
            Costs = new SparseGridArray<byte>();
        }

        public void Generate(float terminationDelta)
        {
            //Validate input data
            if (boundary == null)
                throw new ArgumentException("Boundary has not been set.");

            //Do one calculation step
            float delta = float.PositiveInfinity;
            float lastCost = float.PositiveInfinity;

            int loopCount = 0;

            while(delta > terminationDelta)
            {
                //Hardcode an exit for non-collapsing solution
                if(loopCount > 1)
                {
                    break;
                }

                float cost = CalculationStage();
                delta = lastCost - cost;
                lastCost = cost;
                loopCount++;
            }

            //Review results

            //Final Code if done
        }

        private float CalculationStage()
        {
            //Iterate over each Start point and generate path
            float totalCost = 0;
            object costLocker = new object();
            /*foreach(DrainageNode n in InputNodes)
            {
                float path = Search(n); //totalCost += Search(n);
                lock (costLocker)
                {
                    totalCost += path;
                }
            }*/
            Parallel.ForEach(InputNodes, n =>
            {
                float path = Search(n); //totalCost += Search(n);
                lock (costLocker)
                {
                    totalCost += path;
                }
            });

            //Update screen with paths
            foreach(DrainageNode n in InputNodes)
            {
                DrawPath(n);
            }

            return totalCost;
        }

        private void DrawPath(Node n)
        {
            if (n.Child != null)
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                Transaction trans = acCurDb.TransactionManager.TopTransaction;

                Polyline pline = new Polyline();
                pline.AddVertexAt(0, new Point2d(n.X, n.Y), 0, 0, 0);
                pline.AddVertexAt(1, new Point2d(n.Child.X, n.Child.Y), 0, 0, 0);

                // Open the Block table for read
                BlockTable acBlkTbl = trans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;               
                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                acBlkTblRec.AppendEntity(pline);
                trans.AddNewlyCreatedDBObject(pline, true);

                DrawPath(n.Child);
            }
        }

        private void DrawNode(Node n)
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
            {

                Circle c = new Circle();
                c.Diameter = 0.5f;
                c.Center = new Point3d(n.X, n.Y, 0);

                // Open the Block table for read
                BlockTable acBlkTbl = trans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                acBlkTblRec.AppendEntity(c);
                trans.AddNewlyCreatedDBObject(c, true);

                trans.Commit();
            }

            acDoc.TransactionManager.EnableGraphicsFlush(true);
            acDoc.TransactionManager.QueueForGraphicsFlush();
            Autodesk.AutoCAD.Internal.Utils.FlushGraphics();

        }

        public void SetBoundary(Curve c)
        {
            if (!c.Closed)
            {
                throw new ArgumentException("Curve is not closed and does not form a boundary");
            }
            boundary = c;
        }

        public void SetCost(Hatch h, byte Cost)
        {

        }

        private float Search(DrainageNode startNode)
        {
            List<Node> openNodes = new List<Node>();
            List<Node> closedNodes = new List<Node>();
            openNodes.Add(startNode);
            Node currentNode = null;
            while (openNodes.Count > 0)
            {
                Node lastNode = currentNode;

                openNodes = openNodes.OrderBy(o => o.F).ToList();

                currentNode = openNodes[0];
                openNodes.RemoveAt(0);
                if (DebugGraphics)
                    DrawNode(currentNode);                

                if(lastNode != null)
                    lastNode.Child = currentNode;

                //Check to see if we are at the target point
                //TODO: Is this right? Use F score???
                if(currentNode.Equals(Outfall))
                {
                    break;
                }

                List<Node> adjacents = GetAdjacentWalkableNodes(currentNode);
                //Estimate code from current node to next
                //TODO: Reverse for speed??
                //Check against open nodes
                foreach(Node n in adjacents)
                {
                    bool found = false;
                    
                    for (int i = 0; i < openNodes.Count; i++)
                    {
                        if(n.Equals(openNodes[i]))
                        {
                            found = true;
                            if(openNodes[i].G > n.G)
                            {
                                openNodes[i] = n;
                            }
                        }
                    }

                    //Check against closed nodes
                    for (int i = 0; i < closedNodes.Count; i++)
                    {
                        if (n.Equals(closedNodes[i]))
                        {
                            if (closedNodes[i].G > n.G)
                            {
                                closedNodes.RemoveAt(i);
                                openNodes.Add(n);
                            }

                            found = true;
                        }
                    }

                    if (!found)
                    {
                        openNodes.Add(n);
                    }
                }            

                closedNodes.Add(currentNode);
            }
            currentNode.Child = Outfall;
            return Outfall.F;
        }               

        private List<Node> GetAdjacentWalkableNodes(Node fromNode)
        {
            List<Node> walkableNodes = new List<Node>();                       

            walkableNodes.Add(new Node()
            {
                X = fromNode.X - 1,
                Y = fromNode.Y - 1
            });
            walkableNodes.Add(new Node()
            {
                X = fromNode.X,
                Y = fromNode.Y - 1
            });
            walkableNodes.Add(new Node()
            {
                X = fromNode.X + 1,
                Y = fromNode.Y - 1
            });
            walkableNodes.Add(new Node()
            {
                X = fromNode.X - 1,
                Y = fromNode.Y
            });
            walkableNodes.Add(new Node()
            {
                X = fromNode.X + 1,
                Y = fromNode.Y
            });
            walkableNodes.Add(new Node()
            {
                X = fromNode.X - 1,
                Y = fromNode.Y + 1
            });
            walkableNodes.Add(new Node()
            {
                X = fromNode.X,
                Y = fromNode.Y + 1
            });
            walkableNodes.Add(new Node()
            {
                X = fromNode.X + 1,
                Y = fromNode.Y + 1
            });

            //Get cost
            List<int> blocked = new List<int>();
            for(int i = 0; i < 8; i++)
            {
                int cost = GetCost(fromNode, walkableNodes[i]);
                if (cost < 0 || !NodeWithinBounds(walkableNodes[i]))
                {
                    blocked.Add(i);
                }
                walkableNodes[i].G = fromNode.G + cost;
                walkableNodes[i].H = (float)Math.Sqrt(Math.Pow(Outfall.X - walkableNodes[i].X, 2) + Math.Pow(Outfall.Y - walkableNodes[i].Y, 2));
            }

            var sortedBlocked = blocked.OrderBy(o => o).ToArray();

            //TODO: Verify this works
            for (int i = 0; i < sortedBlocked.Count(); i++)
            {
                walkableNodes.RemoveAt(sortedBlocked[i] - i);
            }

            return walkableNodes;
        }

        private int GetCost(Node from, Node to)
        {
            double modifier = 1;
            //Check diagonal
            if(from.X != to.X && from.Y != to.Y)
            {
                modifier = Math.Sqrt(2);
            }

            return (int)Math.Round(Costs[to.X, to.Y] * modifier);            
        }

        private bool NodeWithinBounds(Node n)
        {
            //The blow throws errors, trying intersection method instead
            Point2d testPoint = new Point2d(n.X, n.Y);
            /*Point3d testPoint = new Point3d(n.X, n.Y, 0);
            DBObjectCollection bounds = new DBObjectCollection();
            bounds.Add(boundary);
            var region = Region.CreateFromCurves(bounds)[0];
            Brep brep = new Brep(region as Entity);
            PointContainment containment;
            brep.GetPointContainment(testPoint, out containment);

            if(containment == PointContainment.Outside)
            {
                return false;
            }
            return true;*/

            Polyline testline = new Polyline();
            testline.AddVertexAt(0, testPoint, 0, 0, 0);
            testline.AddVertexAt(1, new Point2d(0, 0), 0, 0, 0);

            Point3dCollection intersections = new Point3dCollection();
            testline.IntersectWith(boundary, Intersect.OnBothOperands, intersections, IntPtr.Zero, IntPtr.Zero);
            if (intersections.Count % 2 == 0)
            {
                return false;
            }
            return true;
        }

        [CommandMethod("C_D_GenerateNetwork")]
        public static void GenerateNetowrkCommand()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            DrainageNetwork dn = new DrainageNetwork();

            PromptSelectionResult acSSPrompt = acDoc.Editor.GetSelection();

            using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
            {
                // If the prompt status is OK, objects were selected
                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    SelectionSet acSSet = acSSPrompt.Value;

                    // Step through the objects in the selection set
                    foreach (SelectedObject acSSObj in acSSet)
                    {
                        DBObject ent = trans.GetObject(acSSObj.ObjectId, OpenMode.ForRead);
                        if (ent is Curve)
                        {
                            dn.SetBoundary(ent as Curve);
                        }
                    }
                }

                PromptPointResult pPtRes;
                PromptPointOptions pPtOpts = new PromptPointOptions("");
                                
                while (true)
                {
                    // Prompt for the start point
                    pPtOpts.Message = "\nEnter the drainage entry point: ";
                    pPtRes = acDoc.Editor.GetPoint(pPtOpts);

                    if (pPtRes.Status != PromptStatus.OK)                        
                        break;

                    Point3d ptStart = pPtRes.Value;
                    dn.InputNodes.Add(new DrainageNode()
                    {
                        X = (int)Math.Round(ptStart.X),
                        Y = (int)Math.Round(ptStart.Y)
                    });
                }

                pPtOpts.Message = "\nEnter the drainage outfall point: ";
                pPtRes = acDoc.Editor.GetPoint(pPtOpts);
                Point3d ptEnd = pPtRes.Value;                

                dn.Outfall =new DrainageNode()
                {
                    X = (int)Math.Round(ptEnd.X),
                    Y = (int)Math.Round(ptEnd.Y)
                };

                //dn.InputNodes.Add()
                dn.Generate(50);

                trans.Commit();
            }
        }
    }
}
