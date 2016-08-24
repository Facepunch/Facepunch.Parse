using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Parse
{
    public sealed class ConcatParser : Parser
    {
        [ThreadStatic]
        private static Stack<Parser> _sWhitespaceParserStack;
        private static Stack<Parser> WhitespaceParserStack
        {
            get { return _sWhitespaceParserStack ?? (_sWhitespaceParserStack = new Stack<Parser>()); }
        }

        private static readonly WhitespaceDisposable _sWhitespaceDisposable = new WhitespaceDisposable();
        private static Parser CurrentWhitespaceParser => WhitespaceParserStack.Count == 0 ? null : WhitespaceParserStack.Peek();

        private class WhitespaceDisposable : IDisposable
        {
            public void Dispose()
            {
                WhitespaceParserStack.Pop();
            }
        }

        public static IDisposable ForbidWhitespace()
        {
            WhitespaceParserStack.Push( null );
            return _sWhitespaceDisposable;
        }

        public static IDisposable AllowWhitespace( Parser whitespaceParser )
        {
            if ( CurrentWhitespaceParser == null )
            {
                WhitespaceParserStack.Push( whitespaceParser );
            }
            else
            {
                WhitespaceParserStack.Push( CurrentWhitespaceParser | whitespaceParser );
            }

            return _sWhitespaceDisposable;
        }

        private readonly List<Parser> _inner = new List<Parser>();
        private readonly Parser _whitespaceParser = CurrentWhitespaceParser;

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

        public override bool Parse( ParseResult result )
        {
            if ( _inner.Count == 0 ) return false;

            var first = result.Index != 0;
            foreach ( var parser in _inner )
            {
                if ( _whitespaceParser != null && !first )
                {
                    ParseResult whitespace;
                    while ( (whitespace = result.Peek( _whitespaceParser )).Success )
                    {
                        result.Skip( whitespace );
                    }
                }

                first = false;

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
