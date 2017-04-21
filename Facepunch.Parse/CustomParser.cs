using System.Reflection;

namespace Facepunch.Parse
{
    public abstract class CustomParser : Parser
    {
        private Parser _definedParser;

        protected abstract Parser OnDefine();

        private bool IsDefined => _definedParser != null;

        private void Define()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach ( var field in GetType().GetFields(flags) )
            {
                if ( field.FieldType != typeof (NamedParser) ) continue;

                var val = field.GetValue( this );
                if ( val != null ) continue;

                field.SetValue( this, new NamedParser( field.Name ) );
            }

            _definedParser = OnDefine();
        }

        public Parser this[ NamedParser parser ]
        {
            get { return parser; }
            set { parser.Define( value ); }
        }

        protected override bool OnParse( ParseResult result, bool errorPass )
        {
            if ( !IsDefined ) Define();
            return result.Read( _definedParser, errorPass );
        }
    }
}
