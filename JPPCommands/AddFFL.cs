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
    public static partial class AddFFL
    {        
        public static bool NewFFL()
        {
            // Add comment here to explain what the following code does
            ObjectId outlineId = CreateOutline();
            if (outlineId == ObjectId.Null)
                return false;
            if (!FormatOutline(outlineId))
            {
                // Remove the outline from the drawing
                JPPUtils.EraseEntity(outlineId);
                return false;
            }
               
            if (!AddFFLData(outlineId))
            {
                // Remove the outline from the drawing
                JPPUtils.EraseEntity(outlineId);
                return false;
            }
            if (!AddLevels(outlineId))
            {
                // Remove the outline from the drawing
                JPPUtils.EraseEntity(outlineId);
                return false;
            }
            // Adding the text will add other entities to the polyline so need to create an ObjectId collection
            // to store the object ids of each entity added to enable them to be grouped together.
            ObjectIdCollection FFLObjectIdCollection = JPPUtils.createObjectIdCollection(outlineId);
            if (FFLObjectIdCollection == null)
            {
                // Remove the outline from the drawing
                JPPUtils.EraseEntity(outlineId);
                return false;
            }

            if (!AddText(outlineId, FFLObjectIdCollection))
            {
                // If the text is not added sucessfully need to delete the objects in the object id collection.
                JPPUtils.EraseObjectsCollection(FFLObjectIdCollection);
                JPPUtils.EraseEntity(outlineId);
                return false;
            }
            if (!JPPUtils.CreateNewBlock(FFLObjectIdCollection))
            {
                // If the block is not created need to delete the objects in the object id collection,
                // and then delete the objects in the object id collection
                JPPUtils.EraseObjectsCollection(FFLObjectIdCollection);
                JPPUtils.EraseEntity(outlineId);
                return false;
            } 
            /* if (!JPPUtils.CreateNewGroup(FFLObjectIdCollection))
            {
                // If the group is not created need to delete the objects in the object id collection,
                // and then delete the objects in the object id collection
                return false;
            } */              
            return true;
        }
    }
}
