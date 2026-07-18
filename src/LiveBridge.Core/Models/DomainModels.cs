namespace LiveBridge.Core.Models;

/// <summary>
/// Representa um evento MIDI bruto capturado da controladora física.
/// </summary>
public record MidiEvent(byte Status, byte Data1, byte Data2);

/// <summary>
/// Identifica um controle físico da DDJ-400 de forma amigável.
/// </summary>
public enum PhysicalControl
{
    Unknown,

    // --- Deck Left ---
    Play_Left,
    Cue_Left,
    JogWheel_Left,
    Pad1_Left, Pad2_Left, Pad3_Left, Pad4_Left,
    Pad5_Left, Pad6_Left, Pad7_Left, Pad8_Left,
    EQ_High_Left, EQ_Mid_Left, EQ_Low_Left,
    Filter_Left,
    Volume_Left,
    Trim_Left,
    
    LoopIn_Left,
    LoopOut_Left,
    ReloopExit_Left,
    LoopCallLeft_Left,
    LoopCallRight_Left, Sync_Left,
    TempoSlider_Left,
    
    HotCueMode_Left,
    BeatLoopMode_Left,
    BeatJumpMode_Left,
    SamplerMode_Left,
    // Modos secundários (Shift + modo pad)
    KeyboardMode_Left,
    PadFX1_Left,
    PadFX2_Left,
    KeyShiftMode_Left,

    // --- Deck Right ---
    Play_Right,
    Cue_Right,
    JogWheel_Right,
    Pad1_Right, Pad2_Right, Pad3_Right, Pad4_Right,
    Pad5_Right, Pad6_Right, Pad7_Right, Pad8_Right,
    EQ_High_Right, EQ_Mid_Right, EQ_Low_Right,
    Filter_Right,
    Volume_Right,
    Trim_Right,

    LoopIn_Right,
    LoopOut_Right,
    ReloopExit_Right,
    LoopCallLeft_Right,
    LoopCallRight_Right, Sync_Right,
    TempoSlider_Right,

    HotCueMode_Right,
    BeatLoopMode_Right,
    BeatJumpMode_Right,
    SamplerMode_Right,
    // Modos secundários (Shift + modo pad)
    KeyboardMode_Right,
    PadFX1_Right,
    PadFX2_Right,
    KeyShiftMode_Right,

    // --- Mixer & Global ---
    Shift_Left,
    Shift_Right,
    BrowseEncoder_Turn,
    BrowseEncoder_Click,
    Load_Left,
    Load_Right,
    Crossfader,
    HeadphoneCue_Left,
    HeadphoneCue_Right,
    MasterCue,
    HeadphoneMixing, HeadphoneLevel, MasterLevel,

    // --- Beat FX ---
    BeatLeft,
    BeatRight,
    FxSelect,
    FxChannelSelect,
    LevelDepth,
    FxOnOff
}

/// <summary>
/// Representa um evento processado e identificado, pronto para o StateManager.
/// </summary>
public record ControlEvent(
    PhysicalControl Control,
    int Value,       // 0-127 para knobs/faders; 127=pressed, 0=released para botões
    bool IsShiftActive,
    string DeckMode  // Ex: "SessionBrowser", "Mixing", "Arrangement"
);

/// <summary>
/// Tipos de ação que o ActionRouter pode despachar.
/// </summary>
public enum ActionType
{
    MidiCC,            // Usa MidiCC e MidiValue
    MidiNote,          // Usa MidiNote e MidiValue
    RemoteScriptCmd,   // Envia um CC reservado que o Python Remote Script intercepta
    ModeChange,        // Muda o modo interno do StateManager (sem saída para Ableton)
    LedFeedback,       // Acende ou apaga um LED na controladora física
}

/// <summary>
/// Ação resolvida que o OutputEmitter irá executar.
/// </summary>
public record ResolvedAction(
    ActionType Type,
    string Command,       // Ex: "Nav_Up", "Session_Record", "ModeChange:Mixing"
    int MidiChannel = 1,
    int MidiCC = -1,
    int MidiNote = -1,
    int MidiValue = 0
);

