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

    public sealed class NamedParserCollection : INamedParserResolver
    {
        private readonly Dictionary<string, NamedParser> _namedParsers = new Dictionary<string, NamedParser>();

        private readonly Stack<string> _namespace = new Stack<string>();

        public void PushNamespace( string @namespace )
        {
            if ( _namespace.Count > 0 ) @namespace = $"{_namespace.Peek()}.{@namespace}";
            _namespace.Push( @namespace );
        }

        public void PopNamespace()
        {
            _namespace.Pop();
        }

        public void Add( string name, Parser definition )
        {
            if ( _namespace.Count > 0 ) name = $"{_namespace.Peek()}.{name}";

            NamedParser parser;
            if ( _namedParsers.TryGetValue( name, out parser ) )
            {
                parser.Resolve( parser.ResolvedParser | definition );
            }
            else
            {
                parser = new NamedParser( name );
                _namedParsers.Add( name, parser );
                parser.Resolve( definition );
            }
        }

        public NamedParser Get( string name )
        {
            return new NamedParser( name, _namespace.Count == 0 ? null : _namespace.Peek(), this );
        }

        public NamedParser this[ string name ] => _namedParsers[name];

        public override string ToString()
        {
            return string.Join( Environment.NewLine, _namedParsers.Select( x => $"{x.Key} = {x.Value.ResolvedParser};" ).ToArray() );
        }

        private Parser GetExisting( string fullName )
        {
            NamedParser parser;
            return _namedParsers.TryGetValue( fullName, out parser ) ? parser.ResolvedParser : null;
        }

        public Parser Resolve( NamedParser named )
        {
            if ( named.Namespace == null )
            {
                return GetExisting( named.Name );
            }

            var splitIndex = named.Namespace.Length;
            while (true)
            {
                var name = splitIndex <= 0 ? named.Name : $"{named.Namespace.Substring( 0, splitIndex )}.{named.Name}";
                var existing = GetExisting( name );
                if ( existing != null )
                {
                    named.ResolvedName = name;
                    return existing;
                }
                if ( splitIndex <= 0 ) break;

                splitIndex = named.Namespace.LastIndexOf( ".", splitIndex - 1 );
            }

            return null;
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
            foreach ( var statement in statementBlock )
            {
                var value = statement[0];
                if ( value.Parser == Parser.Definition )
                {
                    ReadDefinition( value, rules );
                }
                else if ( value.Parser == Parser.SpecialBlock )
                {
                    ReadSpecialBlock( value, rules );
                }
            }
        }

        private static void ReadDefinition( ParseResult definition, NamedParserCollection rules )
        {
            var name = definition[0].Value;

            rules.PushNamespace( name );

            if ( definition.InnerCount > 2 )
            {
                ReadStatementBlock( definition[2], rules );
            }

            var parser = ReadBranch( definition[1], rules );

            rules.PopNamespace();

            rules.Add( name, parser );
        }

        private static Parser ReadBranch( ParseResult branch, NamedParserCollection rules )
        {
            return branch.InnerCount == 1
                ? ReadConcat( branch[0], rules )
                : new BranchParser( branch.Select( x => ReadConcat( x, rules ) ) );
        }

        private static Parser ReadConcat( ParseResult concat, NamedParserCollection rules )
        {
            return concat.InnerCount == 1
                ? ReadModifier( concat[0], rules )
                : new ConcatParser( concat.Select( x => ReadModifier( x, rules ) ) );
        }

        private static Parser ReadModifier( ParseResult modifier, NamedParserCollection rules )
        {
            return modifier.InnerCount == 1
                ? ReadTerm( modifier[0], rules )
                : new StrictParser( ReadTerm( modifier[1], rules ) );
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

        private static void ReadSpecialBlock( ParseResult specialBlock, NamedParserCollection rules )
        {
            if ( specialBlock[0].InnerCount == 1 )
            {
                var ignore = ReadBranch( specialBlock[0][0], rules );

                using ( Parse.Parser.AllowWhitespace( ignore ) )
                {
                    ReadStatementBlock( specialBlock[1], rules );
                }
            }
            else switch ( specialBlock[0].Value )
            {
                case "noignore":
                    using ( Parse.Parser.ForbidWhitespace() )
                    {
                        ReadStatementBlock( specialBlock[1], rules );
                    }
                    break;
                case "collapse":
                    using ( Parse.Parser.EnableCollapseSingletons() )
                    {
                        ReadStatementBlock( specialBlock[1], rules );
                    }
                    break;
                default:
                    throw new NotImplementedException(specialBlock[0].Value);
            }
        }
    }
}
