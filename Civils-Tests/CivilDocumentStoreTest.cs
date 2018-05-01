using NUnit.Framework;
using NUnit.Framework.Internal;

namespace JPP.Civils.Test
{
    [TestFixture()]
    public class CivilDocumentStoreTest : CivilDocumentStore
    {
        [TestCase()]
        public void LoadTest()
        {
            CivilDocumentStoreTest cdst = new CivilDocumentStoreTest();
            cdst.Load();
        }
    }
}