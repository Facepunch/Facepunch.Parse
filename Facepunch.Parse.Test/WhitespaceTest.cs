using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class WhitespaceTest
    {
        [TestMethod]
        public void BasicWhitespace()
        {
            var document = new NamedParser( "Document" );
            var sentence = new NamedParser( "Sentence" );
            var period = new NamedParser( "Period" );
            var word = new NamedParser( "Word" );

            using ( ConcatParser.AllowWhitespace( " " ) )
            {
                document.Resolve( sentence | (sentence + document) );
                sentence.Resolve( (word + period) | (word + sentence) );
                period.Resolve( (Parser) "." | "?" | "!" );
                word.Resolve( new Regex( "[a-z]+", RegexOptions.IgnoreCase ) );
            }

            Assert.IsTrue( document.Parse( "Hello world! How are you doing today? This is another sentence. Testing." ).Success );
            Assert.IsFalse( document.Parse( "Hello world, How are you doing today? This is another sentence. Testing." ).Success );
            Assert.IsFalse( document.Parse( "Hello world! How are you doing today? This is another sentence. Testing" ).Success );
        }
    }
}
