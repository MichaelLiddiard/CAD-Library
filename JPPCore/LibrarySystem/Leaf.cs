using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;

namespace JPP.Core
{
    public class Leaf
    {
        public string Path { get; set; }
        public string Name { get; set; }

        public Leaf(string path)
        {
            Path = path;
            Name = path.Split('\\').Last().Replace(".dwg", "");
        }

        public Database GetDatabase()
        {
            Database d = new Database(false, true);
            d.ReadDwgFile(Path, FileOpenMode.OpenForReadAndReadShare, false, null);
            return d;
        }                
    }
}
