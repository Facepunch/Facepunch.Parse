using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    public static class TestHelper
    {
        public static ParseResult Test( Parser parser, string input, bool shouldSucceed )
        {
            var result = parser.Parse( input );

            Console.WriteLine( $"# Input:\r\n{input}" );
            Console.WriteLine( $"# Output:\r\n{result.ToXElement()}" );
            Console.WriteLine( $"# Success: {result.Success}" );

            if ( !result.Success )
            {
                var error = result.Errors.First();

                Console.WriteLine( $"Error: {result.ErrorMessage} at line {error.LineNumber}, column {error.ColumNumber}" );
            }

            Assert.AreEqual( shouldSucceed, result.Success );

            return result;
        }
    }
}
