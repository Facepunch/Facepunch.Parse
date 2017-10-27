namespace Facepunch.Parse
{
    public class StrictParser : Parser, IUnaryParser
    {
        private readonly Parser _inner;

        public Parser Inner => _inner;

        public override bool FlattenHierarchy => true;

        public StrictParser( Parser inner )
        {
            _inner = inner;
        }

        protected override bool OnParse( ParseResult result, bool errorPass )
        {
            var next = result.Peek( _inner, errorPass);
            var success = next.Success;
            if ( success ) result.Apply( next, errorPass );
            else result.Error( next, errorPass, false );
            return success;
        }

        public override string ToString()
        {
            return $"${_inner}";
        }
    }
}
