using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class GrammarBuilderTest
    {
        [TestMethod]
        public void GrammarBuilder1()
        {
            var grammar = GrammarBuilder.FromString( @"
                Whitespace = /\s+/;
                Word = /[a-z]+/i;
                Period = ""."" | ""?"" | ""!"";

                ignore Whitespace
                {
                    Document = Sentence /$/;
                    Document = Sentence Document;

                    Sentence = Word Period;
                    Sentence = Word Sentence;
                }
            " );

            TestHelper.Test( grammar["Document"], "Hello world! How are you? Testing testing testing testing.", true );
            TestHelper.Test( grammar["Document"], "Hello world! How are you? Testing testing-testing testing.", false );
            TestHelper.Test( grammar["Word"], "Testing", true );
            TestHelper.Test( grammar["Word"], "?", false );
            TestHelper.Test( grammar["Period"], "?", true );
            TestHelper.Test( grammar["Period"], " ", false );
        }
    }
}
