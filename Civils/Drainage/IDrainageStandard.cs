using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils.Drainage
{
    interface IDrainageStandard
    {
        void VerifyManhole(Manhole manhole);

        void SetManholeType(Manhole manhole);
    }
}
