using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser
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
