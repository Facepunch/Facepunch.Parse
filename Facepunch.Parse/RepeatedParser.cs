using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Facepunch.Parse
{
    public sealed class RepeatedParser : Parser
    {
        private readonly Parser _inner;

        public override bool FlattenHierarchy => true;

        public Parser Inner => _inner;

        public RepeatedParser( Parser inner )
        {
            _inner = inner;
        }

        protected override bool OnParse( ParseResult result )
        {
            if ( !result.Read( _inner ) ) return false;

            ParseResult peek;
            while ( (peek = result.Peek( _inner )).Success )
            {
                result.Apply( peek );
            }

            return true;
        }

        public override string ToString()
        {
            return $"{_inner}+";
        }
    }
}
