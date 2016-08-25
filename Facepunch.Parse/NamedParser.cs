using System;
using System.Collections.Generic;

namespace Facepunch.Parse
{
    public interface INamedParserResolver
    {
        Parser Resolve( NamedParser named );
    }

    public class NamedParser : Parser
    {
        private static readonly Dictionary<Type, NamedParser> _sSingletons = new Dictionary<Type, NamedParser>();

        public static TParser Get<TParser>()
            where TParser : NamedParser, new()
        {
            var type = typeof (TParser);
            NamedParser singleton;
            if ( _sSingletons.TryGetValue( type, out singleton ) ) return (TParser) singleton;

            singleton = new TParser();
            _sSingletons.Add( type, singleton );
            return (TParser) singleton;
        }

        private Parser _resolvedParser;

        public string Name { get; }
        public INamedParserResolver Resolver { get; }

        public bool IsResolved => _resolvedParser != null;

        public Parser ResolvedParser => _resolvedParser ?? (_resolvedParser = Value);

        protected virtual Parser Value => Resolver?.Resolve( this );

        public void Resolve( Parser value )
        {
            _resolvedParser = value;
        }

        protected NamedParser()
        {
            Name = GetType().Name;
            Resolver = null;
        }

        public NamedParser( string name, INamedParserResolver resolver = null )
        {
            Name = name;
            Resolver = resolver;
        }

        protected override bool OnParse( ParseResult result )
        {
            if ( ResolvedParser == null ) throw new Exception( $"Could not resolve parser with name '{Name}'" );

            return ResolvedParser.Parse( result );
        }

        protected override string ElementName => Name;

        public override string ToString()
        {
            return $"<{Name}>";
        }
    }
}
