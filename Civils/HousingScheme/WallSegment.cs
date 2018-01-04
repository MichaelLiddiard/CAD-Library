using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils
{
    [Serializable]
    public class WallSegment
    {
        public WallJoint StartJoint;

        public WallJoint EndJoint;
    }
}
