using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Facepunch.Parse
{
    public abstract class GrammarException : Exception
    {
        public ParseResult Context { get; }

        protected GrammarException( ParseResult context, string message )
            : base( $"{message} at line {context.LineNumber}, column {context.ColumNumber}" )
        {
            Context = context;
        }
    }

    public class GrammarParseException : GrammarException
    {
        public GrammarParseException( ParseResult context )
            : base( context.Errors.First(), context.ErrorMessage ) { }
    }

    public sealed class NamedParserCollection
    {
        private readonly Dictionary<string, NamedParser> _namedParsers = new Dictionary<string, NamedParser>();

        public void Add( string name, Parser definition )
        {
            var parser = Get( name );
            if ( parser.IsResolved )
            {
                parser.Resolve( parser.ResolvedParser | definition );
            }
            else
            {
                parser.Resolve( definition );
            }
        }

        public NamedParser Get( string name )
        {
            NamedParser parser;
            if ( !_namedParsers.TryGetValue( name, out parser ) )
            {
                parser = new NamedParser( name );
                _namedParsers.Add( name, parser );
            }

            return parser;
        }

        public NamedParser this[ string name ] => _namedParsers[name];

        public override string ToString()
        {
            return string.Join( Environment.NewLine, _namedParsers.Select( x => $"{x.Key} = {x.Value.ResolvedParser};" ) );
        }
    }

    public static class GrammarBuilder
    {
        private static GrammarParser Parser { get; } = new GrammarParser();

        public static NamedParserCollection FromString( string str )
        {
            var result = Parser.Parse( str );
            if ( !result.Success ) throw new GrammarParseException( result );
            
            var rules = new NamedParserCollection();
            ReadStatementBlock( result[0], rules );

            return rules;
        }

        private static void ReadStatementBlock( ParseResult statementBlock, NamedParserCollection rules )
        {
            foreach ( var statement in statementBlock.Inner )
            {
                var value = statement[0];
                if ( value.Parser == Parser.Definition )
                {
                    ReadDefinition( value, rules );
                }
                else if ( value.Parser == Parser.IgnoreBlock )
                {
                    ReadIgnoreBlock( value, rules );
                }
            }
        }

        private static void ReadDefinition( ParseResult definition, NamedParserCollection rules )
        {
            var name = definition[0].Value;
            var parser = ReadBranch( definition[1], rules );

            rules.Add( name, parser );
        }

        private static Parser ReadBranch( ParseResult branch, NamedParserCollection rules )
        {
            return branch.InnerCount == 1
                ? ReadConcat( branch[0], rules )
                : new BranchParser( branch.Inner.Select( x => ReadConcat( x, rules ) ) );
        }

        private static Parser ReadConcat( ParseResult concat, NamedParserCollection rules )
        {
            return concat.InnerCount == 1
                ? ReadTerm( concat[0], rules )
                : new ConcatParser( concat.Inner.Select( x => ReadTerm( x, rules ) ) );
        }

        private static Parser ReadTerm( ParseResult term, NamedParserCollection rules )
        {
            var value = term[0];
            if ( value.Parser == Parser.String ) return ReadString( value, rules );
            if ( value.Parser == Parser.Regex ) return ReadRegex( value, rules );
            if ( value.Parser == Parser.NonTerminal ) return ReadNonTerminal( value, rules );
            if ( value.Parser == Parser.Branch ) return ReadBranch( value, rules );
            throw new NotImplementedException();
        }

        private static Parser ReadString( ParseResult str, NamedParserCollection rules )
        {
            var value = str[0];
            if ( value.Length == 0 ) return EmptyParser.Instance;

            var builder = new StringBuilder( value.Length );
            var escaped = false;
            for ( var i = 0; i < value.Length; ++i )
            {
                var c = value.Value[i];
                if ( escaped )
                {
                    escaped = false;
                    switch ( c )
                    {
                        case 'r': builder.Append( '\r' ); break;
                        case 'n': builder.Append( '\n' ); break;
                        case 't': builder.Append( '\t' ); break;
                        default: builder.Append( c ); break;
                    }
                }
                else if ( c == '\\' )
                {
                    escaped = true;
                }
                else
                {
                    builder.Append( c );
                }
            }

            return builder.ToString();
        }

        private static Parser ReadRegex( ParseResult regex, NamedParserCollection rules )
        {
            var pattern = regex[0];
            var options = regex[1];

            var builder = new StringBuilder( pattern.Length );
            var escaped = false;
            for ( var i = 0; i < pattern.Length; ++i )
            {
                var c = pattern.Value[i];
                if ( escaped )
                {
                    escaped = false;
                    switch ( c )
                    {
                        case '/': builder.Append( '/' ); break;
                        default: builder.Append( "\\" + c ); break;
                    }
                }
                else if ( c == '\\' )
                {
                    escaped = true;
                }
                else
                {
                    builder.Append( c );
                }
            }

            var parsedOptions = RegexOptions.None;
            for ( var i = 0; i < options.Length; ++i )
            {
                switch ( options.Value[i] )
                {
                    case 'i': parsedOptions |= RegexOptions.IgnoreCase; break;
                }
            }

            return new Regex( builder.ToString(), parsedOptions );
        }

        private static Parser ReadNonTerminal( ParseResult nonTerminal, NamedParserCollection rules )
        {
            var name = nonTerminal.Value;
            return rules.Get( name );
        }

        private static void ReadIgnoreBlock( ParseResult ignoreBlock, NamedParserCollection rules )
        {
            var ignore = ReadBranch( ignoreBlock[0], rules );

            using ( Parse.Parser.AllowWhitespace( ignore ) )
            {
                ReadStatementBlock( ignoreBlock[1], rules );
            }
        }
    }
}
