using Autodesk.AutoCAD.DatabaseServices;

namespace JPP.Core
{
    public interface ILibraryItem
    {
        void LoadFrom(string Name, Database from);
        void SaveTo(string Name, Database to);
    }
}
