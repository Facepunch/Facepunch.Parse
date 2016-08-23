using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Facepunch.Parse
{
    public abstract class Parser
    {
        public static implicit operator Parser(string token)
        {
            if (token.Length == 0) return EmptyParser.Instance;
            return new TokenParser(token);
        }

        public static implicit operator Parser(Regex regex)
        {
            return new RegexParser(regex);
        }

        public static ConcatParser operator +(Parser a, Parser b)
        {
            var aConcat = a as ConcatParser;
            var bConcat = b as ConcatParser;

            var result = new ConcatParser();

            if (aConcat != null) result.AddRange(aConcat.Inner); else result.Add(a);
            if (bConcat != null) result.AddRange(bConcat.Inner); else result.Add(b);

            return result;
        }

        public static BranchParser operator |(Parser a, Parser b)
        {
            var aBranch = a as BranchParser;
            var bBranch = b as BranchParser;

            var result = new BranchParser();

            if (aBranch != null) result.AddRange(aBranch.Inner); else result.Add(a);
            if (bBranch != null) result.AddRange(bBranch.Inner); else result.Add(b);

            return result;
        }

        public ParseResult Parse(string source)
        {
            var result = new ParseResult(source, this);
            Parse(result);
            return result;
        }

        public virtual bool FlattenHierarchy { get; } = false;
        public virtual bool OmitFromResult { get; } = false;

        public abstract bool Parse(ParseResult result);

        private string _elementName;
        protected virtual string ElementName
        {
            get
            {
                if (_elementName != null) return _elementName;

                _elementName = GetType().Name;
                if (_elementName.EndsWith("Parser"))
                {
                    _elementName = _elementName.Substring(0, _elementName.Length - "Parser".Length);
                }

                return _elementName;
            }
        }

        public virtual XElement ToXElement(ParseResult result)
        {
            var elem = new XElement(ElementName);

            if (result.InnerCount == 0)
            {
                if (result.ErrorType > ParseError.SubParser)
                {
                    elem.Add(new XElement("ParseError", result.ErrorMessage));
                }
                else
                {
                    elem.Value = result.ToString();
                }
            }
            else
            {
                foreach (var inner in result.Inner)
                {
                    elem.Add(inner.ToXElement());
                }
            }

            return elem;
        }
    }
}
