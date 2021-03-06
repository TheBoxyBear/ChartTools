﻿using System;

namespace ChartTools
{
    /// <summary>
    /// Difficulty levels
    /// </summary>
    public enum Difficulty : byte { Easy, Medium, Hard, Expert }
    /// <summary>
    /// *Unsupported* Modifier that affects the way the chord can be played
    /// </summary>
    /// <remarks>Support will be added once accent notes are added to Clone Hero.</remarks>
    [Flags] public enum DrumsChordModifier : byte { None, Accent, Ghost }
    /// <summary>
    /// Drums pads and pedals for a <see cref="DrumsNote"/>
    /// </summary>
    public enum DrumsNotes : byte { Kick, Red, Yellow, Blue, Green4Lane_Orange5Lane, Green5Lane }
    /// <summary>
    /// Modifier that affects how a <see cref="GHLChord"/> can be played
    /// </summary>
    [Flags] public enum GHLChordModifier : byte { None, Forced, Tap }
    /// <summary>
    /// Guiter Hero Live instruments
    /// </summary>
    /// <remarks>Casting to <see cref="Instruments"/> will match the instrument.</remarks>
    public enum GHLInstrument : byte { Guitar = 1, Bass }
    /// <summary>
    /// Frets for a <see cref="GHLNote"/>
    /// </summary>
    public enum GHLNotes : byte { Open, Black1, Black2, Black3, White1, White2, White3 }
    /// <summary>
    /// Types of <see cref="GlobalEvent"/>
    /// </summary>
    public enum GlobalEventType : byte
    {
        Unknown,
        PhraseStart,
        PhraseEnd,
        Lyric,
        Idle,
        Play,
        HalfTempo,
        NormalTempo,
        Verse,
        Chorus,
        End,
        MusicStart,
        Lighting,
        LightingFlare,
        LightingBlackout,
        LightingChase,
        LightingStrobe,
        LightingColor1,
        LightingColor2,
        LightingSweep,
        CrowdLightersFast,
        CrowdLightersOff,
        CrowdLightersSlow,
        CrowdHalfTempo,
        CrowdNormalTempo,
        CrowdDoubleTempo,
        BandJump,
        Section,
        SyncHeadBang,
        SyncWag
    }
    /// <summary>
    /// Types of <see cref="LocalEvent"/>
    /// </summary>
    public enum LocalEventType : byte
    {
        Unknown,
        Solo,
        SoloEnd,
        GHL6,
        GHL6Forced,
        SoloOn,
        SoloOff,
        WailOn,
        WailOff,
        OwFaceOn,
        OwFaceOff
    }
    /// <summary>
    /// Modifier that affects how a <see cref="StandardChord"/> can be played
    /// </summary>
    [Flags] public enum StandardChordModifier : byte { None, Forced, Tap }
    /// <summary>
    /// Standard five-fret instruments
    /// </summary>
    /// <remarks><inheritdoc cref="GHLInstrument"/></remarks>
    public enum StandardInstrument : byte { LeadGuitar = 3, RhythmGuitar, CoopGuitar, Bass, Keys }
    /// <summary>
    /// Frets for a <see cref="StandardNote"/>
    /// </summary>
    public enum StandardNotes : byte { Open, Green, Red, Yellow, Blue, Orange }
    /// <summary>
    /// All instruments
    /// </summary>
    public enum Instruments : byte { Drums, GHLGuitar, GHLBass, LeadGuitar, RhythmGuitar, CoopGuitar, Bass, Keys }
}