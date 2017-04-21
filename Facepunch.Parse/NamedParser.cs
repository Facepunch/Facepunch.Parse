using System;
using System.Collections.Generic;

namespace Facepunch.Parse
{
    public interface INamedParserResolver
    {
        bool ResolveDefinition( NamedParser named );
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
        private readonly string _nameEnd;

        private bool _collapseSingletons;

        public string Name { get; }
        public string Namespace { get; }
        public INamedParserResolver Resolver { get; }

        public override bool CollapseIfSingleElement
        {
            get
            {
                if ( !IsResolved ) AttemptResolve();
                return _collapseSingletons;
            }
        }

        public string ResolvedName { get; set; }

        public bool IsResolved => _resolvedParser != null;

        public Parser ResolvedParser => _resolvedParser ?? (_resolvedParser = AttemptResolve());

        protected virtual Parser AttemptResolve()
        {
            Resolver?.ResolveDefinition( this );
            return _resolvedParser;
        }

        public void Define( Parser value )
        {
            _resolvedParser = value;
            _collapseSingletons = CurrentCollapseState;
        }

        public void Define( Parser value, bool collapseIfSingleElement )
        {
            _resolvedParser = value;
            _collapseSingletons = collapseIfSingleElement;
        }

        protected NamedParser()
        {
            _nameEnd = Name = GetType().Name;
            Resolver = null;
        }

        public NamedParser( string name, string @namespace = null, INamedParserResolver resolver = null )
        {
            Name = name;
            Namespace = @namespace;
            Resolver = resolver;

            _nameEnd = name.Substring( name.LastIndexOf( "." ) + 1 );
        }

        protected override bool OnParse( ParseResult result, bool errorPass )
        {
            if ( ResolvedParser == null ) throw new Exception( $"Could not resolve parser with name '{Name}'" );

            return result.Read( ResolvedParser, errorPass );
        }

        public override string ElementName => ResolvedName ?? Name;

        public override int GetHashCode()
        {
            return _nameEnd.GetHashCode();
        }

        public override bool Equals( Parser other )
        {
            var named = other as NamedParser;
            return named != null && named._nameEnd == _nameEnd && named.ResolvedParser.Equals( ResolvedParser );
        }

        public override string ToString()
        {
            return $"<{ElementName}>";
        }
    }
}
