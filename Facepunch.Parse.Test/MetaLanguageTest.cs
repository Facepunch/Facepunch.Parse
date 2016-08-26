using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class MetaLanguageTest
    {
        [TestMethod]
        public void MetaLanguage1()
        {
            var grammar = GrammarBuilder.FromString( Properties.Resources.ExampleLanguage );
            TestHelper.Test( grammar["Language"], Properties.Resources.ExampleInput, true );
        }
    }
}
