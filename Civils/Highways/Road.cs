using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using JPP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

[assembly: CommandClass(typeof(JPP.Civils.Highways.Road))]

namespace JPP.Civils.Highways
{
    public class Road
    {
        public long CentrelinePtr { get; set; }

        public float OverallWidth { get; set; }

        [XmlIgnore]
        public ObjectId Centreline
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(CentrelinePtr), 0);
            }
            set
            {
                CentrelinePtr = value.Handle.Value;

            }
        }

        /*public long KerbOnePtr { get; set; }

        [XmlIgnore]
        public ObjectId KerbOne
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(KerbOnePtr), 0);
            }
            set
            {
                KerbOnePtr = value.Handle.Value;

            }
        }

        public long KerbTwoPtr { get; set; }

        [XmlIgnore]
        public ObjectId KerbTwo
        {
            get
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;
                return acCurDb.GetObjectId(false, new Handle(KerbTwoPtr), 0);
            }
            set
            {
                KerbTwoPtr = value.Handle.Value;

            }
        }*/

        [CommandMethod("C_H_AddRoad")]
        public static void AddRoadCommand()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            DrainageNetwork dn = new DrainageNetwork();

            PromptSelectionOptions pso = new PromptSelectionOptions();            
            pso.SingleOnly = true;
            pso.MessageForAdding = "Select road centreline";
            PromptSelectionResult acSSPrompt = acDoc.Editor.GetSelection(pso);

            using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
            {
                // If the prompt status is OK, objects were selected
                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    SelectionSet acSSet = acSSPrompt.Value;


                    DBObject ent = trans.GetObject(acSSet[0].ObjectId, OpenMode.ForRead);
                    if (ent is Curve)
                    {
                        Road r = new Road();
                        r.Centreline = ent.ObjectId;

                        //TODO: Process centreline start is at end of drain runs etc. I.E. End of road is deeper in site hierarchy

                        PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter road width: ");
                        pStrOpts.AllowSpaces = false;
                        PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);
                        r.OverallWidth = float.Parse(pStrRes.StringResult);

                        /*pso.MessageForAdding = "Select kerbs";
                        pso.SingleOnly = false;
                        acSSPrompt = acDoc.Editor.GetSelection(pso);

                        // If the prompt status is OK, objects were selected
                        if (acSSPrompt.Status == PromptStatus.OK)
                        {
                            acSSet = acSSPrompt.Value;
                            DBObject ent1 = trans.GetObject(acSSet[0].ObjectId, OpenMode.ForRead);
                            DBObject ent2 = trans.GetObject(acSSet[1].ObjectId, OpenMode.ForRead);

                            if(ent1 is Curve && ent2 is Curve)
                            {
                                r.KerbOne = ent1.ObjectId;
                                r.KerbTwo = ent2.ObjectId;

                                r.OverallWidth = r.KerbOne.
                            }
                        }*/
                                                
                        acDoc.GetDocumentStore<CivilDocumentStore>().Roads.Add(r);
                    }
                }
            }
        }
    }
}
