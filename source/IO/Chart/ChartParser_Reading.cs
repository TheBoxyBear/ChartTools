﻿using ChartTools.Lyrics;
using ChartTools.SystemExtensions;
using ChartTools.SystemExtensions.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ChartTools.IO.Chart
{
    /// <summary>
    /// Provides methods for reading chart files
    /// </summary>
    internal static partial class ChartParser
    {
        /// <summary>
        /// Reads a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Song"/> contianing all song data</returns>
        /// <param name="path">Path of the file to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Song ReadSong(string path)
        {
            string[] lines;

            try { lines = GetLines(path).ToArray(); }
            catch { throw; }

            Song song = new Song();
            Type songType = typeof(Song);

            // Add threads to read metadata, global events, synctrack and drums
            List<Task> tasks = new List<Task>()
            {
                Task.Run(() =>
                {
                    try { song.Metadata = GetMetadata(lines); }
                    catch { throw; }
                }),
                Task.Run(() =>
                {
                    try { song.GlobalEvents = GetGlobalEvents(lines).ToList(); }
                    catch { throw; }
                }),
                Task.Run(() =>
                {
                    try { song.SyncTrack = GetSyncTrack(lines); }
                    catch { throw; }
                }),
                Task.Run(() =>
                {
                    try { song.Drums = GetInstrument(lines, part => GetDrumsTrack(part), partNames[Instruments.Drums]); }
                    catch { throw; }
                })
            };

            // Add a thread to read each ghl instrument
            foreach (GHLInstrument instrument in EnumExtensions.GetValues<GHLInstrument>())
                tasks.Add(Task.Run(() => songType.GetProperty($"GHL{instrument}").SetValue(song, GetInstrument(lines, part => GetGHLTrack(part), partNames[(Instruments)instrument]))));
            // Add a thread to read each standard instrument
            foreach (StandardInstrument instrument in EnumExtensions.GetValues<StandardInstrument>())
                tasks.Add(Task.Run(() =>
                    songType.GetProperty(instrument.ToString()).SetValue(song, GetInstrument(lines, part => GetStandardTrack(part), partNames[(Instruments)instrument]))));

            foreach (Task task in tasks)
            {
                task.Wait();
                task.Dispose();
            }

            return song;
        }

        /// <summary>
        /// Reads an instrument from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Instrument"/> containing all data about the given instrument
        ///     <para><see langword="null"/> if the file contains no data for the given instrument</para>
        /// </returns>
        /// <param name="path">Path of the file to read</param>
        /// <param name="instrument">Instrument to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Instrument ReadInstrument(string path, Instruments instrument)
        {
            if (instrument == Instruments.Drums)
            {
                try { return ReadDrums(path); }
                catch { throw; }
            }
            if (Enum.IsDefined(typeof(GHLInstrument), instrument))
            {
                try { return ReadInstrument(path, (GHLInstrument)instrument); }
                catch { throw; }
            }
            if (Enum.IsDefined(typeof(StandardInstrument), instrument))
            {
                try { return ReadInstrument(path, (StandardInstrument)instrument); }
                catch { throw; }
            }

            throw CommonExceptions.GetUndefinedException(instrument);
        }

        /// <summary>
        /// Reads drums from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Instrument{TChord}"/> where TChord is <see cref="DrumsChord"/> containing all drums data
        ///     <para><see langword="null"/> if the file contains no drums data</para>
        /// </returns>
        /// <param name="path">Path of the file to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Instrument<DrumsChord> ReadDrums(string path)
        {
            try { return GetInstrument(GetLines(path).ToArray(), part => GetDrumsTrack(part), partNames[Instruments.Drums]); }
            catch { throw; }
        }
        /// <summary>
        /// Reads a Guitar Hero Live instrument from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Instrument{TChord}"/> where TChord is <see cref="GHLChord"/> containing all data about the given instrument
        ///     <para><see langword="null"/> if the file has no data for the given instrument</para>
        /// </returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Instrument<GHLChord> ReadInstrument(string path, GHLInstrument instrument)
        {
            try { return GetInstrument(GetLines(path).ToArray(), part => GetGHLTrack(part), partNames[(Instruments)instrument]); }
            catch { throw; }
        }
        /// <summary>
        /// Reads a standard instrument from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Instrument{TChord}"/> where TChord is <see cref="StandardChord"/> containing all data about the given instrument
        ///     <para><see langword="null"/> if the file contains no data for the given instrument</para>
        /// </returns>
        /// <param name="path">Path of the file to read</param>
        /// <param name="instrument">Instrument to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Instrument<StandardChord> ReadInstrument(string path, StandardInstrument instrument)
        {
            try { return GetInstrument(GetLines(path).ToArray(), part => GetStandardTrack(part), partNames[(Instruments)instrument]); }
            catch { throw; }
        }
        /// <summary>
        /// Gets all data for an instrument from the contents of a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Instrument{TChord}"/> containing all data about the given instrument
        ///     <para><see langword="null"/> if the file contains no data for the given instrument</para>
        /// </returns>
        /// <param name="lines">Lines in the file</param>
        /// <param name="getTrack">Function that retrieves the track from the lines</param>
        /// <param name="instrumentPartName">Part name of the instrument excluding the difficulty</param>
        /// <exception cref="FormatException"/>
        private static Instrument<TChord> GetInstrument<TChord>(string[] lines, Func<IEnumerable<string>, Track<TChord>> getTrack, string instrumentPartName) where TChord : Chord
        {
            Instrument<TChord> instrument = new Instrument<TChord>();
            Type instrumentType = typeof(Instrument<TChord>);
            Difficulty[] difficulties = EnumExtensions.GetValues<Difficulty>().ToArray();

            // Create threads to reach each difficulty and wait
            Task[] tasks = difficulties.Select(d => Task.Run(() =>
            {
                Track<TChord> track;
                string difficultyString = d.ToString();

                try { track = getTrack(GetPart(lines, $"{difficultyString}{instrumentPartName}")); }
                catch { throw; }

                // Find the property named after the difficulty and set its value to the created track
                if (track is not null)
                    instrumentType.GetProperty(difficultyString).SetValue(instrument, track);
            })).ToArray();

            foreach (Task task in tasks)
            {
                try { task.Wait(); }
                catch { throw; }

                task.Dispose();
            }

            return difficulties.Select(d => instrument.GetTrack(d)).All(t => t is null) ? null : instrument;
        }

        #region Tracks
        /// <summary>
        /// Reads a track from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Track"/> containing all data about the given track
        ///     <para><see langword="null"/> if the file contains no data for the given track</para>
        /// </returns>
        /// <param name="path">Path of the file to read</param>
        /// <param name="instrument">Instrument of the track to read</param>
        /// <param name="difficulty">Difficulty of the track to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Track ReadTrack(string path, Instruments instrument, Difficulty difficulty)
        {
            if (instrument == Instruments.Drums)
            {
                try { return ReadDrumsTrack(path, difficulty); }
                catch { throw; }
            }
            if (Enum.IsDefined(typeof(GHLInstrument), instrument))
            {
                try { return ReadTrack(path, (GHLInstrument)instrument, difficulty); }
                catch { throw; }
            }
            if (Enum.IsDefined(typeof(StandardInstrument), instrument))
            {
                try { return ReadTrack(path, (StandardInstrument)instrument, difficulty); }
                catch { throw; }
            }

            throw CommonExceptions.GetUndefinedException(instrument);
        }

        /// <summary>
        /// Reads a drums track from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChors is <see cref="DrumsChord"/> containing all drums data for the given difficulty
        ///     <para><see langword="null"/> if the file contians no drums data for the given difficulty</para>
        /// </returns>
        /// <param name="path">Path of the file to read</param>
        /// <param name="difficulty">Difficulty of the track to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Track<DrumsChord> ReadDrumsTrack(string path, Difficulty difficulty)
        {
            try { return GetDrumsTrack(GetLines(path), difficulty); }
            catch { throw; }
        }
        /// <summary>
        /// Gets a drums track from the contents of a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChord is <see cref="DrumsChord"/> containing all drums data for the given difficulty
        ///     <para><see langword="null"/> if the lines contain no drums data for the given difficulty</para>
        /// </returns>
        /// <param name="lines">Lines in the file</param>
        /// <param name="difficulty">Difficulty of the track</param>
        /// <exception cref="FormatException"/>
        public static Track<DrumsChord> GetDrumsTrack(IEnumerable<string> lines, Difficulty difficulty)
        {
            try { return GetDrumsTrack(GetPart(lines, GetFullPartName(Instruments.Drums, difficulty))); }
            catch { throw; }
        }
        /// <summary>
        /// Gets all data from a portion of a chart file containing a drums track.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChord is <see cref="DrumsChord"/> containing all data in the part
        ///     <para><see langword="null"/> if the part contains no data</para>
        /// </returns>
        /// <param name="part">Lines of the file belonging to the track</param>
        /// <exception cref="FormatException"/>
        public static Track<DrumsChord> GetDrumsTrack(IEnumerable<string> part)
        {
            try
            {
                return GetTrack<DrumsChord>(part, (track, chord, entry, data, newChord) =>
                {
                    // Body of noteCase in GetTrack call

                    // Find the parent chord or create it
                    if (chord is null)
                        chord = new DrumsChord(entry.Position);
                    else if (entry.Position != chord.Position)
                        chord = track.Chords.FirstOrDefault(c => c.Position == entry.Position, new DrumsChord(entry.Position), out newChord);
                    else
                        newChord = false;

                    // Note
                    if (data.NoteIndex < 5)
                        chord.Notes.Add(new DrumsNote((DrumsNotes)data.NoteIndex) { SustainLength = data.SustainLength });
                    // Cymbal
                    else if (data.NoteIndex is > 66 and < 69)
                    {
                        DrumsNote note = null;
                        // NoteIndex of the note to set as cymbal
                        byte seekedIndex = (byte)(data.NoteIndex - 63);

                        // Find matching note
                        note = chord.Notes.FirstOrDefault(n => n.NoteIndex == seekedIndex, null, out bool returnedDefault);

                        if (returnedDefault)
                            note.IsCymbal = true;
                        else
                        {
                            chord.Notes.Add(new DrumsNote((DrumsNotes)(seekedIndex + 1)) { IsCymbal = true, SustainLength = data.SustainLength });
                            returnedDefault = false;
                        }
                    }

                    if (newChord)
                        track.Chords.Add(chord);

                    // Instance gets lost if not returned back to GetTrack
                    return chord;
                });
            }
            catch { throw; }
        }
        /// <summary>
        /// Reads a Guitar Hero Live track from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChors is <see cref="GHLChord"/> containing all data for the given instrument and difficulty
        ///     <para><see langword="null"/> if the file contians no data for the given instrument and difficulty</para>
        /// </returns>
        /// <param name="path">Path of the file to read</param>
        /// <param name="instrument">Instrument of the track</param>
        /// <param name="difficulty">Difficulty of the track</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Track<GHLChord> ReadTrack(string path, GHLInstrument instrument, Difficulty difficulty)
        {
            try { return GetGHLTrack(GetLines(path), instrument, difficulty); }
            catch { throw; }
        }
        /// <summary>
        /// Gets a Guitar Hero Live track from the contents of a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChord is <see cref="GHLChord"/> containing all data for the given instrument and difficulty
        ///     <para><see langword="null"/> if the lines contain no data for the given instrument and difficulty</para>
        /// </returns>
        /// <param name="lines">Lines in the file</param>
        /// <param name="instrument">Instrument of the track</param>
        /// <param name="difficulty">Difficulty of the track</param>
        /// <exception cref="FormatException"/>
        private static Track<GHLChord> GetGHLTrack(IEnumerable<string> lines, GHLInstrument instrument, Difficulty difficulty)
        {
            try { return GetGHLTrack(GetPart(lines, GetFullPartName((Instruments)instrument, difficulty))); }
            catch { throw; }
        }
        /// <summary>
        /// Gets all data from a portion of a chart file containing a Guitar Hero Live track.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChord is <see cref="GHLChord"/> containing all data in the part
        ///     <para><see langword="null"/> if the part contains no data</para>
        /// </returns>
        /// <param name="part">Lines in the file belonging to the track</param>
        /// <exception cref="FormatException"/>
        private static Track<GHLChord> GetGHLTrack(IEnumerable<string> part)
        {
            try
            {
                return GetTrack<GHLChord>(part, (track, chord, entry, data, newChord) =>
                {
                    // Body of noteCase in GetTrack call

                    // Find the parent chord or create it
                    if (chord is null)
                        chord = new GHLChord(entry.Position);
                    else if (entry.Position != chord.Position)
                        chord = track.Chords.FirstOrDefault(c => c.Position == entry.Position, new GHLChord(entry.Position), out newChord);
                    else
                        newChord = false;

                    // White notes
                    if (data.NoteIndex < 3)
                        chord.Notes.Add(new GHLNote((GHLNotes)(data.NoteIndex + 4)) { SustainLength = data.SustainLength });
                    // Black 1 and 2
                    else if (data.NoteIndex < 5)
                        chord.Notes.Add(new GHLNote((GHLNotes)(data.NoteIndex - 2)) { SustainLength = data.SustainLength });
                    else
                        // Chord modifier or open note or black3
                        switch (data.NoteIndex)
                        {
                            case 5:
                                chord.Modifier |= GHLChordModifier.Forced;
                                break;
                            case 6:
                                chord.Modifier |= GHLChordModifier.Tap; ;
                                break;
                            case 7:
                                chord.Notes.Add(new GHLNote(GHLNotes.Open) { SustainLength = data.SustainLength });
                                break;
                            case 8:
                                chord.Notes.Add(new GHLNote(GHLNotes.Black3) { SustainLength = data.SustainLength });
                                break;
                        }

                    if (newChord)
                        track.Chords.Add(chord);

                    // Instance gets lost if not returned back to GetTrack
                    return chord;
                });
            }
            catch { throw; }
        }
        /// <summary>
        /// Reads a standard track from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChors is <see cref="StandardChord"/> containing all drums data for the given instrument and difficulty
        ///     <para><see langword="null"/> if the file contians no data for the given instrument and difficulty</para>
        /// </returns>
        /// <param name="path">Path of the file to read</param>
        /// <param name="instrument">Instrumnent of the track</param>
        /// <param name="difficulty">Difficulty of the track</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Track<StandardChord> ReadTrack(string path, StandardInstrument instrument, Difficulty difficulty)
        {
            try { return GetStandardTrack(GetLines(path).ToArray(), instrument, difficulty); }
            catch { throw; }
        }
        /// <summary>
        /// Gets a standard track from the contents of a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChord is <see cref="StandardChord"/> containing all data for the given instrument and difficulty
        ///     <para><see langword="null"/> if the lines contain no data for the given instrument and difficulty</para>
        /// </returns>
        /// <param name="lines">Liens in the file</param>
        /// <param name="instrument">Instrument of the track</param>
        /// <param name="difficulty">Difficulty of the track</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        private static Track<StandardChord> GetStandardTrack(string[] lines, StandardInstrument instrument, Difficulty difficulty)
        {
            try { return GetStandardTrack(GetPart(lines, GetFullPartName((Instruments)instrument, difficulty))); }
            catch { throw; }
        }
        /// <summary>
        /// Gets all data from a portion of a chart file containing a standard track.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChord is <see cref="StandardChord"/> containing all data in the part
        ///     <para><see langword="null"/> if the part contains no data</para>
        /// </returns>
        /// <param name="part">Lines in the file belonging to the track</param>
        /// <exception cref="FormatException"/>
        private static Track<StandardChord> GetStandardTrack(IEnumerable<string> part)
        {
            try
            {
                return GetTrack<StandardChord>(part, (track, chord, entry, data, newChord) =>
                {
                    // Body of noteCase in GetTrack call

                    // Find the parent chord or create it
                    if (chord is null)
                        chord = new StandardChord(entry.Position);
                    else if (entry.Position != chord.Position)
                        chord = track.Chords.FirstOrDefault(c => c.Position == entry.Position, new StandardChord(entry.Position), out newChord);
                    else
                        newChord = false;

                    // Note
                    if (data.NoteIndex < 5)
                        chord.Notes.Add(new StandardNote((StandardNotes)(data.NoteIndex + 1)) { SustainLength = data.SustainLength });
                    // Chord modifier or open notes
                    else
                        switch (data.NoteIndex)
                        {
                            case 5:
                                chord.Modifier |= StandardChordModifier.Forced;
                                break;
                            case 6:
                                chord.Modifier |= StandardChordModifier.Tap;
                                break;
                            case 7:
                                chord.Notes.Add(new StandardNote(StandardNotes.Open) { SustainLength = data.SustainLength });
                                break;
                        }

                    if (newChord)
                        track.Chords.Add(chord);

                    // Instance gets lost if not returned back to GetTrack
                    return chord;
                });
            }
            catch { throw; }
        }
        /// <summary>
        /// Gets a track from a portion of a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Track{TChord}"/> where TChord containing all data in the part
        ///     <para><see langword="null"/> if the part contains no data</para>
        /// </returns>
        /// <param name="part">Lines in the file belonging to the track</param>
        /// <param name="noteCase">Function that handles entries containing note data. Must return the same chord received as a parameter.</param>
        /// <exception cref="FormatException"/>
        private static Track<TChord> GetTrack<TChord>(IEnumerable<string> part, Func<Track<TChord>, TChord, TrackObjectEntry, NoteData, bool, TChord> noteCase) where TChord : Chord
        {
            Track<TChord> track = new Track<TChord>();

            TChord chord = null;
            bool newChord = true;

            foreach (string line in part)
            {
                TrackObjectEntry entry;

                try { entry = new TrackObjectEntry(line); }
                catch (Exception e) { throw GetLineException(line, e); }

                switch (entry.Type)
                {
                    // Local event
                    case "E":
                        string[] split = GetDataSplit(entry.Data.Trim('"'));
                        track.LocalEvents.Add(new LocalEvent(entry.Position, split.Length > 0 ? split[0] : string.Empty));
                        break;
                    // Note or chord modifier
                    case "N":
                        NoteData data;
                        try
                        {
                            data = new NoteData(entry.Data);
                            chord = noteCase(track, chord, entry, data, newChord);
                        }
                        catch (Exception e) { throw GetLineException(line, e); }

                        break;
                    // Star power
                    case "S":
                        try
                        {
                            split = GetDataSplit(entry.Data);

                            if (!uint.TryParse(split[1], out uint length))
                                throw new FormatException($"Cannot parse length \"{split[0]}\" to uint.");

                            track.StarPower.Add(new StarPowerPhrase(entry.Position, length));
                        }
                        catch (Exception e) { throw GetLineException(line, e); }
                        break;
                }
            }

            // Return null if no data
            byte emptyCount = 0;

            Type trackType = typeof(Track<TChord>);

            foreach (PropertyInfo pInfo in new string[] { "Chords", "LocalEvents", "StarPower" }.Select(s => trackType.GetProperty(s)))
                if (((ICollection<TrackObject>)pInfo.GetValue(track)).Count == 0)
                {
                    emptyCount++;
                    pInfo.SetValue(track, null);
                }

            return emptyCount == 3 ? null : track;
        }
        #endregion

        /// <summary>
        /// Splits the data of an entry.
        /// </summary>
        /// <param name="data">Data portion of a <see cref="TrackObjectEntry"/></param>
        private static string[] GetDataSplit(string data) => data.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        /// <summary>
        /// Generates an exception to throw when a line cannot be converted.
        /// </summary>
        /// <returns>Instance of <see cref="Exception"/> to throw</returns>
        /// <param name="line">Line that caused the excpetion</param>
        /// <param name="innerException">Exception caught when interpreting the line</param>
        private static Exception GetLineException(string line, Exception innerException) => new FormatException($"Line \"{line}\": {innerException.Message}", innerException);

        /// <summary>
        /// Reads the metadata from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Metadata"/> containing metadata from the file
        ///     <para>Null if the file contains no metadata</para>
        /// </returns>
        /// <param name="path">Path of the file to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static Metadata ReadMetadata(string path)
        {
            try { return GetMetadata(GetLines(path).ToArray()); }
            catch { throw; }
        }
        /// <summary>
        /// Gets the metadata from the contents of a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="Metadata"/> containing metadata from the file
        ///     <para>Null if the lines contain no metadata</para>
        /// </returns>
        /// <param name="lines">Lines in the file</param>
        /// <exception cref="FormatException"/>
        private static Metadata GetMetadata(string[] lines)
        {
            Metadata metadata = new Metadata();

            foreach (string line in GetPart(lines, "Song"))
            {
                ChartEntry entry;
                try { entry = new ChartEntry(line); }
                catch (Exception e) { throw GetLineException(line, e); }

                string data = entry.Data.Trim('"');

                switch (entry.Header)
                {
                    case "Name":
                        metadata.Title = data;
                        break;
                    case "Artist":
                        metadata.Artist = data;
                        break;
                    case "Charter":
                        metadata.Charter = new Charter() { Name = data };
                        break;
                    case "Album":
                        metadata.Album = data;
                        break;
                    case "Year":
                        try { metadata.Year = ushort.Parse(data.TrimStart(',')); }
                        catch (Exception e) { throw GetLineException(line, e); }
                        break;
                    case "Offset":
                        try { metadata.AudioOffset = int.Parse(entry.Data); }
                        catch (Exception e) { throw GetLineException(line, e); }
                        break;
                    case "Resolution":
                        try { metadata.Resolution = ushort.Parse(data); }
                        catch (Exception e) { throw GetLineException(line, e); }
                        break;
                    case "Difficulty":
                        try { metadata.Difficulty = byte.Parse(data); }
                        catch (Exception e) { throw GetLineException(line, e); }
                        break;
                    case "PreviewStart":
                        try { metadata.PreviewStart = uint.Parse(data); }
                        catch (Exception e) { throw GetLineException(line, e); }
                        break;
                    case "PreviewEnd":
                        try { metadata.PreviewEnd = uint.Parse(data); }
                        catch (Exception e) { throw GetLineException(line, e); }
                        break;
                    case "Genre":
                        metadata.Genre = data;
                        break;
                    case "MediaType":
                        metadata.MediaType = data;
                        break;
                    case "MusicStream":
                        metadata.Streams.Music = data;
                        break;
                    case "GuitarStream":
                        metadata.Streams.Guitar = data;
                        break;
                    case "BassStream":
                        metadata.Streams.Bass = data;
                        break;
                    case "RhythmStream":
                        metadata.Streams ??= new StreamCollection();
                        metadata.Streams.Rhythm = data;
                        break;
                    case "KeysStream":
                        metadata.Streams ??= new StreamCollection();
                        metadata.Streams.Keys = data;
                        break;
                    case "DrumStream":
                        metadata.Streams ??= new StreamCollection();
                        metadata.Streams.Drum = data;
                        break;
                    case "Drum2Stream":
                        metadata.Streams ??= new StreamCollection();
                        metadata.Streams.Drum2 = data;
                        break;
                    case "Drum3Stream":
                        metadata.Streams ??= new StreamCollection();
                        metadata.Streams.Drum3 = data;
                        break;
                    case "Drum4Stream":
                        metadata.Streams ??= new StreamCollection();
                        metadata.Streams.Drum4 = data;
                        break;
                    case "VocalStream":
                        metadata.Streams ??= new StreamCollection();
                        metadata.Streams.Vocal = data;
                        break;
                    case "CrowdStream":
                        metadata.Streams ??= new StreamCollection();
                        metadata.Streams.Crowd = data;
                        break;
                }
            }

            return metadata;
        }

        /// <summary>
        /// Reads the lyrics from a chart file.
        /// </summary>
        /// <returns>Enumerable of <see cref="Phrase"/> containing the lyrics from the file</returns>
        /// <param name="path">Path of the file to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static IEnumerable<Phrase> ReadLyrics(string path)
        {
            IEnumerable<GlobalEvent> events;

            try { events = ReadGlobalEvents(path); }
            catch { throw; }

            return events.GetLyrics();
        }

        /// <summary>
        /// Reads the global events from a chart file.
        /// </summary>
        /// <returns>Enumerable of <see cref="GlobalEvent"/></returns>
        /// <param name="path">Path of the file the read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static IEnumerable<GlobalEvent> ReadGlobalEvents(string path)
        {
            try { return GetGlobalEvents(GetLines(path).ToArray()); }
            catch { throw; }
        }
        /// <summary>
        /// Gets the global events from the contents of a chart file.
        /// </summary>
        /// <param name="lines">Lines in the file</param>
        /// <returns>Enumerable of <see cref="GlobalEvent"/></returns>
        /// <exception cref="FormatException"/>
        private static IEnumerable<GlobalEvent> GetGlobalEvents(string[] lines)
        {
            foreach (string line in GetPart(lines, "Events"))
            {
                TrackObjectEntry entry;
                try { entry = new TrackObjectEntry(line); }
                catch (Exception e) { throw GetLineException(line, e); }

                yield return new GlobalEvent(entry.Position, entry.Data.Trim('"'));
            }
        }

        /// <summary>
        /// Reads the sync track from a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="SyncTrack"/>
        ///     <para><see langword="null"/> if the file contains no sync track</para>
        /// </returns>
        /// <param name="path">Path of the file to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        public static SyncTrack ReadSyncTrack(string path)
        {
            try { return GetSyncTrack(GetLines(path).ToArray()); }
            catch { throw; }
        }
        /// <summary>
        /// Gets the sync track from the contents of a chart file.
        /// </summary>
        /// <returns>Instance of <see cref="SyncTrack"/>
        ///     <para><see langword="null"/> if the lines contain no sync track</para>
        /// </returns> 
        /// <param name="lines">Lines in the file</param>
        /// <exception cref="FormatException"/>
        private static SyncTrack GetSyncTrack(string[] lines)
        {
            SyncTrack syncTrack = new SyncTrack();

            foreach (string line in GetPart(lines, "SyncTrack"))
            {
                TrackObjectEntry entry;
                try { entry = new TrackObjectEntry(line); }
                catch (Exception e) { throw GetLineException(line, e); }

                Tempo marker;

                switch (entry.Type)
                {
                    // Time signature
                    case "TS":
                        string[] split = GetDataSplit(entry.Data);

                        byte denominator;

                        if (!byte.TryParse(split[0], out byte numerator))
                            throw new FormatException($"Cannot parse numerator \"{split[0]}\" to byte.");

                        // Denominator is only written if not equal to 4
                        if (split.Length < 2)
                            denominator = 4;
                        else
                        {
                            if (byte.TryParse(split[1], out denominator))
                                //Denominator is written as its second power
                                denominator = (byte)Math.Pow(2, denominator);
                            else
                                throw new FormatException($"Cannot parse denominator \"{split[1]}\" to byte.");
                        }

                        try { syncTrack.TimeSignatures.Add(new TimeSignature(entry.Position, numerator, denominator)); }
                        catch { throw; }
                        break;
                    // Tempo
                    case "B":
                        float value;

                        // Floats are written by ronding to the 3rd decimal and removing the decimal point
                        if (float.TryParse(entry.Data, out value))
                            value /= 1000;
                        else
                            throw new FormatException($"Cannot parse value \"{entry.Data}\" to float.");

                        // Find the marker matching the position in case it was already added through a mention of anchor
                        marker = syncTrack.Tempo.FirstOrDefault(m => m.Position == entry.Position);

                        if (marker is null)
                            try { syncTrack.Tempo.Add(new Tempo(entry.Position, value)); }
                            catch { throw; }
                        else
                            marker.Value = value;
                        break;
                    // Anchor
                    case "A":
                        float anchor;

                        // Floats are written by ronding to the 3rd decimal and removing the decimal point
                        if (float.TryParse(entry.Data, out anchor))
                            anchor /= 1000;
                        else
                            throw new FormatException($"Cannot parse value \"{entry.Data}\" to float.");

                        // Find the marker matching the position in case it was already added through a mention of value
                        marker = syncTrack.Tempo.FirstOrDefault(m => m.Position == entry.Position);

                        if (marker is null)
                            try { syncTrack.Tempo.Add(new Tempo(entry.Position, 0) { Anchor = anchor }); }
                            catch { throw; }
                        else
                            marker.Anchor = anchor;

                        break;
                }
            }

            // Return null if no data
            return syncTrack.TimeSignatures.Count == 0 && syncTrack.Tempo.Count == 0 ? null : syncTrack;
        }

        /// <summary>
        /// Gets the lines from a text file.
        /// </summary>
        /// <returns>Enumerable of all the lines in the file</returns>
        /// <param name="path">Path of the file to read</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="OutOfMemoryException"/>
        private static IEnumerable<string> GetLines(string path)
        {
            StreamReader reader;

            try { reader = new StreamReader(path); }
            catch { throw; }

            // Read to the end
            using (reader)
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line != string.Empty)
                        yield return line;
                }
        }
        /// <summary>
        /// Gets a part from the contents of a chart file
        /// </summary>
        /// <returns>Enumesable of all the lines in the part</returns>
        /// <param name="lines">Lines in the file</param>
        /// <param name="partName">Name of the part to extract</param>
        /// <exception cref="InvalidDataException"/>
        private static IEnumerable<string> GetPart(IEnumerable<string> lines, string partName)
        {
            using IEnumerator<string> enumerator = lines.GetEnumerator();
            enumerator.MoveNext();

            // Find part
            while (enumerator.Current != $"[{partName}]")
                if (!enumerator.MoveNext())
                    yield break;

            // Move past the part name and opening bracket
            for (int i = 0; i < 2; i++)
                if (!enumerator.MoveNext())
                    yield break;

            // Read until closing bracket
            while (enumerator.Current != "}")
            {
                yield return enumerator.Current;

                if (!enumerator.MoveNext())
                    throw new InvalidDataException($"Part \"{partName}\" did not end within the provided lines.");
            }
        }
    }
}
