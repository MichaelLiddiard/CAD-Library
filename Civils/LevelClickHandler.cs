using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using JPP.Core;

namespace JPP.Civils
{
    class LevelClickHandler : IClickOverrideInstance
    {
        public bool CanHandle(DBObject obj)
        {
            if(obj is BlockReference)
            {
                BlockReference blockRef = obj as BlockReference;
                if(blockRef.BlockName == "ProposedLevel")
                {
                    return true;
                }
            }

            return false;
        }

        public string CommandName()
        {
            throw new NotImplementedException();
        }
    }
}
