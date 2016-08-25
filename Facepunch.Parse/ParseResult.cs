using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Facepunch.Parse
{
    public enum ParseError
    {
        None,
        SubParser,
        ExpectedToken,
        InvalidGrammar,
        NullParser
    }

    public sealed class ParseResult
    {
        private readonly string _source;
        private readonly List<ParseResult> _inner = new List<ParseResult>();

        private int _lineNumber;
        private int _columnNumber;

        private ParseResult _parent;
        private string _errorMessage;
        private string _innerMessage;
        private List<ParseResult> _errorResults;

        public Parser Parser { get; }

        public int Index { get; }
        public int Length { get; private set; }

        public int LineNumber
        {
            get
            {
                if ( _lineNumber == 0 ) GetLineCol( Index, out _lineNumber, out _columnNumber );
                return _lineNumber;
            }
        }

        public int ColumNumber
        {
            get
            {
                if ( _columnNumber == 0 ) GetLineCol( Index, out _lineNumber, out _columnNumber );
                return _columnNumber;
            }
        }

        private int ReadPos => Index + Length;

        public bool Success { get; private set; }

        public string Value => _source.Substring( Index, Length );

        public string ErrorMessage => GetErrorMessage();
        public ParseError ErrorType { get; private set; }

        public IEnumerable<ParseResult> Errors => _errorResults ?? (_errorResults = GetErrors());

        public int InnerCount => _inner.Count;
        public IEnumerable<ParseResult> Inner => _inner;

        public ParseResult this[ int index ] => _inner[index];

        private void GetLineCol( int index, out int line, out int col )
        {
            line = 1;
            col = 1;

            for ( var i = 0; i < index; ++i )
            {
                switch ( _source[i] )
                {
                    case '\r': col = 1; break;
                    case '\n': ++line; col = 1; break;
                    default: ++col; break;
                }
            }
        }

        internal ParseResult( string source, Parser parser )
        {
            _source = source;
            Parser = parser;
            Success = true;
        }

        internal ParseResult( ParseResult parent, Parser parser )
            : this( parent._source, parser )
        {
            _parent = parent;
            Index = parent.Index + parent.Length;
        }

        private bool IsIdentical( ParseResult other )
        {
            return other.Parser == Parser && other.Index == Index && other.Length == Length;
        }

        public bool GetIsInfiniteRecursion()
        {
            var parent = this;
            while ( (parent = parent._parent) != null )
            {
                if ( IsIdentical( parent ) )
                {
                    return true;
                }
            }

            return false;
        }

        private List<ParseResult> GetErrors( List<ParseResult> dst = null )
        {
            if ( _errorResults != null )
            {
                if ( dst != null ) _errorResults.AddRange( _errorResults );

                return _errorResults;
            }

            if ( dst == null ) dst = new List<ParseResult>();

            if ( ErrorType == ParseError.None ) return dst;

            if ( ErrorType != ParseError.SubParser )
            {
                dst.Add( this );
            }

            foreach ( var inner in _inner )
            {
                dst = inner.GetErrors( dst );
            }

            return dst;
        }

        private string GetErrorMessage()
        {
            if ( _innerMessage != null ) return _innerMessage;

            var errors = GetErrors();
            if ( errors.Count == 0 )
            {
                return _innerMessage = string.Empty;
            }

            var max = errors.Max(x => x.Index);
            errors.RemoveAll( x => x.Index != max );

            var nonExpect = errors.FirstOrDefault(x => x.ErrorType != ParseError.ExpectedToken);
            if ( nonExpect != null ) return _innerMessage = nonExpect.ErrorMessage;

            var builder = new StringBuilder();

            var distinct = errors.Select(x => x._errorMessage).Distinct().ToArray();
            for ( var i = 0; i < distinct.Length; ++i )
            {
                if ( i > 0 )
                {
                    builder.Append( ", " );
                    if ( i == distinct.Length - 1 ) builder.Append( "or " );
                }

                builder.Append( distinct[i] );
            }

            int line, col;
            GetLineCol( errors[0].Index, out line, out col );

            return _innerMessage = $"Expected {builder}";
        }

        private bool ShouldFlattenInner( ParseResult result )
        {
            if ( result.Parser.FlattenHierarchy ) return true;
            if ( result.Parser == Parser ) return true;
            return false;
        }

        private void AddInner( ParseResult result )
        {
            if ( !ShouldFlattenInner( result ) )
            {
                if ( !result.Parser.OmitFromResult || !result.Success )
                {
                    _inner.Add( result );
                    result._parent = this;
                }

                Length = result.ReadPos - Index;
                Success &= result.Success;
                return;
            }

            foreach ( var inner in result.Inner )
            {
                AddInner( inner );
            }
        }

        public bool Error( ParseError type, string message )
        {
            Success = false;
            ErrorType = type;
            _errorMessage = message;
            return false;
        }

        public bool Error( ParseResult inner )
        {
            Success = false;
            ErrorType = ParseError.SubParser;
            AddInner( inner );
            return false;
        }

        public bool Read( string token )
        {
            var index = ReadPos;
            for ( var i = 0; i < token.Length; ++i, ++index )
            {
                if ( index >= _source.Length || _source[index] != token[i] )
                {
                    return false;
                }
            }

            Length += token.Length;
            return true;
        }

        public bool Read( Regex regex )
        {
            Match match;
            return Read( regex, out match );
        }

        public bool Read( Regex regex, out Match match )
        {
            match = regex.Match( _source, ReadPos );
            if ( !match.Success || match.Index != ReadPos ) return false;
            Length += match.Length;
            return true;
        }

        public bool Read( Parser parser )
        {
            var result = Peek(parser);
            if ( result.Success ) Apply( result );
            else Error( result );
            return result.Success;
        }

        public bool Read( Parser parser, out ParseResult result )
        {
            result = Peek( parser );
            if ( result.Success ) Apply( result );
            else Error( result );
            return result.Success;
        }

        public void Skip( ParseResult inner )
        {
            if ( inner._parent != this ) throw new ArgumentException();
            if ( inner.Index != ReadPos ) throw new ArgumentException();

            Length = inner.ReadPos - Index;
        }

        public void Apply( ParseResult inner )
        {
            if ( inner._parent != this ) throw new ArgumentException();
            if ( inner.Index != ReadPos ) throw new ArgumentException();

            AddInner( inner );
        }

        public ParseResult Peek( Parser parser )
        {
            var inner = new ParseResult(this, parser);
            parser.Parse( inner );
            return inner;
        }

        public bool IsBetterThan( ParseResult other )
        {
            return other == null || ErrorType != ParseError.NullParser && (ReadPos > other.ReadPos || ReadPos == other.ReadPos && Success && !other.Success) || other.ErrorType == ParseError.NullParser;
        }

        public XElement ToXElement()
        {
            return Parser.ToXElement( this );
        }

        public override string ToString()
        {
            return Success ? _source.Substring( Index, Length ) : ErrorMessage;
        }
    }
}
