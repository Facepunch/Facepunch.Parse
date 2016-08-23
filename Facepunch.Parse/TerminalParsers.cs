using System.Text.RegularExpressions;

namespace Facepunch.Parse
{
    public sealed class EmptyParser : Parser
    {
        public static EmptyParser Instance { get; } = new EmptyParser();

        public override bool Parse(ParseResult result)
        {
            return true;
        }
    }

    public sealed class NullParser : Parser
    {
        public static NullParser Instance { get; } = new NullParser();

        public override bool Parse(ParseResult result)
        {
            return result.Error(ParseError.NullParser, null);
        }
    }

    public sealed class TokenParser : Parser
    {
        public string Token { get; }
        public override bool OmitFromResult => true;

        public TokenParser(string token)
        {
            Token = token;
        }

        public override bool Parse(ParseResult result)
        {
            return result.Read(Token) || result.Error(ParseError.ExpectedToken, $"'{Token}'");
        }

        public override string ToString()
        {
            return $"\"{Token}\"";
        }
    }

    public sealed class RegexParser : Parser
    {
        public Regex Regex { get; }

        public RegexParser(Regex regex)
        {
            Regex = regex;
        }

        public override bool Parse(ParseResult result)
        {
            return result.Read(Regex) || result.Error(ParseError.ExpectedToken, $"/{Regex}/");
        }

        public override string ToString()
        {
            return $"/{Regex}/";
        }
    }
}
