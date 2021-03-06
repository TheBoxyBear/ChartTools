﻿using ChartTools.Collections.Unique;
using ChartTools.IO.MIDI;

using System.Collections.Generic;
using System.Linq;

namespace ChartTools
{
    /// <summary>
    /// Set of tracks common to an instrument
    /// </summary>
    public class Instrument<TChord> : Instrument where TChord : Chord
    {
        /// <summary>
        /// Easy track
        /// </summary>
        public Track<TChord> Easy { get; set; } = null;
        /// <summary>
        /// Medium track
        /// </summary>
        public Track<TChord> Medium { get; set; } = null;
        /// <summary>
        /// Hard track
        /// </summary>
        public Track<TChord> Hard { get; set; } = null;
        /// <summary>
        /// Expert track
        /// </summary>
        public Track<TChord> Expert { get; set; } = null;

        /// <summary>
        /// Gets the <see cref="Track{TChord}"/> that matches a <see cref="Difficulty"/>
        /// </summary>
        public Track<TChord> GetTrack(Difficulty difficulty) => (Track<TChord>)GetType().GetProperty(difficulty.ToString()).GetValue(this);

        public void ShareLocalEvents(IO.MIDI.LocalEventSource source)
        {
            LocalEvent[] events = ((IEnumerable<LocalEvent>)(source switch
            {
                LocalEventSource.Easy => Easy?.LocalEvents,
                LocalEventSource.Medium => Medium?.LocalEvents,
                LocalEventSource.Hard => Hard?.LocalEvents,
                LocalEventSource.Expert => Expert?.LocalEvents,
                LocalEventSource.Merge => new UniqueEnumerable<LocalEvent>((e, other) => e.Equals(other), new Track<TChord>[] { Easy, Medium, Hard, Expert }.Select(t => t?.LocalEvents).ToArray()),
                _ => throw CommonExceptions.GetUndefinedException(source)
            })).ToArray();

            if (events.Length == 0)
                return;

            (Easy ??= new Track<TChord>()).LocalEvents = new List<LocalEvent>(events);
            (Medium ??= new Track<TChord>()).LocalEvents = new List<LocalEvent>(events);
            (Hard ??= new Track<TChord>()).LocalEvents = new List<LocalEvent>(events);
            (Expert ??= new Track<TChord>()).LocalEvents = new List<LocalEvent>(events);
        }
    }
}
