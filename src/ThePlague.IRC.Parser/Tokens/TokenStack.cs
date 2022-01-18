// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace ThePlague.IRC.Parser.Tokens
{
    internal class TokenStack
    {
        private TokenAsValueType[] _array;
        private int _size;

        public TokenStack(int size)
        {
            this._array = new TokenAsValueType[size];
            this._size = 0;
        }

        public int Count => this._size;

        public bool TryPop(out Token result)
        {
            int size = this._size - 1;
            TokenAsValueType[] array = this._array;

            if((uint)size >= (uint)array.Length)
            {
                result = default;
                return false;
            }

            this._size = size;
            result = array[size];
            array[size] = default;
            return true;
        }

        // Pushes an item to the top of the stack.
        public void Push(Token item)
        {
            int size = this._size;
            TokenAsValueType[] array = this._array;

            if((uint)size < (uint)array.Length)
            {
                array[size] = item;
                this._size = size + 1;
            }
            else
            {
                this.PushWithResize(item);
            }
        }

        // Non-inline from Stack.Push to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PushWithResize(Token item)
        {
            Array.Resize(ref this._array, 2 * this._array.Length);
            this._array[this._size] = item;
            this._size++;
        }

        /// <summary>
        /// A simple struct we wrap reference types inside when storing in arrays to
        /// bypass the CLR's covariant checks when writing to arrays.
        /// </summary>
        /// <remarks>
        /// We use <see cref="TokenAsValueType"/> as a wrapper to avoid paying the cost of covariant checks whenever
        /// the underlying array that the <see cref="TokenStack"/> class uses is written to.
        /// We've recognized this as a perf win in ETL traces for these stack frames:
        /// clr!JIT_Stelem_Ref
        ///   clr!ArrayStoreCheck
        ///     clr!ObjIsInstanceOf
        /// </remarks>
        private readonly struct TokenAsValueType
        {
            private readonly Token _value;
            private TokenAsValueType(Token value)
            {
                this._value = value;
            }
            public static implicit operator TokenAsValueType(Token s) => new TokenAsValueType(s);
            public static implicit operator Token(TokenAsValueType s) => s._value;
        }
    }
}
