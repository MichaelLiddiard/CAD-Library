using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using System;

namespace JPP.Core
{
    public class RibbonCommandHandler : System.Windows.Input.ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true; //return true means the button always enabled
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            RibbonCommandItem cmd = parameter as RibbonCommandItem;
            Document dwg = Application.DocumentManager.MdiActiveDocument;
            dwg.SendStringToExecute((string)cmd.CommandParameter, true, false, false);
        }
    }
}
