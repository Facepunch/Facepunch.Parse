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
                document.Resolve( word + word );
                word.Resolve( "A" );
            }

            return document;
        }

        [TestMethod]
        public void BasicWhitespace1()
        {
            Assert.IsTrue( CreateParser().Parse( "AA" ).Success );
        }

        [TestMethod]
        public void BasicWhitespace2()
        {
            Assert.IsTrue( CreateParser().Parse( "A A" ).Success );
        }

        [TestMethod]
        public void BasicWhitespace3()
        {
            Assert.IsTrue( CreateParser().Parse( " AA" ).Success );
        }

        [TestMethod]
        public void BasicWhitespace4()
        {
            Assert.IsTrue( CreateParser().Parse( "AA " ).Success );
        }

        [TestMethod]
        public void BasicWhitespace5()
        {
            Assert.IsTrue( CreateParser().Parse( "A  A" ).Success );
        }

        [TestMethod]
        public void BasicWhitespace6()
        {
            Assert.IsTrue( CreateParser().Parse( "   A    A          " ).Success );
        }

        [TestMethod]
        public void BasicWhitespace7()
        {
            Assert.IsFalse( CreateParser().Parse( "   A    B          " ).Success );
        }

        [TestMethod]
        public void BasicWhitespace8()
        {
            Assert.IsFalse( CreateParser().Parse( "   A  \t  A          " ).Success );
        }
    }
}
