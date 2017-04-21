using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Facepunch.Parse
{
    public abstract class Parser : IEquatable<Parser>
    {
        [ThreadStatic]
        private static ParseResultPool _sResultPool;
        internal static ParseResultPool ResultPool => _sResultPool ?? (_sResultPool = new ParseResultPool());

        [ThreadStatic]
        private static Stack<Parser> _sWhitespaceParserStack;
        private static Stack<Parser> WhitespaceParserStack => _sWhitespaceParserStack ?? (_sWhitespaceParserStack = new Stack<Parser>());

        [ThreadStatic]
        private static Stack<bool> _sCollapseStateStack;
        private static Stack<bool> CollapseStateStack => _sCollapseStateStack ?? (_sCollapseStateStack = new Stack<bool>());

        private static readonly WhitespaceDisposable _sWhitespaceDisposable = new WhitespaceDisposable();
        private static Parser CurrentWhitespaceParser => WhitespaceParserStack.Count == 0 ? null : WhitespaceParserStack.Peek();
        
        private static readonly CollapseStateDisposable _sCollapseStateDisposable = new CollapseStateDisposable();
        protected static bool CurrentCollapseState => CollapseStateStack.Count != 0 && CollapseStateStack.Peek();

        public static Parser EndOfInput { get; } = new RegexParser( new Regex( "$" ) );

        private class WhitespaceDisposable : IDisposable
        {
            public void Dispose()
            {
                WhitespaceParserStack.Pop();
            }
        }
        
        private class CollapseStateDisposable : IDisposable
        {
            public void Dispose()
            {
                CollapseStateStack.Pop();
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

        public static IDisposable EnableCollapseIfSingleElement()
        {
            CollapseStateStack.Push( true );
            return _sCollapseStateDisposable;
        }

        public static IDisposable DisableCollapseIfSingleElement()
        {
            CollapseStateStack.Push( false );
            return _sCollapseStateDisposable;
        }

        public static implicit operator Parser( string token )
        {
            if ( token.Length == 0 ) return EmptyParser.Instance;
            return new TokenParser( token );
        }

        public static implicit operator Parser( Regex regex )
        {
            return new RegexParser( regex );
        }

        public static ConcatParser operator +( Parser a, Parser b )
        {
            var aConcat = a as ConcatParser;
            var bConcat = b as ConcatParser;

            var result = new ConcatParser();

            if ( aConcat != null ) result.AddRange( aConcat.Inner ); else result.Add( a );
            if ( bConcat != null ) result.AddRange( bConcat.Inner ); else result.Add( b );

            return result;
        }

        public static BranchParser operator |( Parser a, Parser b )
        {
            var aBranch = a as BranchParser;
            var bBranch = b as BranchParser;

            var result = new BranchParser();

            if ( aBranch != null ) result.AddRange( aBranch.Inner ); else result.Add( a );
            if ( bBranch != null ) result.AddRange( bBranch.Inner ); else result.Add( b );

            return result;
        }

        public ParseResult Parse( string source )
        {
            var result = new ParseResult( ResultPool );
            result.Init( source, this );

            if ( !Parse( result, false ) || !result.Success )
            {
                result.Dispose();
                result = new ParseResult( ResultPool );
                result.Init( source, this );
                Parse( result, true );
            }

            if ( CollapseIfSingleElement && result.InnerCount == 1 )
            {
                return result[0];
            }

            return result;
        }

        private readonly Parser _whitespaceParser = CurrentWhitespaceParser;

        public virtual bool CollapseIfSingleElement { get; } = false;
        public virtual bool FlattenHierarchy { get; } = false;
        public virtual bool OmitFromResult { get; } = false;

        protected abstract bool OnParse( ParseResult result, bool errorPass );

        private void SkipWhitespace(ParseResult result)
        {
            if ( _whitespaceParser == null ) return;

            ParseResult whitespace;
            while ( (whitespace = result.Peek( _whitespaceParser, false )).Success && whitespace.Length > 0 )
            {
                result.Skip( whitespace );
            }

            whitespace.Dispose();
        }

        public bool Parse( ParseResult result, bool errorPass )
        {
            SkipWhitespace( result );
            if ( !OnParse( result, errorPass ) ) return false;
            SkipWhitespace( result );
            return true;
        }

        private string _elementName;
        public virtual string ElementName
        {
            get
            {
                if ( _elementName != null ) return _elementName;

                _elementName = GetType().Name;
                if ( _elementName.EndsWith( "Parser" ) )
                {
                    _elementName = _elementName.Substring( 0, _elementName.Length - "Parser".Length );
                }

                return _elementName;
            }
        }

        public virtual XElement ToXElement( ParseResult result )
        {
            var elem = new XElement(ElementName);

            elem.SetAttributeValue( "index", result.TrimmedIndex );
            elem.SetAttributeValue( "length", result.TrimmedLength );

            if ( result.InnerCount == 0 )
            {
                if ( result.ErrorType > ParseError.SubParser )
                {
                    elem.Add( new XElement( "ParseError", result.ErrorMessage ) );
                }
                else
                {
                    elem.Value = result.ToString();
                }
            }
            else
            {
                foreach ( var inner in result )
                {
                    elem.Add( inner.ToXElement() );
                }
            }

            return elem;
        }

        public Parser Repeated
        {
            get { return new RepeatedParser( this ); }
        }

        public Parser Optional
        {
            get { return this | ""; }
        }

        public override int GetHashCode()
        {
            return ElementName.GetHashCode();
        }

        public override bool Equals( object obj )
        {
            return Equals( obj as Parser );
        }

        public virtual bool Equals( Parser other )
        {
            return ReferenceEquals( this, other );
        }
    }
}
