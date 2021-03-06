﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Facepunch.Parse
{
    public sealed class BranchParser : Parser
    {
        private readonly List<Parser> _inner = new List<Parser>();

        public override bool FlattenHierarchy => true;

        public IEnumerable<Parser> Inner => _inner;

        public BranchParser() { }

        public BranchParser( IEnumerable<Parser> inner )
        {
            _inner.AddRange( inner );
        }

        public BranchParser( params Parser[] inner )
        {
            _inner.AddRange( inner );
        }

        public void Add( Parser inner )
        {
            _inner.Add( inner );
        }

        public void AddRange( IEnumerable<Parser> inner )
        {
            _inner.AddRange( inner );
        }

        protected override bool OnParse( ParseResult result, bool errorPass )
        {
            if ( _inner.Count == 0 ) return false;
            if ( result.GetIsInfiniteRecursion() )
                return result.Error( ParseError.InvalidGrammar, "Grammar contains left recursion" );

            var results = ResultPool.CreateList();

            ParseResult bestResult = null;
            foreach ( var parser in _inner )
            {
                var inner = result.Peek( parser, errorPass );
                results.Add( inner );
                if ( inner.IsBetterThan( bestResult ) ) bestResult = inner;
            }

            Debug.Assert( bestResult != null, "bestResult != null" );
            if ( bestResult.Success )
            {
                result.Apply( bestResult, errorPass );
                ResultPool.ReleaseList( results );
                return true;
            }

            var max = results.Max( x => x.MaxErrorIndex );

            foreach ( var parseResult in results )
            {
                if ( parseResult.MaxErrorIndex == max )
                {
                    result.Error( parseResult, errorPass );
                }
            }

            ResultPool.ReleaseList( results );
            return false;
        }

        public override string ToString()
        {
            return string.Join( " | ", Inner.Select( x => x.ToString() ).ToArray() );
        }
    }
}
