using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPP.Civils.Drainage;

namespace JPP.Civils
{
    class DrainageNetwork
    {
        public static DrainageNetwork Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new DrainageNetwork();
                }

                return _Current;
            }
        }

        static DrainageNetwork _Current;

        public List<DrainageNode> InputNodes;
        public DrainageNode Outfall;
        public int[,] Costs;

        public IDrainageStandard Standard;

        public DrainageNetwork()
        {
            Standard = new UnitedUtilities();
        }

        private bool Search(Node current)
        {
            //List<Node> nextNodes = GetAdjacentWalkableNodes(current);
            //nextNodes.Sort(())

            return true;
        }

        /*private List<Node> GetAdjacentWalkableNodes(Node fromNode)
        {
            /*List<Node> walkableNodes = new List<Node>();
            IEnumerable<Point> nextLocations = GetAdjacentLocations(fromNode);

            foreach (var location in nextLocations)
            {
                int x = location.X;
                int y = location.Y;

                // Stay within the grid's boundaries
                if (x < 0 || x >= this.width || y < 0 || y >= this.height)
                    continue;

                Node node = this.nodes[x, y];
                // Ignore non-walkable nodes
                if (!node.IsWalkable)
                    continue;

                // Ignore already-closed nodes
                if (node.State == NodeState.Closed)
                    continue;

                // Already-open nodes are only added to the list if their G-value is lower going via this route.
                if (node.State == NodeState.Open)
                {
                    float traversalCost = Node.GetTraversalCost(node.Location, node.ParentNode.Location);
                    float gTemp = fromNode.G + traversalCost;
                    if (gTemp < node.G)
                    {
                        node.ParentNode = fromNode;
                        walkableNodes.Add(node);
                    }
                }
                else
                {
                    // If it's untested, set the parent and flag it as 'Open' for consideration
                    node.ParentNode = fromNode;
                    node.State = NodeState.Open;
                    walkableNodes.Add(node);
                }
            }

            return walkableNodes;
        }*/
    }
}
