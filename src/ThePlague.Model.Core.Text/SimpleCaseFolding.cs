using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace ThePlague.Model.Core.Text
{
    internal delegate uint MapCodePoint(uint cp);

    internal static partial class SimpleCaseFolding
    {
        private const uint _BMPLevel1Max = 0xFF;
        private const uint _BMPLevel3Max = 0xFFFF;
        private const uint _SMPMax = 0x01FFFF;
        private const uint _SMPMask = 0x0F0000;
        private const uint _SMPPrefix = 0x010000;

        private static readonly byte[] _LeadingZeroLength;
        private static readonly MapCodePoint[] _MapCodePoint
            = new MapCodePoint[]
        {
            new MapCodePoint(MapError),
            new MapCodePoint(MapBMPBelowFF),
            new MapCodePoint(MapBMPBelowFFFF),
            new MapCodePoint(MapSMP),
            new MapCodePoint(ReturnCodePoint)
        };
        private static readonly MapCodePoint[] _MapSMP;

        static SimpleCaseFolding()
        {
            _LeadingZeroLength = new byte[32];

            int uintLength = 4;
            int currentLength = uintLength;
            for(int i = 0; i < uintLength * 8; i += 8) //size of uint in bits
            {
                for(int j = 0; j < 8; j++)
                {
                    _LeadingZeroLength[i + j] = (byte)currentLength;
                }

                currentLength--;
            }

            _MapSMP = new MapCodePoint[16];
            for(int i = 0; i < 16; i++)
            {
                if(i == 1)
                {
                    _MapSMP[i] = new MapCodePoint(MapSMPBelow01FFFF);
                }
                else
                {
                    _MapSMP[i] = new MapCodePoint(ReturnCodePoint);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountBytes(uint value)
             => _LeadingZeroLength[BitOperations.LeadingZeroCount(value)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MapError(uint cp)
            => throw new ArgumentOutOfRangeException
            (
                nameof(cp),
                "Can not map a 0 byte codepoint"
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint MapBMPBelowFF(uint cp)
        {
            fixed(ushort* l1 = _BMPMapBelowFF)
            {
                return *(l1 + cp);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint MapBMPBelowFFFF(uint cp)
        {
            uint v, ch;

            fixed(ushort* l1 = _BMPMapLevel1)
            {
                fixed(ushort* l3 = _BMPMapLevel3)
                {
                    v = *(l1 + (cp >> 8));
                    ch = *(l3 + (v + (cp & 0xFF)));
                }
            }

            return ch == 0 ? cp : ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint MapSMP(uint cp)
        {
            ref MapCodePoint mapCodePoint =
                ref MemoryMarshal.GetArrayDataReference(_MapSMP);

            //get 5th nibble, and use jump table accordingly
            MapCodePoint map =
                Unsafe.Add(ref mapCodePoint, (int)((cp & _SMPMask) >> 16));

            return map(cp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint MapSMPBelow01FFFF(uint cp)
        {
            uint v, ch;

            fixed(ushort* l1 = _SMPMapLevel1)
            {
                fixed(ushort* l3 = _SMPMapLevel3)
                {
                    v = *(l1 + ((ushort)cp >> 8));
                    ch = *(l3 + (v + (cp & 0xFF)));
                }
            }

            //if found, fill 3rd byte
            return ch == 0 ? cp : ch | _SMPPrefix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReturnCodePoint(uint cp)
            => cp;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SimpleCaseFold(uint cp)
            => SimpleCaseFold(cp, CountBytes(cp));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint SimpleCaseFold(uint cp, int byteCount)
        {
            ref MapCodePoint mapCodePoint =
                ref MemoryMarshal.GetArrayDataReference(_MapCodePoint);

            MapCodePoint map = Unsafe.Add(ref mapCodePoint, byteCount);
            return map(cp);
        }

        public static int CompareUsingSimpleCaseFolding
        (
            this Utf8String leftStr,
            Utf8String rightStr
        )
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

            return CompareUsingSimpleCaseFolding
            (
                leftStr.GetEnumerator(),
                leftStr.Length,
                rightStr.GetEnumerator(),
                rightStr.Length
            );
        }

        public static int CompareUsingSimpleCaseFolding
        (
            Utf8CodePointEnumerator leftEnumerator,
            int leftLength,
            Utf8CodePointEnumerator rightEnumerator,
            int rightLength
        )
        {
            //lengths should be equal for simple case folding
            if(leftLength > rightLength)
            {
                return 1;
            }
            else if(rightLength > leftLength)
            {
                return -1;
            }
            //if both are 0 length
            else if(rightLength == 0)
            {
                return 0;
            }

            ref MapCodePoint mapCodePoint =
                ref MemoryMarshal.GetArrayDataReference(_MapCodePoint);
            ref ushort bmpMapFF =
                ref MemoryMarshal.GetArrayDataReference(_BMPMapBelowFF);
            ref ushort bmpMapL1 =
                ref MemoryMarshal.GetArrayDataReference(_BMPMapLevel1);
            ref ushort bmpMapL3 =
                ref MemoryMarshal.GetArrayDataReference(_BMPMapLevel3);
            ref ushort smpMapL1 =
                ref MemoryMarshal.GetArrayDataReference(_SMPMapLevel1);
            ref ushort smpMapL3 =
                ref MemoryMarshal.GetArrayDataReference(_SMPMapLevel3);

            uint lCp, rCp, lCf, rCf;
            long compare;

            //advance 1
            leftEnumerator.MoveNext();
            rightEnumerator.MoveNext();

            //and get current code points
            lCp = leftEnumerator.Current;
            rCp = rightEnumerator.Current;

            while(true)
            {
                //check for all ASCII code points
                while(lCp <= _BMPLevel1Max
                    && rCp <= _BMPLevel1Max)
                {
                    lCf = MapBMPBelowFF(lCp, ref bmpMapFF);
                    rCf = MapBMPBelowFF(rCp, ref bmpMapFF);

                    compare = lCf - rCf;
                    if(compare == 0)
                    {
                        //if end has been reached, strings are equal! strings
                        //are the same length
                        if(!(leftEnumerator.MoveNext()
                            && rightEnumerator.MoveNext()))
                        {
                            return 0;
                        }

                        lCp = leftEnumerator.Current;
                        rCp = rightEnumerator.Current;

                        continue;
                    }

                    return (int)compare;
                }

                //check for all BMP code points
                while(lCp <= _BMPLevel3Max
                    && lCp > _BMPLevel1Max
                    && rCp <= _BMPLevel3Max
                    && rCp > _BMPLevel1Max)
                {
                    lCf = MapBMPBelowFFFF(lCp, ref bmpMapL1, ref bmpMapL3);
                    rCf = MapBMPBelowFFFF(rCp, ref bmpMapL1, ref bmpMapL3);

                    compare = lCf - rCf;
                    if(compare == 0)
                    {
                        //if end has been reached, strings are equal
                        if(!(leftEnumerator.MoveNext()
                            && rightEnumerator.MoveNext()))
                        {
                            return 0;
                        }

                        lCp = leftEnumerator.Current;
                        rCp = rightEnumerator.Current;

                        continue;
                    }

                    return (int)compare;
                }

                //check for a small subset of SMP
                while(lCp <= _SMPMax
                    && lCp > _BMPLevel3Max
                    && rCp <= _SMPMax
                    && rCp > _BMPLevel3Max)
                {
                    lCf = MapSMPBelow01FFFF(lCp, ref smpMapL1, ref smpMapL3);
                    rCf = MapSMPBelow01FFFF(rCp, ref smpMapL1, ref smpMapL3);

                    compare = lCf - rCf;
                    if(compare == 0)
                    {
                        //if end has been reached, strings are equal
                        if(!(leftEnumerator.MoveNext()
                            && rightEnumerator.MoveNext()))
                        {
                            return 0;
                        }

                        lCp = leftEnumerator.Current;
                        rCp = rightEnumerator.Current;

                        continue;
                    }

                    return (int)compare;
                }

                //check if other sequence follows, both code point should
                //atleast be in the same plane
                if((lCp <= _BMPLevel1Max
                        && rCp <= _BMPLevel1Max)
                   || (lCp <= _BMPLevel3Max
                        && rCp <= _BMPLevel3Max))
                {
                    continue;
                }

                //else compare
                compare = SimpleCaseFold(lCp) - SimpleCaseFold(rCp);

                if(compare == 0)
                {
                    //if end has been reached, strings are equal
                    if(!(leftEnumerator.MoveNext()
                        && rightEnumerator.MoveNext()))
                    {
                        return 0;
                    }

                    lCp = leftEnumerator.Current;
                    rCp = rightEnumerator.Current;

                    continue;
                }

                return (int)compare;
            }

            throw new InvalidOperationException("Comparison failed");
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MapBMPBelowFF
        (
            uint cp,
            ref ushort bmpMapBelowFF
        )
            => Unsafe.Add(ref bmpMapBelowFF, (int)cp);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MapBMPBelowFFFF
        (
            uint cp,
            ref ushort bmpMapL1,
            ref ushort bmpMapL3
        )
        {
            ushort v = Unsafe.Add(ref bmpMapL1, (ushort)cp >> 8);
            uint ch = Unsafe.Add(ref bmpMapL3, (int)(v + (cp & 0XFF)));

            return ch == 0 ? cp : ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MapSMPBelow01FFFF
        (
            uint cp,
            ref ushort smpMapL1,
            ref ushort smpMapL3
        )
        {
            ushort v = Unsafe.Add(ref smpMapL1, (ushort)cp >> 8);
            uint ch = Unsafe.Add(ref smpMapL3, (int)(v + (cp & 0XFF)));

            //if found, fill 3rd byte
            return ch == 0 ? cp : ch | _SMPPrefix;
        }

        internal static int GetHashCode(Utf8String str)
            => GetHashCode(str.GetEnumerator());

        internal static int GetHashCode
        (
            Utf8CodePointEnumerator enumerator
        )
        {
            ref MapCodePoint mapCodePoint =
                ref MemoryMarshal.GetArrayDataReference(_MapCodePoint);
            ref ushort bmpMapFF =
                ref MemoryMarshal.GetArrayDataReference(_BMPMapBelowFF);
            ref ushort bmpMapL1 =
                ref MemoryMarshal.GetArrayDataReference(_BMPMapLevel1);
            ref ushort bmpMapL3 =
                ref MemoryMarshal.GetArrayDataReference(_BMPMapLevel3);
            ref ushort smpMapL1 =
                ref MemoryMarshal.GetArrayDataReference(_SMPMapLevel1);
            ref ushort smpMapL3 =
                ref MemoryMarshal.GetArrayDataReference(_SMPMapLevel3);

            uint cp, cf;
            int length = 0;
            uint hash = MurmurHash._Seed;

            if(enumerator.MoveNext())
            {
                cp = enumerator.Current;
                length++;
            }
            else
            {
                MurmurHash.HashUInt(hash, 0);
                return (int)MurmurHash.FinalizeHash(hash, 0);
            }

            while(true)
            {
                //check for all ASCII code points
                while(cp <= _BMPLevel1Max)
                {
                    cf = MapBMPBelowFF(cp, ref bmpMapFF);
                    MurmurHash.HashUInt(hash, cf);

                    if(enumerator.MoveNext())
                    {
                        cp = enumerator.Current;
                        length++;
                    }
                    else
                    {
                        return (int)MurmurHash.FinalizeHash(hash, (uint)length);
                    }
                }

                //check for all BMP code points
                while(cp is <= _BMPLevel3Max
                      and > _BMPLevel1Max)
                {
                    cf = MapBMPBelowFFFF(cp, ref bmpMapL1, ref bmpMapL3);
                    MurmurHash.HashUInt(hash, cf);

                    if(enumerator.MoveNext())
                    {
                        cp = enumerator.Current;
                        length++;
                    }
                    else
                    {
                        return (int)MurmurHash.FinalizeHash(hash, (uint)length);
                    }
                }

                //check for a small subset of SMP codepoints
                while(cp is <= _SMPMax
                      and > _BMPLevel3Max)
                {
                    cf = MapSMPBelow01FFFF(cp, ref smpMapL1, ref smpMapL3);
                    MurmurHash.HashUInt(hash, cf);

                    if(enumerator.MoveNext())
                    {
                        cp = enumerator.Current;
                        length++;
                    }
                    else
                    {
                        return (int)MurmurHash.FinalizeHash(hash, (uint)length);
                    }
                }

                //fell trough previous loop
                if(cp is <= _BMPLevel1Max
                   or <= _BMPLevel3Max)
                {
                    continue;
                }

                //code point > _SMPMax
                MurmurHash.HashUInt(hash, cp);

                if(enumerator.MoveNext())
                {
                    cp = enumerator.Current;
                    length++;
                }
                else
                {
                    return (int)MurmurHash.FinalizeHash(hash, (uint)length);
                }
            }

            throw new InvalidOperationException("Hash validation failed");
        }
    }
}
