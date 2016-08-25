using System.Text.RegularExpressions;

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
        public NamedParser Definition;
        public NamedParser Branch;
        public NamedParser Concat;
        public NamedParser Term;
        public NamedParser NonTerminal;
        public NamedParser String;
        public NamedParser StringValue;
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

            this[NonTerminal] = new Regex( @"[a-z0-9_]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase );
            this[String] = "\"" + StringValue + "\"";
            this[StringValue] = new Regex( @"(\\[\\""rnt]|[^\\""])*" );
            this[Regex] = "/" + RegexValue + "/" + RegexOptions;
            this[RegexValue] = new Regex( @"(\\.|\[[^\]]+\]|[^\\[/])+" );
            this[RegexOptions] = "" | (RegexOption + RegexOptions);
            this[RegexOption] = "i";

            using ( AllowWhitespace( Ignore ) )
            {
                this[StatementBlock] = Statement | (Statement + StatementBlock);
                this[Statement] = Definition | IgnoreBlock;
                this[IgnoreBlock] = "ignore" + Branch + "{" + StatementBlock + "}";
                this[Definition] = NonTerminal + "=" + Branch + ";";
                this[Branch] = Concat | (Concat + "|" + Branch);
                this[Concat] = Term | (Term + Concat);
                this[Term] = String | Regex | NonTerminal | "(" + Branch + ")";
            }

            return StatementBlock;
        }
    }
}
