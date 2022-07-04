# NippyWard.IRC.Parser
A fast LL(1) recursive descent IRC parser written for .NET containing a high-performance UTF-8 implementation 

## Rationale
After learning parsing with [CNFDotnet](https://github.com/sebaFlame/CNFDotnet), I needed to test these newly learned skills. I decided on writing an IRC grammar and parser.

As IRC is built around UTF-8, I would need UTF-8 wrapper code: [NippyWard.Text](deps/NippyWard.Text).

This project was also written using [ViM](https://github.com/vim/vim) on [WSL1](https://docs.microsoft.com/en-us/windows/wsl/install).

## Installation
Clone the repository.

## Usage
The grammar can be found [here](data/ll1_irc_grammar.txt)

The parser is a single static method:
```C#
ReadOnlySequence<byte> buffer; //the input buffer

bool success = IRCParser.TryParse //returns true when a message was parsed
(
    in buffer, //input buffer
    out Token token, //the parsed message as a Token (linked list of Tokens) if success
    out SequencePosition examined //the examined position in the input buffer
);

//use the token
```
A Token is an AST of a single message. Every Token is linked to a single TokenType by which you can (de)construct every message.
For more examples, check [NippyWard.IRC.Parser.Tests](test/NippyWard.IRC.Parser.Tests).

A message (Token) can be constructed using a [GeneralIRCMessageFactory](src/NippyWard.IRC.Parser/GeneralIRCMessageFactory.cs).
```C#
using
(
    Token constructedMessage = this._factory
        .Reset() //reset the factory, it can be reused
        .Verb("foo") //set the verb
        .Parameter("bar") //set 1 or more parameters for the verb
        .Parameter("baz")
        .Parameter("asdf")
        .ConstructMessage() //construct the message
)
{
    // use the token
}
```
For more usage examples, check [UnparserTests](test/NippyWard.IRC.Parser.Tests/UnparserTests.cs).