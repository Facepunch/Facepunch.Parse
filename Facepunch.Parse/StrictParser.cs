namespace Facepunch.Parse
{
    public class StrictParser : Parser
    {
        private readonly Parser _inner;

        public override bool FlattenHierarchy => true;

        public StrictParser( Parser inner )
        {
            _inner = inner;
        }

        protected override bool OnParse( ParseResult result, bool errorPass )
        {
            var next = result.Peek( _inner, errorPass);
            if ( next.Success ) result.Apply( next, errorPass );
            else result.Error( result, errorPass );
            return next.Success;
        }

        public override string ToString()
        {
            return $"${_inner}";
        }
    }
}
