using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class CollapseTest
    {
        private Parser CreateParser()
        {
            var root = new NamedParser( "Root" );
            var a = new NamedParser( "A" );
            var b = new NamedParser( "B" );
            var c = new NamedParser( "C" );

            using ( Parser.AllowWhitespace( " " ) )
            {
                root.Resolve( a );
                using ( Parser.EnableCollapseSingletons() )
                {
                    a.Resolve( b + (("?" + a + ":" + a) | "") );
                    b.Resolve( c + (("+" + b) | "") );
                }
            }

            c.Resolve( new Regex( @"[0-9]+" ) );

            return root;
        }

        [TestMethod]
        public void Collapse1()
        {
            TestHelper.Test( CreateParser(), "0", true );
        }

        [TestMethod]
        public void Collapse2()
        {
            TestHelper.Test( CreateParser(), "0 + 1", true );
        }

        [TestMethod]
        public void Collapse3()
        {
            TestHelper.Test( CreateParser(), "0 + 1 + 2", true );
        }

        [TestMethod]
        public void Collapse4()
        {
            TestHelper.Test( CreateParser(), "0 ++ 1", false );
        }

        [TestMethod]
        public void Collapse5()
        {
            TestHelper.Test( CreateParser(), "0 ? 1 + 3 : 2", true );
        }

        [TestMethod]
        public void Collapse6()
        {
            TestHelper.Test( CreateParser(), "0 ? 1 ? 3 : 4 : 2", true );
        }
    }
}
