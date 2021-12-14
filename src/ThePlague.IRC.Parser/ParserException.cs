using System;

namespace ThePlague.IRC.Parser
{
    public class ParserException : Exception
    {
        public ParserException(string message)
            : base(message)
        { }
    }
}
