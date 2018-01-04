using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JPP.Civils
{
    public class WallJoint
    {
        public float X, Y, Z;

        [XmlIgnore]
        public List<WallSegment> Segments;        
    }
}
