using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace JPP.Core
{
    public class ClickOverride
    {
        public static ClickOverride Current
        {
            get
            {
                if(_Current == null)
                {
                    _Current = new ClickOverride();                
                }
                return _Current;
            }
        }

        List<IClickOverrideInstance> Overrides;

        string _customCommand = null;
        bool _customFound = false;
        ObjectId _selected;

        private static ClickOverride _Current;

        public ClickOverride()
        {
            Overrides = new List<IClickOverrideInstance>();

            Autodesk.AutoCAD.ApplicationServices.Application.BeginDoubleClick += Application_BeginDoubleClick;
            Application.DocumentManager.DocumentLockModeChanged += DocumentManager_DocumentLockModeChanged;
            Application.DocumentManager.DocumentLockModeChangeVetoed += DocumentManager_DocumentLockModeChangeVetoed;
        }

        private void DocumentManager_DocumentLockModeChangeVetoed(object sender, DocumentLockModeChangeVetoedEventArgs e)
        {
            if(_customFound)
            {
                Launch();
            }
        }

        private void DocumentManager_DocumentLockModeChanged(object sender, DocumentLockModeChangedEventArgs e)
        {            
            if(e.GlobalCommandName.Length > 0)
            {
                if (_customFound && e.GlobalCommandName.ToUpper() != _customCommand.ToUpper())
                {
                    e.Veto();
                } else
                {
                    _customFound = false;
                }
            }
        }

        private void Application_BeginDoubleClick(object sender, BeginDoubleClickEventArgs e)
        {
            _customCommand = null;
            _customFound = false;
            //Get entity which user double-clicked on
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //PromptSelectionResult res = ed.SelectAtPickBox(e.Location);
            PromptSelectionResult res = ed.GetSelection();
            if (res.Status == PromptStatus.OK)
            {
                ObjectId[] ids = res.Value.GetObjectIds();

                //Only when there is one entity selected, we go ahead to see
                //if there is a custom command supposed to target at this entity
                if (ids.Length == 1)
                {
                    using (Transaction tr = ids[0].Database.TransactionManager.StartTransaction())
                    {

                        foreach (IClickOverrideInstance oci in Overrides)
                        {
                            if (oci.CanHandle(tr.GetObject(ids[0], OpenMode.ForRead)))
                            {
                                _customCommand = oci.CommandName();
                                _customFound = true;
                                _selected = ids[0];
                            }
                        }

                        if (_customFound)
                        {
                            //Check if an overriden type exists
                            if ((int)Application.GetSystemVariable("DBLCLKEDIT") == 0)
                            {
                                //Not enabled so launch custom
                                Launch();
                            }
                            else
                            {
                                //block custom command in the event handlers
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void Launch()
        {
            Application.DocumentManager.MdiActiveDocument.Editor.SetImpliedSelection(new ObjectId[] { _selected });
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute(_customCommand + " ", true, false, false);
        }

        public void Add(IClickOverrideInstance oci)
        {
            Overrides.Add(oci);
        }
    }
}
