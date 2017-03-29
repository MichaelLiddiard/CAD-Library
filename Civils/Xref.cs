using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: CommandClass(typeof(JPP.Civils.Xref))]

namespace JPP.Civils
{    
    class Xref
    {
        [CommandMethod("ImportAsXref")]
        public static void ImportAsXref()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

            //Make all the drawing changes

            using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
            {

                //Get all model space drawing objects
                TypedValue[] tv = new TypedValue[1];
                tv.SetValue(new TypedValue(67, 0), 0);
                SelectionFilter sf = new SelectionFilter(tv);
                PromptSelectionResult psr = acDoc.Editor.SelectAll(sf);

                foreach (SelectedObject so in psr.Value)
                {
                    //For each object set its color, transparency, lineweight and linetype to ByLayer
                    Entity obj = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                    obj.ColorIndex = 256;
                    obj.LinetypeId = acDoc.Database.Celtype;
                    obj.LineWeight = acDoc.Database.Celweight;

                    //Adjust Z values

                }

                // Open the Layer table for read
                LinetypeTable acLinTbl;
                acLinTbl = tr.GetObject(acDoc.Database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                Byte alpha = (Byte)(255 * (1));
                Transparency trans = new Transparency(alpha);

                //Iterate over all layer and set them to color 8, 0 transparency and continuous linetype
                // Open the Layer table for read
                LayerTable acLyrTbl = tr.GetObject(acDoc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId id in acLyrTbl)
                {
                    LayerTableRecord ltr = tr.GetObject(id, OpenMode.ForWrite) as LayerTableRecord;
                    ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByColor, 8);
                    ltr.LinetypeObjectId = acLinTbl["Continuous"];
                    ltr.Transparency = trans;

                }

                //Change all text to Romans

                tr.Commit();
            }
            
            //Prompt for the save location
            sfd.Filter = "Drawing File|*.dwg";
            sfd.Title = "Save drawing as";
            sfd.ShowDialog();
            if(sfd.FileName != "")
            {
                acDoc.Database.SaveAs(sfd.FileName, Autodesk.AutoCAD.DatabaseServices.DwgVersion.Current);
            }

            //Close the original file as its no longer needed
        }
    }
}
