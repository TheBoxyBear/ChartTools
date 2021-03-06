﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ChartTools.SystemExtensions.Linq;

namespace ChartTools.Collections.Alternating
{
    /// <summary>
    /// Enumerator that yields <typeparamref name="T"/> items from a set of enumerators in order using a <typeparamref name="TKey"/> key
    /// </summary>
    /// <typeparam name="T">Type of the enumerated items</typeparam>
    /// <typeparam name="TKey">Type of the key used to determine the order</typeparam>
    public class OrderedAlternatingEnumerator<T, TKey> : IInitializable, IEnumerator<T> where TKey : IComparable<TKey>
    {
        private IEnumerator<T>[] Enumerators { get; }
        /// <summary>
        /// Method that retrieves the key from an item
        /// </summary>
        private Func<T, TKey> KeyGetter { get; }
        /// <inheritdoc/>
        public bool Initialized { get; private set; }

        /// <inheritdoc/>
        public T Current { get; private set; }
        /// <inheritdoc/>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Contains items if the last ManyMinBy call returned more than one index
        /// </summary>
        LinkedList<int> equalMins = null;
        /// <summary>
        /// <see langword="true"/> for indexes where MoveNext previously returned <see langword="false"/>
        /// </summary>
        bool[] endsReached;

        /// <summary>
        /// Creates a new instance of <see cref="OrderedAlternatingEnumerator{T, TKey}"/>.
        /// </summary>
        /// <param name="keyGetter">Method that retrieves the key from an item</param>
        /// <param name="enumerators">Enumerators to alternate between</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public OrderedAlternatingEnumerator(Func<T, TKey> keyGetter, params IEnumerator<T>[] enumerators)
        {
            if (keyGetter is null)
                throw new CommonExceptions.ParameterNullException("keyGetter", 0);
            if (enumerators is null)
                throw new CommonExceptions.ParameterNullException("enumerators", 1);
            if (enumerators.Length == 0)
                throw new ArgumentException("No enumerators provided.");

            Enumerators = enumerators.Where(e => e is not null).ToArray();
            KeyGetter = keyGetter;
            endsReached = new bool[Enumerators.Length];
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (IEnumerator<T> enumerator in Enumerators)
                enumerator.Dispose();
        }
        ~OrderedAlternatingEnumerator() => Dispose();

        /// <inheritdoc/>
        public bool MoveNext()
        {
            // Return remaining values if MinMaxBy returned multiple
            if (equalMins is not null && equalMins.Count > 0)
            {
                Current = Enumerators[equalMins.First.Value].Current;
                equalMins.RemoveFirst();

                return true;
            }

            T current = default;
            int index = 0;

            // Index of the enumerators with items yet to have been set as Current
            LinkedList<int> usableEnumerators = new LinkedList<int>();

            Initialize();

            if (!SearchEnumerator())
                return false;

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

                // Continue search with the same enumerator until it runs out of items or the item is not null
                if (current is null)
                    return enumerator.MoveNext() && SearchEnumerator();

                usableEnumerators.AddLast(index);
                return true;
            }

            // Get the index of the enumerators whose current item yields the smallest key
            equalMins = new LinkedList<int>(usableEnumerators.ManyMinBy(i => KeyGetter(Enumerators[i].Current)));

            IEnumerator<T> minEnumerator = Enumerators[equalMins.First.Value];

            Current = minEnumerator.Current;

            if (!minEnumerator.MoveNext())
                endsReached[equalMins.First.Value] = true;

            equalMins.RemoveFirst();

            return true;
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            if (!Initialized)
            {
                foreach (IEnumerator<T> enumerator in Enumerators)
                    enumerator.MoveNext();

                Initialized = true;
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            foreach (IEnumerator<T> enumerator in Enumerators)
                try { enumerator.Reset(); }
                catch { throw; }

            endsReached = default;
            Initialized = false;
        }
    }
}
