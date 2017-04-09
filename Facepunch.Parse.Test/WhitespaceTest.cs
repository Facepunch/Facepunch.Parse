using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class WhitespaceTest
    {
        private Parser CreateParser()
        {
            var document = new NamedParser( "Document" );
            var word = new NamedParser( "Word" );

            using ( Parser.AllowWhitespace( " " ) )
            {
                document.Define( word + word );
                word.Define( "A" );
            }

            return document;
        }

        [TestMethod]
        public void BasicWhitespace1()
        {
            TestHelper.Test( CreateParser(), "AA", true );
        }

        [TestMethod]
        public void BasicWhitespace2()
        {
            TestHelper.Test( CreateParser(), "A A", true );
        }

        [TestMethod]
        public void BasicWhitespace3()
        {
            TestHelper.Test( CreateParser(), " AA", true ); 
        }

        [TestMethod]
        public void BasicWhitespace4()
        {
            TestHelper.Test( CreateParser(), "AA ", true );
        }

        [TestMethod]
        public void BasicWhitespace5()
        {
            TestHelper.Test( CreateParser(), "A  A", true );
        }

        [TestMethod]
        public void BasicWhitespace6()
        {
            TestHelper.Test( CreateParser(), "   A    A          ", true );
        }

        [TestMethod]
        public void BasicWhitespace7()
        {
            TestHelper.Test( CreateParser(), "   A    B          ", false );
        }

        [TestMethod]
        public void BasicWhitespace8()
        {
            TestHelper.Test( CreateParser(), "   A  \t  A          ", false );
        }
    }
}
