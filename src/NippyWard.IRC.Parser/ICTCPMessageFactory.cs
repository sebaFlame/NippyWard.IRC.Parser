using NippyWard.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NippyWard.IRC.Parser
{
    public interface ICTCPMessageFactory
    {
        ICTCPMessageFactory Command(string command);
        ICTCPMessageFactory Command(Utf8String command);
        ICTCPMessageFactory Parameter(string parameter);
        ICTCPMessageFactory Parameter(Utf8String parameter);
    }
}
