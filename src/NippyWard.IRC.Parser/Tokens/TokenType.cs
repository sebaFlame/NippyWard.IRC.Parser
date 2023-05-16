#pragma warning disable CA1708

namespace NippyWard.IRC.Parser.Tokens
{
    public enum TokenType : int
    {
        //First byte are terminals
        Null,                    //00
        CTCP,                    //01
        Bold,                    //02
        Color,                   //03
        HexColor,                //04
        Enquiry,                 //05
        Acknowledge,             //06
        Bell,                   //07
        Backspace,              //08
        HorizontalTab,          //09
        LineFeed,               //0A
        VerticalTab,            //0B
        FormFeed,               //0C
        CarriageReturn,         //0D
        ShiftOut,               //0E
        Reset,                  //0F
        DataLinkEscape,         //10
        Monospace,               //11
        DeviceControl2,         //12
        DeviceControl3,         //13
        DeviceControl4,         //14
        NegativeAcknowledge,    //15
        Synchronize,            //16
        EndOfTransmissionBlock, //17
        Cancel,                 //18
        EndOfMedium,            //19
        Substitute,             //1A
        Escape,                 //1B
        FileSeparator,          //1C
        Italics,                 //1D
        Strikethrough,          //1E
        Underline,              //1F
        Space,                  //20
        ExclamationMark,        //21
        DoubleQuote,            //22
        Number,                 //23
        Dollar,                 //24
        Percent,                //25
        Ampersand,              //26
        SingleQuote,            //27
        LeftParenthesis,        //28
        RightParenthesis,       //29
        Asterisk,               //2A
        Plus,                   //2B
        Comma,                  //2C
        Minus,                  //2D
        Period,                 //2E
        Slash,                  //2F
        Zero,                   //30
        One,                    //31
        Two,                    //32
        Three,                  //33
        Four,                   //34
        Five,                   //35
        Six,                    //36
        Seven,                  //37
        Eight,                  //38
        Nine,                   //39
        Colon,                  //3A
        Semicolon,              //3B
        LessThan,               //3C
        EqualitySign,           //3D
        GreaterThan,            //3E
        QuestionMark,           //3F
        AtSign,                 //40
        A,                      //41
        B,                      //42
        C,                      //43
        D,                      //44
        E,                      //45
        F,                      //46
        G,                      //47
        H,                      //48
        I,                      //49
        J,                      //4A
        K,                      //4B
        L,                      //4C
        M,                      //4D
        N,                      //4E
        O,                      //4F
        P,                      //50
        Q,                      //51
        R,                      //52
        S,                      //53
        T,                      //54
        U,                      //55
        V,                      //56
        W,                      //57
        X,                      //58
        Y,                      //59
        Z,                      //5A
        LeftSquareBracket,      //5B
        Backslash,              //5C
        RightSquareBracket,     //5D
        Caret,                  //5E
        Underscore,             //5F
        Accent,                 //60
        a,                      //61
        b,                      //62
        c,                      //63
        d,                      //64
        e,                      //65
        f,                      //66
        g,                      //67
        h,                      //68
        i,                      //69
        j,                      //6A
        k,                      //6B
        l,                      //6C
        m,                      //6D
        n,                      //6E
        o,                      //6F
        p,                      //70
        q,                      //71
        r,                      //72
        s,                      //73
        t,                      //74
        u,                      //75
        v,                      //76
        w,                      //77
        x,                      //78
        y,                      //79
        z,                      //7A
        LeftCurlyBracket,       //7B
        VerticalBar,            //7C
        RightCurlyBracket,      //7D
        Tilde,                  //7E
        Delete,                 //7F
        //non-ascii
        X80,
        X81,
        X82,
        X83,
        X84,
        X85,
        X86,
        X87,
        X88,
        X89,
        X8A,
        X8B,
        X8C,
        X8D,
        X8E,
        X8F,
        X90,
        X91,
        X92,
        X93,
        X94,
        X95,
        X96,
        X97,
        X98,
        X99,
        X9A,
        X9B,
        X9C,
        X9D,
        X9E,
        X9F,
        XA0,
        XA1,
        XA2,
        XA3,
        XA4,
        XA5,
        XA6,
        XA7,
        XA8,
        XA9,
        XAA,
        XAB,
        XAC,
        XAD,
        XAE,
        XAF,
        XB0,
        XB1,
        XB2,
        XB3,
        XB4,
        XB5,
        XB6,
        XB7,
        XB8,
        XB9,
        XBA,
        XBB,
        XBC,
        XBD,
        XBE,
        XBF,
        XC0,
        XC1,
        XC2,
        XC3,
        XC4,
        XC5,
        XC6,
        XC7,
        XC8,
        XC9,
        XCA,
        XCB,
        XCC,
        XCD,
        XCE,
        XCF,
        XD0,
        XD1,
        XD2,
        XD3,
        XD4,
        XD5,
        XD6,
        XD7,
        XD8,
        XD9,
        XDA,
        XDB,
        XDC,
        XDD,
        XDE,
        XDF,
        XE0,
        XE1,
        XE2,
        XE3,
        XE4,
        XE5,
        XE6,
        XE7,
        XE8,
        XE9,
        XEA,
        XEB,
        XEC,
        XED,
        XEE,
        XEF,
        XF0,
        XF1,
        XF2,
        XF3,
        XF4,
        XF5,
        XF6,
        XF7,
        XF8,
        XF9,
        XFA,
        XFB,
        XFC,
        XFD,
        XFE,
        XFF,
        //next are non-terminals in 2nd to 3rd byte
        Message = 1 << 8,
        Nickname,
        NicknameSuffix,
        Username,
        Channel,
        ChannelPrefix,
        ChannelPrefixWithoutMembership,
        ChannelId,
        ChannelSuffix,
        ChannelString,
        Host,
        HostSuffix,
        ShortName,
        ShortNamePrefix,
        ShortNameSuffix,
        ShortNameList,
        ServerName,
        UserHost,
        UserHostUsername,
        UserHostHostname,
        Integer,
        Timestamp,
        TagPrefix,
        Tags,
        Tag,
        TagsSuffix,
        TagsList,
        TagKey,
        TagKeySuffix,
        TagSuffix,
        TagValue,
        TagValueList,
        TagValueEscapeList,
        TagValueEscape,
        TagValueEscapeSuffix,
        TagValueEscapeBackslash,
        TagValueEscapeSemicolon,
        TagValueEscapeSpace,
        TagValueEscapeCr,
        TagValueEscapeLf,
        TagValueEscapeInvalid,
        SourcePrefix,
        SourcePrefixTarget,
        SourcePrefixTargetPrefix,
        SourcePrefixTargetPrefixPrefix,
        SourcePrefixTargetPrefixSuffix,
        SourcePrefixTargetPrefixTargetList,
        SourcePrefixTargetSuffix,
        SourcePrefixUsername,
        SourcePrefixHostname,
        Verb,
        CommandName,
        CommandCode,
        Params,
        ParamsPrefix,
        ParamsSuffix,
        Middle,
        MiddlePrefixList,
        MiddleSuffix,
        MiddlePrefixListFormatBase,
        MiddlePrefixWithColonList,
        Trailing,
        TrailingPrefix,
        TrailingList,
        TrailingListPrefix,
        TrailingListSuffix,
        UTF8WithoutNullCrLfSemiColonSpace,
        MiddlePrefixListTerminals,
        MiddlePrefixListFormatBaseTerminals,
        CrLf,
        ModeStringList,
        ModeString,
        ModeChars,
        ModeCharsList,
        Mode,
        KeyList,
        KeyListSuffix,
        KeyListItems,
        KeyListItem,
        Key,
        NicknameSpaceList,
        NicknameSpaceListSuffix,
        NicknameSpaceListItems,
        ChannelCommaList,
        ChannelCommaListSuffix,
        ChannelCommaListItems,
        ElistCondList,
        ElistCondListSuffix,
        ElistCondListItems,
        ElistCond,
        MsgTarget,
        MsgTargetSuffix,
        MsgTargetItems,
        MsgTo,
        MsgToTargetMask,
        MsgToTargetMaskLetters,
        MsgToNickname,
        MsgToNicknameSuffix,
        MsgToUserHost,
        MsgToUserHostServer,
        MsgToChannel,
        MsgToChannelPrefix,
        MsgToChannelPrefixChannelPrefixSuffix,
        MsgToChannelChannelPrefixMembership,
        ChannelMembershipPrefixWithoutChannelPrefix,
        ChannelMembershipPrefix,
        MyInfoReply,
        MyInfoReplySuffix,
        Version,
        ISupport,
        ISupportSuffix,
        ISupportList,
        ISupportToken,
        ISupportTokenNegated,
        ISupportTokenSuffix,
        ISupportParameter,
        ISupportValue,
        ISupportValueSuffix,
        ISupportValueList,
        ISupportValueItem,
        ISupportValueItemSuffix,
        ISupportValueItemSuffixValue,
        ISupportValueItemTerminals,
        ISupportValueItemEscape,
        ISupportValueItemEscapeSuffix,
        ISupportValueItemEscapeBackslash,
        ISupportValueItemEscapeSpace,
        ISupportValueItemEscapeEqual,
        CapList,
        CapListSuffix,
        CapListItems,
        CapListItem,
        CapListItemSuffix,
        CapListItemKey,
        CapListItemKeySuffix,
        CapListItemValueList,
        CapListItemValueListSuffix,
        CapListItemValueListItems,
        CapListItemValueListItem,
        NameReply,
        NameReplyChannelType,
        NicknameMembershipSpaceList,
        NicknameMembershipSpaceListSuffix,
        NicknameMembershipSpaceListItems,
        NicknameMembership,
        ChannelMembershipSpaceList,
        ChannelMembershipSpaceListSuffix,
        ChannelMembershipSpaceListItems,
        UserHostList,
        UserHostListSuffix,
        UserHostListItems,
        UserHostListItem,
        UserHostListOp,
        UserHostListAway,
        UserHostListHostname,
        UserHostListHostnamePrefix,
        UserHostListHostnameSuffix,
        WhoReply,
        WhoReplyPrefix,
        WhoReplyFlags,
        WhoReplyAway,
        WhoReplyFlagsOp,
        WhoReplyChannelMembership,
        WhoIsUserReply,
        WhoIsRealName,
        WhoIsServerReply,
        ServerInfo,
        WhoWasReply,
        WhoIsIdleReply,
        ListReply,
        Topic,
        CTCPMessage,
        CTCPCommand,
        CTCPMiddle,
        CTCPParams,
        CTCPParamsSuffix,
        CTCPParamsMiddle,
        CTCPMessageSuffix,
        DCCMessage,
        DCCType,
        DCCArgument,
        DCCQuotedArgument,
        DCCFilenameList,
        DCCFilenameSpaceList,
        DCCQuotedFilename,
        //all formatting in 4th byte
        Format = 1 << 24,
        BoldFormat,
        ItalicsFormat,
        UnderlineFormat,
        StrikethroughFormat,
        MonospaceFormat,
        ResetFormat,
        ColorFormat,
        ColorFormatSuffix,
        ColorCombination,
        ColorCombinationSuffix,
        ForegroundColor,
        BackgroundColor,
        ColorNumber,
        ColorSuffix,
        HexDecimal,
        HexColorTriplet,
        HexColorFormat,
        HexColorCombination,
        HexColorCombinationSuffix,
        ForegroundHexColor,
        BackgroundHexColor,
        ConstructedMessage = int.MaxValue
    }
}
