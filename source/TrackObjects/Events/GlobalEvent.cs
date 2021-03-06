﻿using ChartTools.IO;
using ChartTools.IO.Chart;
using System.Collections.Generic;
using System.Linq;

namespace ChartTools
{
    /// <summary>
    /// Event common to all instruments
    /// </summary>
    public class GlobalEvent : Event
    {
        /// <summary>
        /// <see cref="Event.EventTypeString"/> value for each <see cref="GlobalEventType"/>
        /// </summary>
        private static readonly Dictionary<GlobalEventType, string> globalTypesDictionary = new Dictionary<GlobalEventType, string>()
        {
            { GlobalEventType.PhraseStart, "phrase_start" },
            { GlobalEventType.PhraseEnd, "phrase_end" },
            { GlobalEventType.Lyric, "lyric" },
            { GlobalEventType.Idle, "idle" },
            { GlobalEventType.Play, "play" },
            { GlobalEventType.HalfTempo, "half_tempo" },
            { GlobalEventType.NormalTempo, "normal_tempo" },
            { GlobalEventType.Verse, "verse" },
            { GlobalEventType.Chorus, "chorus" },
            { GlobalEventType.End, "end" },
            { GlobalEventType.MusicStart, "music_start" },
            { GlobalEventType.Lighting, "lighting ()" },
            { GlobalEventType.LightingFlare, "lighting (flare)" },
            { GlobalEventType.LightingBlackout, "lighting (blackout)" },
            { GlobalEventType.LightingChase, "lighting (chase)" },
            { GlobalEventType.LightingStrobe, "lighting (strobe)" },
            { GlobalEventType.LightingColor1, "lighting (color1)" },
            { GlobalEventType.LightingColor2, "lighting (color2)" },
            { GlobalEventType.LightingSweep, "lighting (sweep)" },
            { GlobalEventType.CrowdLightersFast, "crowd_lighters_fast" },
            { GlobalEventType.CrowdLightersOff, "crowd_lighters_off" },
            { GlobalEventType.CrowdLightersSlow, "crowd_lighters_slow" },
            { GlobalEventType.CrowdHalfTempo, "crowd_half_tempo" },
            { GlobalEventType.CrowdNormalTempo, "crowd_normal_tempo" },
            { GlobalEventType.CrowdDoubleTempo, "crowd_double_tempo" },
            { GlobalEventType.BandJump, "band_jump" },
            { GlobalEventType.Section, "section" },
            { GlobalEventType.SyncHeadBang, "sync_head_bang" },
            { GlobalEventType.SyncWag, "sync_wag" }
        };

        /// <inheritdoc cref="Event.EventTypeString"/>
        public GlobalEventType EventType
        {
            get => globalTypesDictionary.ContainsValue(EventTypeString) ? globalTypesDictionary.First(pair => pair.Value == EventTypeString).Key : GlobalEventType.Unknown;
            set => EventTypeString = GetEventTypeString(value);
        }

        /// <summary>
        /// Determines if the <see cref="GlobalEvent"/> serves to display lyrics
        /// </summary>
        public bool IsLyricEvent
        {
            get
            {
                GlobalEventType type = EventType;
                return type is GlobalEventType.PhraseStart or GlobalEventType.Lyric or GlobalEventType.PhraseEnd;
            }
        }

        /// <summary>
        /// Additional data to modifiy the outcome of the event
        /// </summary>
        public string Argument
        {
            get => EventData?.Split(' ', 2, System.StringSplitOptions.RemoveEmptyEntries)[1];
            set
            {
                if (string.IsNullOrEmpty(EventData))
                {
                    EventData = $"Default {value}";
                    return;
                }

                string[] split = EventData.Split(' ', 2, System.StringSplitOptions.RemoveEmptyEntries);
                split[1] = value;

                EventData = string.Join(' ', split);
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="GlobalEvent"/>.
        /// </summary>
        /// <param name="position">Value of <see cref="TrackObject.Position"/></param>
        /// <param name="type">Value of <see cref="EventType"/></param>
        /// <param name="argument">Value of <see cref="Event.Argument"/></param>
        public GlobalEvent(uint position, GlobalEventType type, string argument = "") : this(position, GetEventTypeString(type), argument) { }
        /// <summary>
        /// Creates an instance of <see cref="GlobalEvent"/>.
        /// </summary>
        /// <param name="position">Value of <see cref="TrackObject.Position"/></param>
        /// <param name="type">Value of <see cref="EventTypeString"/></param>
        /// <param name="argument">Value of <see cref="Argument"/></param>
        public GlobalEvent(uint position, string type, string argument = "") : base(position, $"{type} {argument}") { }
        internal GlobalEvent(uint position, string eventData) : base(position, eventData) { } 
        /// <summary>
        /// Gets the string value to set <see cref="Event.EventTypeString"/>.
        /// </summary>
        /// <param name="type">Event type to get the string value of</param>
        internal static string GetEventTypeString(GlobalEventType type) => type == GlobalEventType.Unknown ? "Default" : globalTypesDictionary[type];

        /// <inheritdoc cref="ChartParser.ReadGlobalEvents(string)"/>
        public static IEnumerable<GlobalEvent> FromFile(string path)
        {
            try { return ExtensionHandler.Read(path, (".chart", ChartParser.ReadGlobalEvents)); }
            catch { throw; }
        }
        /// <inheritdoc cref="ChartParser.ReplaceGlobalEvents(string, IEnumerable{GlobalEvent})"/>
        public static void ToFile(string path, IEnumerable<GlobalEvent> events)
        {
            try { ExtensionHandler.Write(path, events, (".chart", ChartParser.ReplaceGlobalEvents)); }
            catch { throw; }
        }
    }
}