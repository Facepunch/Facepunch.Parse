using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Facepunch.Parse
{
    public enum IndentationMode
    {
        None,
        Tabs,
        Spaces
    }

    public class CodeWriterOptions
    {
        public static CodeWriterOptions Default { get; } = new CodeWriterOptions();

        public IndentationMode IndentationMode { get; set; } = IndentationMode.Spaces;
        public int IndentationWidth { get; set; } = 4;
    }

    public class CodeWriter : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly Stack<string> _indentation = new Stack<string>();

        public CodeWriterOptions Options { get; }

        public CodeWriter( TextWriter writer, CodeWriterOptions options = null )
        {
            Options = options ?? CodeWriterOptions.Default;

            _writer = writer;
            _endBlockDisposable = new EndBlockDisposable( this );
        }

        private string _spacesIndentation;

        private string SingleIndentation
        {
            get
            {
                switch (Options.IndentationMode)
                {
                    case IndentationMode.Tabs:
                        return "\t";
                    case IndentationMode.Spaces:
                        return _spacesIndentation == null || _spacesIndentation.Length != Options.IndentationWidth
                            ? (_spacesIndentation = string.Join( "", Enumerable.Repeat( " ", Options.IndentationWidth).ToArray() ))
                            : _spacesIndentation;
                    default:
                        return string.Empty;
                }
            }
        }

        private string Indentation => _indentation.Count == 0 ? string.Empty : _indentation.Peek();

        private readonly EndBlockDisposable _endBlockDisposable;
        private class EndBlockDisposable : IBlock
        {
            private readonly CodeWriter _writer;

            public EndBlockDisposable( CodeWriter writer )
            {
                _writer = writer;
            }

            void IDisposable.Dispose()
            {
                End();
            }

            public void End()
            {
                _writer.EndBlock();
            }
        }

        private bool _indented;

        private void ApplyIndentation()
        {
            if ( !_indented)
            {
                _writer.Write( Indentation );
                _indented = true;
            }
        }

        public void Write( string value )
        {
            ApplyIndentation();
            _writer.Write(value);
        }

        public void Write( Type type )
        {
            Write( $"global::{type.FullName}" );
        }

        public void WriteLine( string value )
        {
            Write( value );
            WriteLine();
        }

        public void WriteLine()
        {
            _writer.WriteLine();
            _indented = false;
        }

        public interface IBlock : IDisposable
        {
            void End();
        }

        public IBlock Block()
        {
            WriteLine( "{" );
            Indent();
            return _endBlockDisposable;
        }

        private void EndBlock()
        {
            Unindent();
            WriteLine( "}" );
        }

        public void Indent()
        {
            _indentation.Push( Indentation + SingleIndentation );
        }

        public void Unindent()
        {
            _indentation.Pop();
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }
}
