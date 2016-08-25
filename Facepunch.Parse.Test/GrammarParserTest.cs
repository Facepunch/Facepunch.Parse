using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class GrammarParserTest
    {
        [TestMethod]
        public void BasicGrammarParser()
        {
            var parser = new GrammarParser();
            var result = parser.Parse( @"
                Whitespace = /\s+/;
                Word = /[a-z]+/i;
                Period = ""."" | ""?"" | ""!"";

                ignore Whitespace
                {
                    Document = Sentence;
                    Document = Sentence | Document;

                    Sentence = Word Period;
                    Sentence = Word Sentence;
                }
                " );

            Debug.WriteLine( result.ToXElement() );

            Assert.IsTrue( result.Success );
        }
    }
}
