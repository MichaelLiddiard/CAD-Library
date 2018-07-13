using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils.Drainage
{
    class UnitedUtilities : SewersForAdoption7th
    {
        public override void VerifyManhole(Manhole manhole)
        {
            base.VerifyManhole(manhole);

            //Additional Checks
            SetManholeType(manhole);
            if(manhole.LargestInternalPipeDiameter <= 525)
            {
                manhole.SafetyChain = false;
                manhole.SafetyRail = false;
            } else
            {
                manhole.SafetyChain = true;
                manhole.SafetyRail = true;
            }

            //Set benching
            if(manhole.LargestInternalPipeDiameter <= 375)
            {
                manhole.MinimumMajorBenching = 600;
            } else
            {
                if (manhole.LargestInternalPipeDiameter <= 525)
                {
                    manhole.MinimumMajorBenching = 750;
                }
                else
                {
                    manhole.MinimumMajorBenching = 1100;
                }
            }
        }

        public virtual void SetManholeType(Manhole manhole)
        {
            if(manhole.LargestInternalPipeDiameter <= 525 && manhole.DepthToSoffitLevel < 6)
            {
                manhole.Type = "TYPE 1";
                manhole.MinimumMinorBenching = 325;
            } else
            {
                throw new ArgumentException("Manhole not Type 1 compliant");
            }
        }
    }
}
