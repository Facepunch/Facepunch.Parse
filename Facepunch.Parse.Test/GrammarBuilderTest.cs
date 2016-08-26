using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class GrammarBuilderTest
    {
        private NamedParserCollection GetGrammar()
        {
            return GrammarBuilder.FromString( @"
                Whitespace = /\s+/;
                Word = /[a-z]+/i;
                Period = '.' | '?' | '!';
                EndOfInput = /$/;

                ignore Whitespace
                {
                    Document = Sentence (Document | EndOfInput);
                    Sentence = Word (Sentence | Period);
                }
            " );
        }

        [TestMethod]
        public void GrammarBuilder1()
        {
            TestHelper.Test( GetGrammar()["Document"], "Hello world! How are you? Testing testing testing testing.", true );
        }

        [TestMethod]
        public void GrammarBuilder2()
        {
            TestHelper.Test( GetGrammar()["Document"], "Hello world! How are you? Testing-testing testing testing.", false );
        }
        
        [TestMethod]
        public void GrammarBuilder3()
        {
            TestHelper.Test( GetGrammar()["Word"], "Testing", true );
        }
        
        [TestMethod]
        public void GrammarBuilder4()
        {
            TestHelper.Test( GetGrammar()["Word"], "?", false );
        }
        
        [TestMethod]
        public void GrammarBuilder5()
        {
            TestHelper.Test( GetGrammar()["Period"], "?", true );
        }
        
        [TestMethod]
        public void GrammarBuilder6()
        {
            TestHelper.Test( GetGrammar()["Period"], " ", false );
        }
    }
}
