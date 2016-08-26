using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class GrammarBuilderTest
    {
        private NamedParserCollection GetGrammar1()
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

        private NamedParserCollection GetGrammar2()
        {
            return GrammarBuilder.FromString( @"
                Whitespace = /\s+/;
                EndOfInput = /$/;

                Value = Integer | Float
                {
                    Integer = /0|-?[1-9][0-9]*/;
                    Float = /-?[0-9]+\.[0-9]+/;
                }

                ignore Whitespace
                {
                    Document = Value (',' Document | EndOfInput);
                }
            " );
        }

        [TestMethod]
        public void GrammarBuilder1()
        {
            TestHelper.Test( GetGrammar1()["Document"], "Hello world! How are you? Testing testing testing testing.", true );
        }

        [TestMethod]
        public void GrammarBuilder2()
        {
            TestHelper.Test( GetGrammar1()["Document"], "Hello world! How are you? Testing-testing testing testing.", false );
        }
        
        [TestMethod]
        public void GrammarBuilder3()
        {
            TestHelper.Test( GetGrammar1()["Word"], "Testing", true );
        }
        
        [TestMethod]
        public void GrammarBuilder4()
        {
            TestHelper.Test( GetGrammar1()["Word"], "?", false );
        }
        
        [TestMethod]
        public void GrammarBuilder5()
        {
            TestHelper.Test( GetGrammar1()["Period"], "?", true );
        }
        
        [TestMethod]
        public void GrammarBuilder6()
        {
            TestHelper.Test( GetGrammar1()["Period"], " ", false );
        }

        [TestMethod]
        public void GrammarBuilder7()
        {
            TestHelper.Test( GetGrammar2()["Document"], "5, 12.324, -3, -18.61, 0, 8.0", true );
        }
    }
}
