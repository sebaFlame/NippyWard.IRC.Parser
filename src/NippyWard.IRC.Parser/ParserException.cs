using System;

namespace NippyWard.IRC.Parser
{
    public class ParserException : Exception
    {
        public ParserException(string message)
            : base(message)
        { }
    }
}
