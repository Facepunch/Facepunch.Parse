using System;

namespace Facepunch.Parse
{
    public interface INamedParserResolver
    {
        Parser Resolve(NamedParser named);
    }

    public sealed class NamedParser : Parser
    {
        private Parser _resolvedParser;

        public string Name { get; }
        public INamedParserResolver Resolver { get; }

        public Parser ResolvedParser => _resolvedParser ?? (_resolvedParser = Resolver.Resolve(this));

        public void Resolve(Parser value)
        {
            _resolvedParser = value;
        }

        public NamedParser(string name, INamedParserResolver resolver = null)
        {
            Name = name;
            Resolver = resolver;
        }

        public override bool Parse(ParseResult result)
        {
            if (ResolvedParser == null) throw new Exception($"Could not resolve parser with name '{Name}'");

            return ResolvedParser.Parse(result);
        }

        protected override string ElementName => Name;

        public override string ToString()
        {
            return $"<{Name}>";
        }
    }
}
