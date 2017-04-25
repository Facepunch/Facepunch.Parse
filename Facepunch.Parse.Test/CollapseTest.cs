using System.Text.RegularExpressions;
using Facepunch.Parse.Test.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class CollapseTest
    {
        private NamedParserCollection CreateGrammar()
        {
            return GrammarBuilder.FromString( Resources.ExpressionGrammar );
        }

        [TestMethod]
        public void CollapseBuilder1()
        {
            Assert.IsTrue( CreateGrammar()["Expression"].CollapseIfSingleElement );
        }

        [TestMethod]
        public void CollapseBuilder2()
        {
            Assert.IsTrue( CreateGrammar()["Expression.Conditional"].CollapseIfSingleElement );
        }

        [TestMethod]
        public void CollapseBuilder3()
        {
            Assert.IsTrue( CreateGrammar()["Expression.ConditionalOr"].CollapseIfSingleElement );
        }

        [TestMethod]
        public void CollapseParse1()
        {
            var parser = CreateGrammar()["Expression"];
            Assert.IsTrue( parser.ResolvedParser.CollapseIfSingleElement );
        }

        [TestMethod]
        public void CollapseParse2()
        {
            var parser = CreateGrammar()["Expression"];
            TestHelper.Test( parser, "a?b:c", true );
        }
    }
}
