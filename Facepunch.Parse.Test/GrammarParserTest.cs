using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class GrammarParserTest
    {
        [TestMethod]
        public void GrammarParserString1()
        {
            TestHelper.Test( new GrammarParser(), "Document = \"A\";", true );
        }

        [TestMethod]
        public void GrammarParserString2()
        {
            TestHelper.Test( new GrammarParser(), "Document = \"\\\"\";", true );
        }

        [TestMethod]
        public void GrammarParserString3()
        {
            TestHelper.Test( new GrammarParser(), "Document = \"\\\";", false );
        }

        [TestMethod]
        public void GrammarParserString4()
        {
            TestHelper.Test( new GrammarParser(), "Document = \"\\\\\";", true );
        }

        [TestMethod]
        public void GrammarParseParetheses1()
        {
            TestHelper.Test( new GrammarParser(), "Document = (A);", true );
        }

        [TestMethod]
        public void GrammarParseParetheses2()
        {
            TestHelper.Test( new GrammarParser(), "Document = (A | B);", true );
        }

        [TestMethod]
        public void GrammarParserComplex1()
        {
            TestHelper.Test( new GrammarParser(), @"
                Whitespace = /\s+/;
                Word = /[a-z]+/i;
                Period = ""."" | ""?"" | ""!"";
                EndOfInput = /$/;

                ignore Whitespace
                {
                    Document = Sentence (Document | EndOfInput);
                    Sentence = Word (Sentence | Period);
                }
            ", true );
        }
    }
}
