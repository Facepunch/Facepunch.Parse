using System.Text.RegularExpressions;

namespace Facepunch.Parse
{
    public sealed class EmptyParser : Parser
    {
        public static EmptyParser Instance { get; } = new EmptyParser();

        public override bool OmitFromResult => true;

        protected override bool OnParse( ParseResult result, bool errorPass )
        {
            return true;
        }
    }

    public sealed class NullParser : Parser
    {
        public static NullParser Instance { get; } = new NullParser();

        protected override bool OnParse( ParseResult result, bool errorPass)
        {
            return result.Error( ParseError.NullParser, null );
        }
    }

    public sealed class TokenParser : Parser
    {
        public string Token { get; }
        public override bool OmitFromResult => true;

        public TokenParser( string token )
        {
            Token = token;
        }

        protected override bool OnParse( ParseResult result, bool errorPass)
        {
            return result.Read( Token ) || result.Error( ParseError.ExpectedToken, errorPass ? $"'{Token}'" : "" );
        }

        public override string ToString()
        {
            return $"\"{Token}\"";
        }
    }

    public sealed class RegexParser : Parser
    {
        public Regex Regex { get; }
        public override bool OmitFromResult => true;

        public RegexParser( Regex regex )
        {
            Regex = regex;
        }

        protected override bool OnParse( ParseResult result, bool errorPass)
        {
            return result.Read( Regex ) || result.Error( ParseError.ExpectedToken, errorPass ? $"/{Regex}/" : "" );
        }

        public override string ToString()
        {
            return $"/{Regex}/";
        }
    }
}
