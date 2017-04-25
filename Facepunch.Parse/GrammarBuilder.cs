using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            : base( context.Errors.First(), context.ErrorMessage )
        {
        }
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
                parser.Define( parser.ResolvedParser | definition, parser.CollapseIfSingleElement );
            }
            else
            {
                parser = new NamedParser( name );
                _namedParsers.Add( name, parser );
                parser.Define( definition );
            }
        }

        public NamedParser Get( string name )
        {
            return new NamedParser( name, _namespace.Count == 0 ? null : _namespace.Peek(), this );
        }

        public NamedParser this[ string name ] => _namedParsers[name];

        public override string ToString()
        {
            return string.Join( Environment.NewLine,
                _namedParsers.Select( x => $"{x.Key} = {x.Value.ResolvedParser};" ).ToArray() );
        }

        private NamedParser GetExisting( string fullName )
        {
            NamedParser parser;
            return _namedParsers.TryGetValue( fullName, out parser ) ? parser : null;
        }

        public bool ResolveDefinition( NamedParser named )
        {
            if ( named.Namespace == null )
            {
                var existing = GetExisting( named.Name );
                if ( existing != null )
                {
                    named.Define( existing.ResolvedParser, existing.CollapseIfSingleElement );
                    return true;
                }

                return false;
            }

            var splitIndex = named.Namespace.Length;
            while ( true )
            {
                var name = splitIndex <= 0 ? named.Name : $"{named.Namespace.Substring( 0, splitIndex )}.{named.Name}";
                var existing = GetExisting( name );
                if ( existing != null )
                {
                    named.ResolvedName = name;
                    named.Define( existing.ResolvedParser, existing.CollapseIfSingleElement );
                    return true;
                }
                if ( splitIndex <= 0 ) break;

                splitIndex = named.Namespace.LastIndexOf( ".", splitIndex - 1 );
            }

            return false;
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
            Debug.Assert( definition.Parser == Parser.Definition );

            var name = definition[0].Value;

            rules.PushNamespace( name );

            var branch = definition.FirstOrDefault( x => x.Parser == Parser.Branch );
            var statementBlock = definition.FirstOrDefault( x => x.Parser == Parser.StatementBlock );

            if ( statementBlock != null )
            {
                ReadStatementBlock( statementBlock, rules );
            }

            Parser parser = null;

            if ( branch != null )
            {
                parser = ReadBranch( branch, rules );
            }

            rules.PopNamespace();

            if ( parser != null ) rules.Add( name, parser );
        }

        private static Parser ReadBranch( ParseResult branch, NamedParserCollection rules )
        {
            Debug.Assert( branch.Parser == Parser.Branch );

            return branch.InnerCount == 1
                ? ReadConcat( branch[0], rules )
                : new BranchParser( branch.Select( x => ReadConcat( x, rules ) ) );
        }

        private static Parser ReadConcat( ParseResult concat, NamedParserCollection rules )
        {
            Debug.Assert( concat.Parser == Parser.Concat );

            return concat.InnerCount == 1
                ? ReadModifier( concat[0], rules )
                : new ConcatParser( concat.Select( x => ReadModifier( x, rules ) ) );
        }

        private static Parser ReadModifier( ParseResult modifier, NamedParserCollection rules )
        {
            Debug.Assert( modifier.Parser == Parser.Modifier );

            if ( modifier.InnerCount == 1 ) return ReadTerm( modifier[0], rules );

            var term = ReadTerm( modifier[0], rules );

            switch ( modifier[1].Value )
            {
                case "?":
                    return term | "";
                case "+":
                    return term.Repeated;
                case "*":
                    return term.Repeated.Optional;
                default:
                    throw new Exception( $"Unrecognised modifier '{modifier[0].Value}'." );
            }
        }

        private static Parser ReadTerm( ParseResult term, NamedParserCollection rules )
        {
            Debug.Assert(term.Parser == Parser.Term);

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
                        case 'r':
                            builder.Append( '\r' );
                            break;
                        case 'n':
                            builder.Append( '\n' );
                            break;
                        case 't':
                            builder.Append( '\t' );
                            break;
                        default:
                            builder.Append( c );
                            break;
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
                        case '/':
                            builder.Append( '/' );
                            break;
                        default:
                            builder.Append( "\\" + c );
                            break;
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

            var parsedOptions = RegexParser.Compiled ? RegexOptions.Compiled : RegexOptions.None;
            for ( var i = 0; i < options.Length; ++i )
            {
                switch ( options.Value[i] )
                {
                    case 'i':
                        parsedOptions |= RegexOptions.IgnoreCase;
                        break;
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
            var header = specialBlock[0];
            var block = specialBlock[1];
            var stack = new Stack<IDisposable>();

            while ( header != null )
            {
                var item = header[0];

                if ( item.InnerCount == 1 )
                {
                    var ignore = ReadBranch( item[0], rules );
                    stack.Push( Parse.Parser.AllowWhitespace( ignore ) );
                }
                else
                    switch ( item.Value )
                    {
                        case "noignore":
                            stack.Push( Parse.Parser.ForbidWhitespace() );
                            break;
                        case "collapse":
                            stack.Push( Parse.Parser.EnableCollapseIfSingleElement() );
                            break;
                        default:
                            throw new NotImplementedException( item.Value );
                    }

                header = header.InnerCount == 1 ? null : header[1];
            }

            ReadStatementBlock( block, rules );

            while ( stack.Count > 0 )
            {
                stack.Pop().Dispose();
            }
        }
    }
}
