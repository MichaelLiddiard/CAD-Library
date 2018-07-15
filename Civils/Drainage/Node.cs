using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils
{
    class Node : IEquatable<Node>
    {
        public int X;
        public int Y;

        public float G;
        public float H;
        public float F { get { return G + H; } }

        public bool Equals(Node other)
        {
            return (X == other.X && Y == other.Y);
        }

        public Node Parent;
    }
}
