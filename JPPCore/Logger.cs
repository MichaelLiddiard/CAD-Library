using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Core
{
    public class Logger
    {
        public static void Log(string Message)
        {
            Log(Message, Severity.Information);
        }

        public static void Log(string Message, Severity sev)
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.CurrentDocument.Editor;
            ed.WriteMessage(Message);
        }

        public enum Severity
        {
            Debug,
            Information,
            Warning,
            Error,
            Crash
        }
    }
}
