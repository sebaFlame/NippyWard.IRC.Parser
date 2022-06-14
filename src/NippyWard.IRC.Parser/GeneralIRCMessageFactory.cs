using NippyWard.Model.Core.Text;

namespace NippyWard.IRC.Parser
{
    public class GeneralIRCMessageFactory : BaseIRCMessageFactory
    {
        protected override BaseIRCMessageFactory ParameterTooLong
        (
            Utf8String parameter,
            int extraLength
        )
            => throw ThrowParameterTooLong();

        protected override BaseIRCMessageFactory TooManyParameters
        (
            Utf8String parameter
        )
            => throw ThrowTooManyParameters();
    }
}
