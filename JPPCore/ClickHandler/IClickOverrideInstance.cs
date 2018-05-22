using Autodesk.AutoCAD.DatabaseServices;

namespace JPP.Core
{
    public interface IClickOverrideInstance
    {
        bool CanHandle(DBObject obj);

        string CommandName();
    }
}
