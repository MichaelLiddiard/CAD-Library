using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace JPPCommands
{
    public class FFLCommand
    {
        [CommandMethod("FFL")]
        public void FFL()
        {
            // Get the current document
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Editor acEditor = acDoc.Editor;

            // Save the current text style
            string currTextStyle = System.Convert.ToString(Application.GetSystemVariable("TextStyle"));


            // Initialise
            if (!JPPCommandsInitialisation.JPPCommandsInitialise())
            {
                acEditor.WriteMessage("\nUnable to initialise the FFL command!");
                return;
            }

            // Will need to save other elements of current context, e.g. layer
            // to make sure current state is restored at when the command is complete

            // Show the FFL command on the command-line
            // Loop until valid command input or ESC pressed
            bool ValidCommand = false;
            // string SubCommand = "";
            while (ValidCommand == false)
            {
                // Display the command and get the FFL sub-command
                PromptResult SubCommand = acEditor.GetString("\n Add, Delete, Edit, Exposed Brickwork, eXit: ");
                // Check if the ESC key has pressed
                if (SubCommand.Status == PromptStatus.Cancel)
                {
                    acEditor.WriteMessage("ESC key pressed!\n");
                    ValidCommand = true;
                }
                else if (SubCommand.Status == PromptStatus.OK)
                {
                    // A string has been returned so convert to lower case and check 
                    // sub -command is valid. 
                    // Note that a valid sub-command can be a single letter abbreviation.....
                    //
                    // "A" or "a" = Add
                    // "D" or "d" = Delete
                    // "E" or "e" = Edit
                    // "B" or "b" = Exposed Brickwork/Tanking
                    // "X" or "x" = Exit
                    //
                    // 
                    // SubCommand = ToLower(PromptReturnString.ToString);   
                    switch (SubCommand.StringResult.ToLower())
                    {
                        case "add":
                        case "a":
                            if(!AddFFL.NewFFL())
                            {
                                acEditor.WriteMessage("\nUnable to add new outline - exiting FFL command!");
                                ValidCommand = true;
                            }
                            break;
                        case "delete":
                        case "d":
                            acEditor.WriteMessage("FFL Delete command!\n");
                            ValidCommand = true;
                            break;
                        case "edit":
                        case "e":
                            if(!EditFFL.EditFFLOrLevels())
                            {
                                acEditor.WriteMessage("\nUnable to edit FFL or Levels - exiting FFL command!");
                                ValidCommand = true;
                            }
                            break;
                        case "brickwork":
                        case "b":
                            if (!AddBrickwork.ExposedAndTanking())
                            {
                                acEditor.WriteMessage("\nUnable to add exposed brick and/or tanking!");
                                ValidCommand = true;
                            }
                            break;
                        case "exit":
                        case "x":
                            acEditor.WriteMessage("FFL Exit command!\n");
                            ValidCommand = true;
                            break;
                        default:
                            acEditor.WriteMessage("Invalid command specified!\n");
                            //validCommand = true;
                            break;
                    }
                }
            }
            // Restore current context
            Application.SetSystemVariable("TEXTSTYLE", currTextStyle);
            acEditor.WriteMessage("\nFFL command finished.");
        }
    }
}
