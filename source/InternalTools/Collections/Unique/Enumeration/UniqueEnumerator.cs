﻿using ChartTools.SystemExtensions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ChartTools.Collections.Unique
{
    /// <summary>
    /// Enumerator where <typeparamref name="T"/> items are pulled from multiple enumerators and filtered to the ones considered unique by an <see cref="EqualityComparison{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of the enumerated items</typeparam>
    public class UniqueEnumerator<T> : IEnumerator<T>, IInitializable
    {
        /// <summary>
        /// Items that have previously been iterated, used for checking if a new item is unique
        /// </summary>
        private readonly LinkedList<T> returnedItems = new LinkedList<T>();
        /// <summary>
        /// Enumerators to pull items from
        /// </summary>
        private IEnumerator<T>[] Enumerators { get; }
        /// <summary>
        /// <see langword="true"/> for indexes where MoveNext previously returned <see langword="false"/>
        /// </summary>
        private bool[] endsReached;

        /// <summary>
        /// Function that determines if two items are the same
        /// </summary>
        private EqualityComparison<T> Comparison { get; }

        /// <inheritdoc/>
        public T Current { get; private set; }
        /// <inheritdoc/>
        object IEnumerator.Current => Current;
        /// <inheritdoc/>
        public bool Initialized { get; private set; }

        /// <summary>
        /// Creates an instance of <see cref="UniqueEnumerator{T}"/>.
        /// </summary>
        /// <param name="comparison">Function that determines if two items are the same</param>
        /// <param name="enumerators">Enumerators to pull items from</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public UniqueEnumerator(EqualityComparison<T> comparison, params IEnumerator<T>[] enumerators)
        {
            if (enumerators is null)
                throw new CommonExceptions.ParameterNullException("enumerators", 1);
            if (enumerators.Length == 0)
                throw new CommonExceptions.ParameterNullException("enumerators", 1);
            if (comparison is null)
                throw new ArgumentNullException("Comparison is null");

            Comparison = comparison;
            Enumerators = enumerators.Where(e => e is not null).ToArray();
            endsReached = new bool[Enumerators.Length];
        }

        ~UniqueEnumerator() => Dispose();

        /// <inheritdoc/>
        public bool MoveNext()
        {
            T current = default;
            int index = 0;

            Initialize();

            return SearchEnumerator();

            bool SearchEnumerator()
            {
                // Skip enumerator if ended
                if (endsReached[index])
                    if (++index == Enumerators.Length)
                        return false;

                IEnumerator<T> enumerator = Enumerators[index];

                try { current = enumerator.Current; }
                catch
                {
                    // If end reached, repeat with next enumerator
                    if (!enumerator.MoveNext())
                    {
                        // All enumerators have been searched
                        return ++index != Enumerators.Length && SearchEnumerator();
                    }
                }

                // Continue search with the same enumerator until it runs out of items or the item is not null or a unique item is found
                if (current is null || returnedItems.Any(i => Comparison(i, current)))
                    return enumerator.MoveNext() && SearchEnumerator();

                Current = current;
                returnedItems.AddLast(current);

                return true;
            }
        }
        /// <inheritdoc/>
        public void Reset()
        {
            // Reset every enumerator
            foreach (IEnumerator<T> enumerator in Enumerators)
                try { enumerator.Reset(); }
                catch { throw; }

            returnedItems.Clear();
            endsReached = default;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (IEnumerator<T> enumerator in Enumerators)
                enumerator.Dispose();
        }
        /// <inheritdoc>/
        public void Initialize()
        {
            if (!Initialized)
            {
                foreach (IEnumerator<T> enumerator in Enumerators)
                    enumerator.MoveNext();

                Initialized = true;
            }
        }
    }
}
