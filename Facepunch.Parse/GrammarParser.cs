﻿using System.Text.RegularExpressions;

namespace Facepunch.Parse
{
    public class GrammarParser : CustomParser
    {
        public NamedParser Ignore;
        public NamedParser Whitespace;
        public NamedParser SingleLineComment;
        public NamedParser MultiLineComment;
        public NamedParser StatementBlock;
        public NamedParser Statement;
        public NamedParser IgnoreBlock;
        public NamedParser IgnoreBlockHeader;
        public NamedParser Definition;
        public NamedParser Branch;
        public NamedParser Concat;
        public NamedParser Term;
        public NamedParser NonTerminal;
        public NamedParser String;
        public NamedParser StringValueSingle;
        public NamedParser StringValueDouble;
        public NamedParser Regex;
        public NamedParser RegexValue;
        public NamedParser RegexOptions;
        public NamedParser RegexOption;

        protected override Parser OnDefine()
        {
            this[Ignore] = Whitespace | SingleLineComment | MultiLineComment;
            this[Whitespace] = new Regex( @"\s" );
            this[SingleLineComment] = new Regex( @"//[^\n]*(\n|$)" );
            this[MultiLineComment] = new Regex( @"/\*([^*]|\*[^/])*\*/" );

            this[NonTerminal] = new Regex( @"[a-z_][a-z0-9_]*(\.[a-z_][a-z0-9_]*)*", System.Text.RegularExpressions.RegexOptions.IgnoreCase );
            this[String] = "\"" + StringValueDouble + "\"" | "'" + StringValueSingle + "'" ;
            this[StringValueSingle] = new Regex( @"(\\[\\rnt']|[^\\'])*" );
            this[StringValueDouble] = new Regex( @"(\\[\\""rnt]|[^\\""])*" );
            this[Regex] = "/" + RegexValue + "/" + RegexOptions;
            this[RegexValue] = new Regex( @"(\\.|\[[^\]]+\]|[^\\[/])+" );
            this[RegexOptions] = "" | (RegexOption + RegexOptions);
            this[RegexOption] = "i";

            using ( AllowWhitespace( Ignore ) )
            {
                this[StatementBlock] = Statement + (StatementBlock | "");
                this[Statement] = Definition | IgnoreBlock;
                this[IgnoreBlock] = IgnoreBlockHeader + "{" + StatementBlock + "}";
                this[IgnoreBlockHeader] = "ignore" + Branch | "noignore";
                this[Definition] = NonTerminal + "=" + Branch + (";" | "{" + StatementBlock + "}");
                this[Branch] = Concat + ("|" + Branch | "");
                this[Concat] = Term + (Concat | "");
                this[Term] = String | Regex | NonTerminal | "(" + Branch + ")";
            }

            return StatementBlock + EndOfInput;
        }
    }
}
