using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPP.Core;

namespace JPP.Civils.Drainage
{
    class SewersForAdoption7th : IDrainageStandard
    {
        public virtual void VerifyManhole(Manhole manhole)
        {
            //Check diamater
            //B 3.2 12
            int minimumDiameter = 0;
            if(manhole.LargestInternalPipeDiameter < 375) 
                    minimumDiameter = 1200;
            if (manhole.LargestInternalPipeDiameter >= 375 && manhole.LargestInternalPipeDiameter < 450)
                minimumDiameter = 1350;
            if (manhole.LargestInternalPipeDiameter >= 450 && manhole.LargestInternalPipeDiameter < 700)
                minimumDiameter = 1500;
            if (manhole.LargestInternalPipeDiameter >= 700 && manhole.LargestInternalPipeDiameter < 900)
                minimumDiameter = 1800;
            if (manhole.LargestInternalPipeDiameter >= 900)
                minimumDiameter = manhole.LargestInternalPipeDiameter + 900;

            if (manhole.Diameter < minimumDiameter)
            {
                throw new ArgumentException("Manhole does not meet minimum dimensions size for pipes - increase to " + minimumDiameter);
            }
            
        }
    }
}
