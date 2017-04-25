using System.Text.RegularExpressions;

namespace Facepunch.Parse
{
    public class GrammarParser : CustomParser
    {
        public NamedParser Whitespace;
        public NamedParser SingleLineComment;
        public NamedParser MultiLineComment;
        public NamedParser StatementBlock;
        public NamedParser Statement;
        public NamedParser SpecialBlock;
        public NamedParser SpecialBlockHeader;
        public NamedParser SpecialBlockType;
        public NamedParser IgnoreBlockHeader;
        public NamedParser Definition;
        public NamedParser Branch;
        public NamedParser Concat;
        public NamedParser Modifier;
        public NamedParser ModifierPostfix;
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
            Parser ignore = "ignore";
            Parser noignore = "noignore";
            Parser collapse = "collapse";

            var regexOptions = RegexParser.Compiled
                ? System.Text.RegularExpressions.RegexOptions.Compiled
                : System.Text.RegularExpressions.RegexOptions.None;

            this[NonTerminal] = new Regex( @"[a-z_][a-z0-9_]*(\.[a-z_][a-z0-9_]*)*",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | regexOptions );
            this[String] = "\"" + StringValueDouble + "\"" | "'" + StringValueSingle + "'";
            this[StringValueSingle] = new Regex( @"(\\[\\rnt']|[^\\'])*", regexOptions );
            this[StringValueDouble] = new Regex( @"(\\[\\""rnt]|[^\\""])*", regexOptions );
            this[Regex] = "/" + RegexValue + "/" + RegexOptions;
            this[RegexValue] = new Regex( @"(\\.|\[[^\]]+\]|[^\\[/])+", regexOptions );
            this[RegexOptions] = RegexOption.Repeated.Optional;
            this[RegexOption] = "i";

            using ( AllowWhitespace( new Regex( @"\s+|//[^\n]*(\n|$)|/\*([^*]|\*[^/])*\*/", regexOptions ) ) )
            {
                this[StatementBlock] = Statement.Repeated;
                this[Statement] = SpecialBlock | Definition;
                this[SpecialBlock] = SpecialBlockHeader + "{" + StatementBlock + "}";
                this[SpecialBlockHeader] = SpecialBlockType + ("," + SpecialBlockType).Repeated.Optional;
                this[SpecialBlockType] = ignore + Branch | noignore | collapse;
                this[Definition] = !(ignore | noignore | collapse) + NonTerminal + ("=" + Branch).Optional +
                                   (";" | "{" + StatementBlock + "}");
                this[Branch] = Concat + ("|" + Concat).Repeated.Optional;
                this[Concat] = Modifier.Repeated;
                this[Modifier] = Term + ModifierPostfix.Optional;
                this[ModifierPostfix] = (Parser) "?" | "*" | "+";
                this[Term] = String | Regex | NonTerminal | "(" + Branch + ")";
            }

            return StatementBlock + EndOfInput;
        }
    }
}
