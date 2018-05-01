using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace JPP.Civils.Test
{
    [TestFixture]
    class MainTest
    {
        [TestCase()]
        [Category("Integration")]
        public void Inititalize()
        {
            Main testSubject = new Main();
            testSubject.Initialize();
            Assert.Pass();
        }
    }
}
