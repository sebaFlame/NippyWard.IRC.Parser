using ThePlague.Model.Core.Text;

namespace ThePlague.IRC.Parser.Tests
{
    public class TestIRCMessageFactory : BaseIRCMessageFactory
    {
        //keep source, tags and 1 parameter (LR)
        public TestIRCMessageFactory()
            : base(1, true, true)
        { }

        public TestIRCMessageFactory
        (
            int keepParams,
            bool keepSourcePrefix,
            bool keepTags
        )
            : base(keepParams, keepSourcePrefix, keepTags)
        { }

        protected override BaseIRCMessageFactory ParameterTooLong
        (
            Utf8String parameter,
            int extraLength
        )
            => this.SplitParameterAndAddToNewMessage(parameter, extraLength);

        protected override BaseIRCMessageFactory TooManyParameters
        (
            Utf8String parameter
        )
            => this.AddParameterToNewMessage(parameter);
    }
}
