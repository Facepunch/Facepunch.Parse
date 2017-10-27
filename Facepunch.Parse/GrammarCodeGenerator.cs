using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Facepunch.Parse
{
    public class GrammarCodeGenerator
    {
        public CodeWriterOptions Options { get; }

        public GrammarCodeGenerator( CodeWriterOptions options = null )
        {
            Options = options ?? CodeWriterOptions.Default;
        }

        public void WriteGrammar( string fullClassName, NamedParserCollection grammar, string rootParserName, string filePath )
        {
            using ( var writer = File.CreateText( filePath ) )
            {
                WriteGrammar( fullClassName, grammar, rootParserName, writer );
            }
        }

        internal const string RegexOptionsVariable = "regexOptions";

        private void WriteParser( CodeWriter writer, Parser parser, double precedence = 0d )
        {
            const double branchPrecedence = 1d;
            const double concatPrecedence = 2d;
            const double unaryPrecedence = 3d;

            if ( parser is IUnaryParser )
            {
                var unary = (IUnaryParser) parser;

                var terminal = unary.Inner is TokenParser || unary.Inner is RegexParser || unary.Inner is EmptyParser;

                if ( parser is NotParser )
                {
                    writer.Write( "!" );
                }

                if ( terminal )
                {
                    writer.Write( "((" );
                    writer.Write( typeof(Parser) );
                    writer.Write( ") " );
                }

                WriteParser( writer, unary.Inner, unaryPrecedence );

                if ( terminal )
                {
                    writer.Write( ")" );
                }

                if ( parser is RepeatedParser )
                {
                    writer.Write( $".{nameof( Parser.Repeated )}" );
                }
                else if ( parser is StrictParser )
                {
                    writer.Write( $".{nameof( Parser.Strict )}" );
                }

                return;
            }

            if ( parser is EmptyParser )
            {
                writer.Write( "\"\"" );
                return;
            }

            if (parser is TokenParser)
            {
                var token = (TokenParser)parser;
                writer.Write("@\"");
                writer.Write(token.Token.Replace("\"", "\"\""));
                writer.Write("\"");
                return;
            }

            if ( parser is RegexParser )
            {
                var regex = (RegexParser) parser;
                writer.Write( "new " );
                writer.Write( typeof(Regex) );
                writer.Write( "(@\"" );
                writer.Write( regex.Regex.ToString().Replace( "\"", "\"\"" ) );
                writer.Write( "\", " );

                writer.Write($"{RegexOptionsVariable}");

                var options = regex.Regex.Options & ~RegexOptions.Compiled;
                if ( options != 0 )
                {
                    writer.Write( " | (" );
                    writer.Write( typeof(RegexOptions) );
                    writer.Write( $") {(int) options}" );
                }
                writer.Write( ")" );
                return;
            }

            if ( parser is BranchParser )
            {
                var branch = (BranchParser) parser;

                if ( branch.Inner.All( x => x is TokenParser || x is RegexParser || x is EmptyParser ) )
                {
                    writer.Write( "(" );
                    writer.Write( typeof(Parser) );
                    writer.Write( ") " );
                }

                if ( precedence > branchPrecedence ) writer.Write( "(" );
                var first = true;
                foreach ( var inner in branch.Inner )
                {
                    if ( first ) first = false;
                    else writer.Write( " | " );
                    WriteParser( writer, inner, precedence > branchPrecedence ? 0d : branchPrecedence );
                }
                if ( precedence > branchPrecedence ) writer.Write( ")" );

                return;
            }

            if ( parser is ConcatParser )
            {
                var concat = (ConcatParser) parser;
                if ( concat.Inner.All( x => x is TokenParser || x is RegexParser || x is EmptyParser ) )
                {
                    writer.Write( "(" );
                    writer.Write( typeof(Parser) );
                    writer.Write( ") " );
                }

                if ( precedence > concatPrecedence ) writer.Write( "(" );
                var first = true;
                foreach ( var inner in concat.Inner )
                {
                    if ( first ) first = false;
                    else writer.Write( " + " );
                    WriteParser( writer, inner, precedence > concatPrecedence ? 0d : concatPrecedence);
                }
                if ( precedence > concatPrecedence ) writer.Write( ")" );

                return;
            }

            if ( parser is NamedParser )
            {
                var named = (NamedParser) parser;
                var resolved = named.ResolvedParser;

                writer.Write( NormalizeParserName( named.ResolvedName ) );

                return;
            }

            throw new NotImplementedException( parser.GetType().FullName );
        }

        private string NormalizeParserName( string name )
        {
            return name.Replace( '.', '_' );
        }

        public void WriteGrammar( string fullClassName, NamedParserCollection grammar, string rootParserName, TextWriter dest )
        {
            var writer = new CodeWriter( dest, Options );

            var splitName = fullClassName.Split( '.' );
            var @namespace = splitName.Length > 1
                ? string.Join( ".", splitName.Take( splitName.Length - 1 ).ToArray() )
                : null;
            var className = splitName[splitName.Length - 1];

            writer.WriteLine( $"namespace {@namespace}" );
            using ( writer.Block() )
            {
                writer.Write( $"public class {className} : " );
                writer.Write( typeof(CustomParser) );
                writer.WriteLine();
                using ( writer.Block() )
                {
                    foreach ( var name in grammar.ParserNames )
                    {
                        writer.Write( "public " );
                        writer.Write( typeof(NamedParser) );
                        writer.WriteLine( $" {NormalizeParserName(name)};" );
                    }

                    writer.WriteLine();

                    writer.Write( "protected override " );
                    writer.Write( typeof(Parser) );
                    writer.WriteLine( " OnDefine()" );
                    using ( writer.Block() )
                    {
                        writer.Write( $"var {RegexOptionsVariable} = " );
                        writer.Write( typeof(RegexParser) );
                        writer.WriteLine( $".{nameof( RegexParser.Compiled )}" );
                        writer.Indent();
                        writer.Write( "? " );
                        writer.Write( typeof(RegexOptions) );
                        writer.WriteLine( $".{nameof( RegexOptions.Compiled )}" );
                        writer.Write( ": " );
                        writer.Write( typeof(RegexOptions) );
                        writer.WriteLine( $".{nameof( RegexOptions.None )};" );
                        writer.Unindent();

                        foreach ( var whitespaceGroup in grammar.ParserNames.GroupBy( x => grammar[x].WhitespaceParser ) )
                        {
                            writer.WriteLine();

                            CodeWriter.IBlock whitespaceBlock = null;
                            if ( whitespaceGroup.Key != null )
                            {
                                writer.Write( "using (" );
                                writer.Write( typeof(Parser) );
                                writer.Write( $".{nameof( Parser.AllowWhitespace )}(" );
                                WriteParser( writer, whitespaceGroup.Key );
                                writer.WriteLine( "))" );
                                whitespaceBlock = writer.Block();
                            }

                            foreach ( var collapseGroup in whitespaceGroup.GroupBy( x => grammar[x].CollapseIfSingleElement ) )
                            {
                                CodeWriter.IBlock collapseBlock = null;
                                if ( collapseGroup.Key )
                                {
                                    writer.WriteLine();
                                    writer.Write( "using (" );
                                    writer.Write( typeof(Parser) );
                                    writer.WriteLine( $".{nameof( Parser.EnableCollapseIfSingleElement )}())" );
                                    collapseBlock = writer.Block();
                                }

                                foreach ( var parserName in collapseGroup )
                                {
                                    writer.Write( $"this[{NormalizeParserName(parserName)}] = " );
                                    WriteParser( writer, grammar[parserName].ResolvedParser );
                                    writer.WriteLine( ";" );
                                }

                                collapseBlock?.End();
                            }

                            whitespaceBlock?.End();
                        }

                        writer.WriteLine($"return {rootParserName};");
                    }
                }
            }

            dest.Flush();
        }
    }
}