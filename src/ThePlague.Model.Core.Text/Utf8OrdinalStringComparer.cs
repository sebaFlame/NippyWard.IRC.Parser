using System;
using System.Diagnostics.CodeAnalysis;
using System.Buffers;

namespace ThePlague.Model.Core.Text
{
    public class Utf8OrdinalStringComparer : Utf8StringComparer
    {
        public static Utf8OrdinalStringComparer Instance => new();

        public override int Compare(Utf8String leftStr, Utf8String rightStr)
        {
            if(object.ReferenceEquals(leftStr, rightStr))
            {
                return 0;
            }

            if(leftStr is null)
            {
                return -1;
            }

            if(rightStr is null)
            {
                return 1;
            }

            int leftLength = leftStr.Length, rightLength = rightStr.Length;

            //lengths should be equal for ordinal compare
            if(leftLength != rightLength)
            {
                return leftLength - rightLength;
            }
            //if both are 0 length
            else if(rightLength == 0)
            {
                return 0;
            }

            uint lCp, rCp;
            Utf8CodePointEnumerator leftEnumerator = leftStr.GetEnumerator(),
                rightEnumerator = rightStr.GetEnumerator();
            long compare;

            while(leftEnumerator.MoveNext()
                && rightEnumerator.MoveNext())
            {
                //and get current code points
                lCp = leftEnumerator.Current;
                rCp = rightEnumerator.Current;

                compare = lCp - rCp;

                if(compare == 0)
                {
                    continue;
                }
                else
                {
                    return (int)compare;
                }
            }

            return 0;
        }

        public override bool Equals(Utf8String leftStr, Utf8String rightStr)
        {
            if(object.ReferenceEquals(leftStr, rightStr))
            {
                return true;
            }

            if(leftStr is null
               || rightStr is null)
            {
                return false;
            }

            if(leftStr.Length != rightStr.Length)
            {
                return false;
            }

            ReadOnlySequence<byte>.Enumerator leftEnumerator, rightEnumerator;
            ReadOnlySpan<byte> lSpan, rSpan, cl, cr;
            int length = leftStr.Length;
            int minLength;

            leftEnumerator = leftStr._Buffer.GetEnumerator();
            rightEnumerator = rightStr._Buffer.GetEnumerator();

            lSpan = rSpan = Span<byte>.Empty;

            //also checks if length is 0 (both)
            while(length > 0)
            {
                if(lSpan.Length == 0)
                {
                    if(leftEnumerator.MoveNext())
                    {
                        lSpan = leftEnumerator.Current.Span;
                    }
                    else
                    {
                        return false;
                    }
                }

                if(rSpan.Length == 0)
                {
                    if(rightEnumerator.MoveNext())
                    {
                        rSpan = rightEnumerator.Current.Span;
                    }
                    else
                    {
                        return false;
                    }
                }

                minLength = Math.Min(lSpan.Length, rSpan.Length);

                cl = lSpan.Slice(0, minLength);
                cr = rSpan.Slice(0, minLength);

                if(!cl.SequenceEqual(cr))
                {
                    return false;
                }

                lSpan = lSpan.Slice(minLength);
                rSpan = rSpan.Slice(minLength);

                length -= minLength;
            }

            //went through all bytes, strings are equal
            return true;
        }

        public override int GetHashCode([DisallowNull] Utf8String obj)
            => (int)MurmurHash.Hash_x86_32(obj._Buffer);
    }
}
