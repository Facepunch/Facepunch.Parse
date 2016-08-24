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

                #pushignore Whitespace

                Document = Sentence;
                Document = Sentence | Document;

                Sentence = Word Period;
                Sentence = Word Sentence;

                Period = ""."" | ""?"" | ""!"";

                #popignore

                Word = /[a-z]+/i;" );

            Debug.WriteLine( result.ToXElement() );

            Assert.IsTrue( result.Success );
        }
    }
}
