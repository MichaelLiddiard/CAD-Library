using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: CommandClass(typeof(JPP.Civils.Pipe))]

namespace JPP.Civils
{
    public class Pipe
    {
        [CommandMethod("LayPipe")]
        public static void Lay()
        {
            bool promptGradient = false;.
            int gradient;

            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            PromptKeywordOptions pKeyOpts = new PromptKeywordOptions("");
            pKeyOpts.Message = "\nPlease enter mode";
            pKeyOpts.Keywords.Add("Gradient");
            pKeyOpts.Keywords.Add("Storm Minimum");
            pKeyOpts.Keywords.Add("Foul Minimum");
            pKeyOpts.Keywords.Default = "Storm Minimum";
            pKeyOpts.AllowNone = true;
            PromptResult pKeyRes = acDoc.Editor.GetKeywords(pKeyOpts);
            switch (pKeyRes.StringResult)
            {
                case "Gradient":
                    promptGradient = true;
                    break;

                case "Storm Minimum":
                    gradient = 100;
                    break;

                case "Foul Minimum":
                    gradient = 80;
                    break;
            }


        }

        [CommandMethod("AnnotatePipe")]
        public static void Annotate()
        {

        }       
    }
}
