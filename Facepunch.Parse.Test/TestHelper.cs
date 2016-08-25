using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    public static class TestHelper
    {
        public static void Test( Parser parser, string input, bool shouldSucceed )
        {
            var result = parser.Parse( input );

            Debug.WriteLine( $"# Input:\r\n{input}" );
            Debug.WriteLine( $"# Output:\r\n{result.ToXElement()}" );
            Debug.WriteLine( $"# Success: {result.Success}" );

            Assert.AreEqual( shouldSucceed, result.Success );
        }
    }
}
