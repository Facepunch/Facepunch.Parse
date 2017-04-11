using System;
using System.Collections;
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

    public sealed class ParseResult : IEnumerable<ParseResult>
    {
        private readonly string _source;
        private readonly List<ParseResult> _inner = new List<ParseResult>();

        private int _lineNumber;
        private int _columnNumber;

        private int _lineIndex;
        private int _lineLength;

        private string _errorMessage;
        private string _innerMessage;
        private List<ParseResult> _errorResults;

        public Parser Parser { get; }

        public ParseResult Parent { get; private set; }

        public int Index { get; }
        public int TrimmedIndex { get; private set; }

        public int Length { get; private set; }
        public int TrimmedLength { get; private set; }

        public int LineNumber
        {
            get
            {
                if ( _lineNumber == 0 ) GetLineCol( Index );
                return _lineNumber;
            }
        }

        public int ColumNumber
        {
            get
            {
                if ( _columnNumber == 0 ) GetLineCol( Index );
                return _columnNumber;
            }
        }

        private int ReadPos => Index + Length;

        public bool Success { get; private set; }

        public string Value => _source.Substring( TrimmedIndex, TrimmedLength );
        public string SourceLine => _source.Substring( _lineIndex, _lineLength );
        public string Source => _source;

        public string ErrorMessage => GetErrorMessage();
        public ParseError ErrorType { get; private set; }

        public string Expected
        {
            get
            {
                if ( ErrorType != ParseError.ExpectedToken ) throw new InvalidOperationException();
                if ( Parser is NamedParser && InnerCount == 0 )
                {
                    return ((NamedParser) Parser).Name;
                }

                return _errorMessage;
            }
        }

        public IEnumerable<ParseResult> Errors => _errorResults ?? (_errorResults = GetErrors());

        public int InnerCount => _inner.Count;

        public ParseResult this[ int index ] => _inner[index];
        public ParseResult this[ string elementName ] => _inner.FirstOrDefault( x => x.Parser.ElementName == elementName );

        public int MaxErrorIndex => ErrorType == ParseError.SubParser && _inner.Count > 0
            ? _inner.Max( x => x.MaxErrorIndex )
            : ErrorType == ParseError.None ? -1 : Index;

        private void GetLineCol( int index )
        {
            _lineNumber = 1;
            _columnNumber = 1;

            for ( var i = 0; i < index; ++i )
            {
                switch ( _source[i] )
                {
                    case '\r': _columnNumber = 1; break;
                    case '\n': ++_lineNumber; _columnNumber = 1; _lineIndex = i + 1; break;
                    default: ++_columnNumber; break;
                }
            }

            _lineLength = _source.Length - _lineIndex;

            for ( var i = index; i < _source.Length; ++i )
            {
                switch ( _source[i] )
                {
                    case '\r':
                    case '\n':
                        _lineLength = i - _lineIndex;
                        return;
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
            Parent = parent;
            TrimmedIndex = Index = parent.Index + parent.Length;
        }

        private bool IsIdentical( ParseResult other )
        {
            return other.Parser == Parser && other.Index == Index && other.Length == Length;
        }

        public bool GetIsInfiniteRecursion()
        {
            var parent = this;
            while ( (parent = parent.Parent) != null )
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
            if ( dst == null ) dst = new List<ParseResult>();

            if ( _errorResults != null )
            {
                if ( _errorResults.Count > 0 )
                {
                    dst.RemoveAll( x => x.Index < _errorResults[0].Index );
                    dst.AddRange( _errorResults );
                }
                return dst;
            }

            if ( ErrorType == ParseError.None ) return dst;

            if ( ErrorType != ParseError.SubParser )
            {
                if ( dst.Count == 0 ) dst.Add( this );
                else if ( dst[0].Index <= Index )
                {
                    dst.Add( this );
                    dst.RemoveAll( x => x.Index < Index );
                }
                return dst;
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

            var nonExpect = errors.FirstOrDefault(x => x.ErrorType != ParseError.ExpectedToken);
            if ( nonExpect != null ) return _innerMessage = nonExpect.ErrorMessage;

            var builder = new StringBuilder();

            var distinct = errors.Select(x => x.Expected).Distinct().ToArray();

            for ( var i = 0; i < distinct.Length; ++i )
            {
                if ( i > 0 )
                {
                    builder.Append( ", " );
                    if ( i == distinct.Length - 1 ) builder.Append( "or " );
                }

                builder.Append( distinct[i] );
            }

            return _innerMessage = $"Expected {builder}";
        }

        private bool ShouldFlattenInner( ParseResult result )
        {
            if ( result.Parser.FlattenHierarchy ) return true;
            if ( result.Parser.CollapseIfSingleElement )
            {
                if ( result.InnerCount == 0 && result.Length == 0 ) return true;
                if ( result.InnerCount == 1 && result[0].Parser is NamedParser ) return true;
            }
            return false;
        }

        private void AddInner( ParseResult result )
        {
            var len = result.ReadPos - Index;
            if ( Length < len )
            {
                Length = len;
                TrimmedLength = result.Index + result.TrimmedLength - TrimmedIndex;
            }

            Success &= result.Success;

            if ( !ShouldFlattenInner( result ) )
            {
                if ( !result.Parser.OmitFromResult || !result.Success )
                {
                    _inner.Add( result );
                    result.Parent = this;
                }

                return;
            }

            foreach ( var inner in result )
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
            if ( ErrorType == ParseError.None )
            {
                ErrorType = ParseError.SubParser;
            }
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
            TrimmedLength += token.Length;
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
            TrimmedLength += match.Length;
            return true;
        }

        public bool Read( Parser parser )
        {
            var result = Peek(parser);
            if ( result.Success ) Apply( result );
            else Error( result );
            return result.Success;
        }

        public void Skip( ParseResult inner )
        {
            if ( inner.Parent != this ) throw new ArgumentException();
            if ( inner.Index != ReadPos ) throw new ArgumentException();

            Length = inner.ReadPos - Index;
            if ( TrimmedLength == 0 ) TrimmedIndex = inner.ReadPos;
        }

        public void Apply( ParseResult inner )
        {
            if ( inner.Parent != this ) throw new ArgumentException();
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

        public IEnumerator<ParseResult> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        public override string ToString()
        {
            return Success ? _source.Substring( TrimmedIndex, TrimmedLength ) : ErrorMessage;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
