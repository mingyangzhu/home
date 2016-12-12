﻿// MIT License
// 
// Copyright (c) 2016 Wojciech Nagórski
//                    Michael DeMond
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;

namespace ExtendedXmlSerialization.Services
{
    /// <summary>
    /// Attribution: https://msdn.microsoft.com/en-us/library/system.runtime.serialization.objectmanager(v=vs.110).aspx
    /// </summary>
    public abstract class ObjectWalkerBase<TInput, TResult> : IEnumerable<TResult>, IEnumerator<TResult>
    {
        readonly private Stack<TInput> _remaining = new Stack<TInput>();

        readonly private ObjectIdGenerator _generator = new ObjectIdGenerator();

        protected ObjectWalkerBase(TInput root)
        {
            Schedule(root);
        }

        public IEnumerator<TResult> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Reset()
        {
            throw new NotSupportedException("Resetting the enumerator is not supported.");
        }

        public TResult Current { get; private set; }

        object IEnumerator.Current => Current;

        protected bool Schedule(TInput candidate)
        {
            if (candidate != null)
            {
                // Ask the ObjectIDManager if this object has been examined before.

                // If this object has been examined before, do not look at it again just return.
                var result = First(candidate);
                if (result)
                {
                    OnSchedule(candidate);
                }
                return result;
            }
            return false;
        }

        bool First(object candidate)
        {
            bool firstOccurrence;
            _generator.GetId(candidate, out firstOccurrence);
            return firstOccurrence;
        }

        protected abstract TResult Select(TInput input);

        private void OnSchedule(TInput candidate) => _remaining.Push(candidate);

        // protected abstract bool ContainsAdditional(TResult parameter);
        /*protected abstract IEnumerable<TResult> Yield(TInput input);
        protected abstract IEnumerable<TResult> Next(TInput input);*/

        // Advance to the next item in the enumeration.
        public bool MoveNext()
        {
            // If there are no more items to enumerate, return false.
            if (_remaining.Count != 0)
            {
                var input = _remaining.Pop();
                Current = Select(input);


                /*foreach (var result in Yield(input))
                {
                    
                }*/

                /*var @continue = ContainsAdditional(current);
                
                if (@continue)
                {
                    foreach (var item in Yield(input))
                    {
                        Schedule(item);
                    }
                    // The object does have field, schedule the object's instance fields to be enumerated.
                    /*foreach (
                        FieldInfo fi in Current.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    )
                    {
                        Schedule(fi.GetValue(Current));
                    }#1#
                }*/
                return true;
            }

            return false;
        }

        public virtual void Dispose() {}
    }
}