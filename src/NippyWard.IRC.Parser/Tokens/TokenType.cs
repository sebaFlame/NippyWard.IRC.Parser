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
        Nickname = 2 << 8,
        NicknameSuffix = 3 << 8,
        Username = 4 << 8,
        Channel = 5 << 8,
        ChannelPrefix = 6 << 8,
        ChannelPrefixWithoutMembership = 7 << 8,
        ChannelId = 8 << 8,
        ChannelSuffix = 9 << 8,
        ChannelString = 10 << 8,
        Host = 11 << 8,
        HostSuffix = 12 << 8,
        ShortName = 13 << 8,
        ShortNamePrefix = 14 << 8,
        ShortNameSuffix = 15 << 8,
        ShortNameList = 16 << 8,
        ServerName = 17 << 8,
        UserHost = 18 << 8,
        UserHostUsername = 19 << 8,
        UserHostHostname = 20 << 8,
        Integer = 21 << 8,
        Timestamp = 22 << 8,
        TagPrefix = 23 << 8,
        Tags = 24 << 8,
        Tag = 25 << 8,
        TagsSuffix = 26 << 8,
        TagsList = 27 << 8,
        TagKey = 28 << 8,
        TagKeySuffix = 29 << 8,
        TagSuffix = 30 << 8,
        TagValue = 31 << 8,
        TagValueList = 32 << 8,
        TagValueEscapeList = 33 << 8,
        TagValueEscape = 34 << 8,
        TagValueEscapeSuffix = 35 << 8,
        TagValueEscapeBackslash = 36 << 8,
        TagValueEscapeSemicolon = 37 << 8,
        TagValueEscapeSpace = 38 << 8,
        TagValueEscapeCr = 39 << 8,
        TagValueEscapeLf = 40 << 8,
        TagValueEscapeInvalid = 41 << 8,
        SourcePrefix = 42 << 8,
        SourcePrefixTarget = 43 << 8,
        SourcePrefixTargetPrefix = 44 << 8,
        SourcePrefixTargetPrefixPrefix = 45 << 8,
        SourcePrefixTargetPrefixSuffix = 46 << 8,
        SourcePrefixTargetPrefixTargetList = 47 << 8,
        SourcePrefixTargetSuffix = 48 << 8,
        SourcePrefixUsername = 49 << 8,
        SourcePrefixHostname = 50 << 8,
        Verb = 51 << 8,
        CommandName = 52 << 8,
        CommandCode = 53 << 8,
        Params = 54 << 8,
        ParamsPrefix = 55 << 8,
        ParamsSuffix = 56 << 8,
        Middle = 57 << 8,
        MiddlePrefixList = 58 << 8,
        MiddleSuffix = 59 << 8,
        MiddlePrefixListFormatBase = 60 << 8,
        MiddlePrefixWithColonList = 61 << 8,
        Trailing = 62 << 8,
        TrailingPrefix = 63 << 8,
        TrailingList = 64 << 8,
        TrailingListPrefix = 65 << 8,
        TrailingListSuffix = 66 << 8,
        UTF8WithoutNullCrLfSemiColonSpace = 67 << 8,
        MiddlePrefixListTerminals = 68 << 8,
        MiddlePrefixListFormatBaseTerminals = 69 << 8,
        CrLf = 70 << 8,
        ModeStringList = 71 << 8,
        ModeString = 72 << 8,
        ModeChars = 73 << 8,
        ModeCharsList = 74 << 8,
        Mode = 75 << 8,
        KeyList = 76 << 8,
        KeyListSuffix = 77 << 8,
        KeyListItems = 78 << 8,
        KeyListItem = 79 << 8,
        Key = 80 << 8,
        NicknameSpaceList = 81 << 8,
        NicknameSpaceListSuffix = 82 << 8,
        NicknameSpaceListItems = 83 << 8,
        ChannelCommaList = 84 << 8,
        ChannelCommaListSuffix = 85 << 8,
        ChannelCommaListItems = 86 << 8,
        ElistCondList = 87 << 8,
        ElistCondListSuffix = 88 << 8,
        ElistCondListItems = 89 << 8,
        ElistCond = 90 << 8,
        MsgTarget = 91 << 8,
        MsgTargetSuffix = 92 << 8,
        MsgTargetItems = 93 << 8,
        MsgTo = 94 << 8,
        MsgToTargetMask = 95 << 8,
        MsgToTargetMaskLetters = 96 << 8,
        MsgToNickname = 97 << 8,
        MsgToNicknameSuffix = 98 << 8,
        MsgToUserHost = 99 << 8,
        MsgToUserHostServer = 100 << 8,
        MsgToChannel = 101 << 8,
        MsgToChannelPrefix = 102 << 8,
        MsgToChannelPrefixChannelPrefixSuffix = 103 << 8,
        MsgToChannelChannelPrefixMembership = 104 << 8,
        ChannelMembershipPrefixWithoutChannelPrefix = 105 << 8,
        ChannelMembershipPrefix = 106 << 8,
        MyInfoReply = 107 << 8,
        MyInfoReplySuffix = 108 << 8,
        Version = 109 << 8,
        ISupport = 110 << 8,
        ISupportSuffix = 111 << 8,
        ISupportList = 112 << 8,
        ISupportToken = 113 << 8,
        ISupportTokenNegated = 114 << 8,
        ISupportTokenSuffix = 115 << 8,
        ISupportParameter = 116 << 8,
        ISupportValue = 117 << 8,
        ISupportValueSuffix = 118 << 8,
        ISupportValueList = 119 << 8,
        ISupportValueItem = 120 << 8,
        ISupportValueItemSuffix = 121 << 8,
        ISupportValueItemSuffixValue = 122 << 8,
        ISupportValueItemTerminals = 123 << 8,
        ISupportValueItemEscape = 124 << 8,
        ISupportValueItemEscapeSuffix = 125 << 8,
        ISupportValueItemEscapeBackslash = 126 << 8,
        ISupportValueItemEscapeSpace = 127 << 8,
        ISupportValueItemEscapeEqual = 128 << 8,
        CapList = 129 << 8,
        CapListSuffix = 130 << 8,
        CapListItems = 131 << 8,
        CapListItem = 132 << 8,
        CapListItemSuffix = 134 << 8,
        CapListItemKey = 135 << 8,
        CapListItemKeySuffix = 136 << 8,
        CapListItemValueList = 137 << 8,
        CapListItemValueListSuffix = 138 << 8,
        CapListItemValueListItems = 139 << 8,
        CapListItemValueListItem = 140 << 8,
        NameReply = 141 << 8,
        NameReplyChannelType = 142 << 8,
        NicknameMembershipSpaceList = 143 << 8,
        NicknameMembershipSpaceListSuffix = 144 << 8,
        NicknameMembershipSpaceListItems = 145 << 8,
        NicknameMembership = 146 << 8,
        ChannelMembershipSpaceList = 147 << 8,
        ChannelMembershipSpaceListSuffix = 148 << 8,
        ChannelMembershipSpaceListItems = 149 << 8,
        UserHostList = 150 << 8,
        UserHostListSuffix = 151 << 8,
        UserHostListItems = 152 << 8,
        UserHostListItem = 153 << 8,
        UserHostListOp = 154 << 8,
        UserHostListAway = 155 << 8,
        UserHostListHostname = 156 << 8,
        UserHostListHostnamePrefix = 157 << 8,
        UserHostListHostnameSuffix = 158 << 8,
        WhoReply = 159 << 8,
        WhoReplyPrefix = 160 << 8,
        WhoReplyFlags = 161 << 8,
        WhoReplyAway = 162 << 8,
        WhoReplyFlagsOp = 163 << 8,
        WhoReplyChannelMembership = 164 << 8,
        WhoIsUserReply = 165 << 8,
        WhoIsRealName = 166 << 8,
        WhoIsServerReply = 167 << 8,
        ServerInfo = 168 << 8,
        WhoWasReply = 169 << 8,
        WhoIsIdleReply = 170 << 8,
        ListReply = 171 << 8,
        Topic = 172 << 8,
        CTCPMessage = 173 << 8,
        CTCPCommand = 174 << 8,
        CTCPMiddle = 175 << 8,
        CTCPParams = 176 << 8,
        CTCPParamsSuffix = 177 << 8,
        CTCPParamsMiddle = 178 << 8,
        CTCPMessageSuffix = 179 << 8,
        DCCMessage = 180 << 8,
        DCCType = 181 << 8,
        DCCArgument = 182 << 8,
        DCCQuotedArgument = 183 << 8,
        DCCFilenameList = 184 << 8,
        DCCFilenameSpaceList = 185 << 8,
        DCCQuotedFilename = 186 << 8,
        //all formatting in 4th byte
        Format = 1 << 24,
        BoldFormat = 2 << 24,
        ItalicsFormat = 3 << 24,
        UnderlineFormat = 4 << 24,
        StrikethroughFormat = 5 << 24,
        MonospaceFormat = 6 << 24,
        ResetFormat = 7 << 24,
        ColorFormat = 8 << 24,
        ColorFormatSuffix = 9 << 24,
        ColorCombination = 10 << 24,
        ColorCombinationSuffix = 11 << 24,
        ForegroundColor = 12 << 24,
        BackgroundColor = 13 << 24,
        ColorNumber = 14 << 24,
        ColorSuffix = 15 << 24,
        HexDecimal = 16 << 24,
        HexColorTriplet = 17 << 24,
        HexColorFormat = 18 << 24,
        HexColorCombination = 19 << 24,
        HexColorCombinationSuffix = 20 << 24,
        ForegroundHexColor = 21 << 24,
        BackgroundHexColor = 22 << 24,
        //MSB
        ConstructedMessage = 1 << 31
    }
}
