using System;
using System.Buffers;
using System.Text;

namespace ThePlague.Model.Core.Text
{
    public class Utf8String : IEquatable<Utf8String>
    {
        public int Length => (int)this._Buffer.Length;

        internal readonly ReadOnlySequence<byte> _Buffer;

        private static Encoding _Utf8Encoding;

        [ThreadStatic]
        private static Encoder _Encoder;

        [ThreadStatic]
        private static Decoder _Decoder;

        static Utf8String()
        {
            _Utf8Encoding = new UTF8Encoding(false, true);
        }

        public Utf8String(ReadOnlySequence<byte> str)
        {
            this._Buffer = str;
        }

        public Utf8String(ReadOnlyMemory<byte> str)
            : this(new ReadOnlySequence<byte>(str))
        { }

        public Utf8String(string str)
            : this(FromUtf16(str))
        { }

        public Utf8CodePointEnumerator GetEnumerator()
            => new Utf8CodePointEnumerator(this._Buffer);

        public override bool Equals(object obj)
        {
            if(obj is Utf8String str)
            {
                return Utf8String.Equals(this, str);
            }

            return false;
        }

#nullable enable
        public bool Equals(Utf8String? other)
            => Utf8String.Equals(this, other);

        public bool Equals(Utf8String? other, StringComparison stringComparison)
            => Utf8String.Equals(this, other, stringComparison);

        public static bool Equals(Utf8String? left, Utf8String? right)
            => Utf8StringComparer.Ordinal.Equals(left, right);

        public static int Compare
        (
            Utf8String? strA,
            Utf8String? strB,
            StringComparison stringComparison
        )
        {
            switch(stringComparison)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    throw new NotImplementedException();
                case StringComparison.Ordinal:
                    return Utf8OrdinalStringComparer
                        .Ordinal
                        .Compare(strA, strB);
                case StringComparison.OrdinalIgnoreCase:
                    return Utf8OrdinalStringComparer
                        .OrdinalIgnoreCase
                        .Compare(strA, strB);
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool Equals
        (
            Utf8String? strA,
            Utf8String? strB,
            StringComparison stringComparison
        )
        {
            switch(stringComparison)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.CurrentCultureIgnoreCase:
                case StringComparison.InvariantCulture:
                case StringComparison.InvariantCultureIgnoreCase:
                    throw new NotImplementedException();
                case StringComparison.Ordinal:
                    return Utf8OrdinalStringComparer
                        .Ordinal
                        .Equals(strA, strB);
                case StringComparison.OrdinalIgnoreCase:
                    return Utf8OrdinalStringComparer
                        .OrdinalIgnoreCase
                        .Equals(strA, strB);
                default:
                    throw new NotImplementedException();
            }
        }
#nullable disable

        public override int GetHashCode()
            => Utf8StringComparer.Ordinal.GetHashCode(this);

        public override string ToString()
            => (string)this;

        public static explicit operator string(Utf8String str)
            => new string(FromUtf8(str).Span);

        public static explicit operator Utf8String(string str)
            => new Utf8String(FromUtf16(str));

        public static ReadOnlyMemory<char> FromUtf8(Utf8String str)
        {
            Decoder decoder;
            if(_Decoder is null)
            {
                _Decoder = _Utf8Encoding.GetDecoder();
            }

            decoder = _Decoder;
            decoder.Reset();

            ReadOnlySequence<byte> sequence = str._Buffer;

            //should always be longer if multibyte
            char[] currentString = new char[sequence.Length];
            Memory<char> stringMemory, completeMemory
                = new Memory<char>(currentString);
            int totalStringLength = 0;
            bool completed = false;
            int charsUsed = 0;

            stringMemory = completeMemory;
            foreach(ReadOnlyMemory<byte> memory in sequence)
            {
                if(memory.Length == 0)
                {
                    continue;
                }

                unsafe
                {
                    fixed(char* c = stringMemory.Span)
                    {
                        fixed(byte* b = memory.Span)
                        {
                            decoder.Convert
                            (
                                b,
                                memory.Span.Length,
                                c,
                                stringMemory.Length,
                                false,
                                out int _,
                                out charsUsed,
                                out completed
                            );
                        }
                    }
                }

                if(completed)
                {
                    totalStringLength += charsUsed;
                    stringMemory = stringMemory.Slice(charsUsed);
                }
                else
                {
                    break;
                }
            }

            return completeMemory.Slice(0, totalStringLength);
        }

        public static ReadOnlyMemory<byte> FromUtf16(string str)
        {
            if(_Encoder is null)
            {
                _Encoder = _Utf8Encoding.GetEncoder();
            }

            _Encoder.Reset();

            ReadOnlySpan<char> chars = str.AsSpan();

            byte[] buf = new byte[_Encoder.GetByteCount(chars, true)];

            Memory<byte> utf8 = new Memory<byte>(buf);

            _Encoder.GetBytes(chars, utf8.Span, true);

            return utf8;
        }
    }
}
