using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Parse
{
    public sealed class ConcatParser : Parser
    {
        private readonly List<Parser> _inner = new List<Parser>();

        public override bool FlattenHierarchy => true;

        public IEnumerable<Parser> Inner => _inner;

        public ConcatParser( IEnumerable<Parser> inner )
        {
            AddRange( inner );
        }

        public ConcatParser( params Parser[] inner )
        {
            AddRange( inner );
        }

        public void Add( Parser inner )
        {
            _inner.Add( inner );
        }

        public void AddRange( IEnumerable<Parser> inner )
        {
            foreach ( var parser in inner )
            {
                Add( parser );
            }
        }

        protected override bool OnParse( ParseResult result )
        {
            if ( _inner.Count == 0 ) return false;
            
            foreach ( var parser in _inner )
            {
                ParseResult inner;
                if ( !result.Read( parser, out inner ) ) return result.Error( inner );
            }

            return true;
        }

        public override string ToString()
        {
            return string.Join( " ", Inner.Select( x => x.ToString() ) );
        }
    }
}
