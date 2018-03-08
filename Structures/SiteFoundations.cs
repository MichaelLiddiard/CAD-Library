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

namespace JPP.CivilStructures
{
    class SiteFoundations
    {
        /// <summary>
        /// Safe ground bearing pressure in kN/m2
        /// </summary>
        public int GroundBearingPressure { get; set; }

        /// <summary>
        /// Default width for all foundations
        /// </summary>
        public float DefaultWidth { get; set; }

        public List<NHBCTree> Trees;

        public float StartDepth;

        public float Step;

        public Shrinkage SoilShrinkage;

        public SiteFoundations()
        {
            Trees = new List<NHBCTree>();
        }

        private void GenerateTreeRings()
        {
            //Generate the rings for each tree
            foreach(NHBCTree tree in Trees)
            {
                DBObjectCollection collection = tree.DrawRings(SoilShrinkage, StartDepth, Step);
            }

            //Combine the rings
        }


        [CommandMethod("CS_NewTree")]
        public static void NewPlot()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            NHBCTree newTree = new NHBCTree();

            //TODO: Add tree determination in here
            PromptStringOptions pStrOptsPlot = new PromptStringOptions("\nEnter tree height: ") { AllowSpaces = true };
            PromptResult pStrResPlot = acDoc.Editor.GetString(pStrOptsPlot);


            SiteFoundations sf = acDoc.GetDocumentStore<CivilStructureDocumentStore>().SiteFoundations;
            sf.Trees.Add(newTree);

            sf.GenerateTreeRings();
        }
    }

    public enum Shrinkage
    {
        Low,
        Medium,
        High
    }
}
