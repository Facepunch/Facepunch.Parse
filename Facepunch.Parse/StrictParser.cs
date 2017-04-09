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

        protected override bool OnParse( ParseResult result )
        {
            var next = result.Peek( _inner );
            if ( next.Success ) result.Apply( next );
            else result.Error( result.ErrorType, result.ErrorMessage );
            return next.Success;
        }

        public override string ToString()
        {
            return $"${_inner}";
        }
    }
}
