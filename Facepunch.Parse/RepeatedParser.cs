﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Facepunch.Parse
{
    public interface IUnaryParser
    {
        Parser Inner { get; }
    }

    public sealed class RepeatedParser : Parser, IUnaryParser
    {
        private readonly Parser _inner;

        public override bool FlattenHierarchy => true;

        public Parser Inner => _inner;

        public RepeatedParser( Parser inner )
        {
            _inner = inner;
        }

        protected override bool OnParse( ParseResult result, bool errorPass )
        {
            if ( !result.Read( _inner, errorPass) ) return false;

            ParseResult peek;
            while ( (peek = result.Peek( _inner, errorPass)).Success && peek.Length > 0 )
            {
                result.Apply( peek, errorPass );
            }

            peek.Dispose();

            return true;
        }

        public override string ToString()
        {
            return $"{_inner}+";
        }
    }
}
