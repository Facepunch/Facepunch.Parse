using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Facepunch.Parse.Test.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Facepunch.Parse.Test
{
    [TestClass]
    public class CodeGenTest
    {
        [TestMethod]
        public void ExpressionGen1()
        {
            var grammar = GrammarBuilder.FromString(Resources.ExpressionGrammar);
            using ( var writer = new StringWriter() )
            {
                var gen = new GrammarCodeGenerator();
                gen.WriteGrammar( "Test.ExpressionParser", grammar, "Expression", writer );
                Console.WriteLine( writer );
            }
        }
    }
}
