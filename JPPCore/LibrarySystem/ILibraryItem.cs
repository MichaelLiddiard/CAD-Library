using Autodesk.AutoCAD.DatabaseServices;

namespace JPP.Core
{
    public interface ILibraryItem
    {
        void Transfer(Database to, Database from);

        /// <summary>
        /// Used to load an insstance into memory
        /// </summary>
        /// <param name="Name">Instance to be loaded</param>
        /// <param name="from">Source database</param>
        ILibraryItem GetFrom(string Name, Database from);
    }
}
