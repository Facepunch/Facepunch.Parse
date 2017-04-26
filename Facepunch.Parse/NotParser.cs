using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Facepunch.Parse
{
    public class NotParser : Parser
    {
        private readonly Parser _inner;

        public Parser Inner => _inner;

        public override bool OmitFromResult => true;

        public NotParser( Parser inner )
        {
            _inner = inner;
        }

        protected override bool OnParse( ParseResult result, bool errorPass )
        {
            using ( var peeked = result.Peek( _inner, errorPass ) )
            {
                return !peeked.Success;
            }
        }
    }
}
