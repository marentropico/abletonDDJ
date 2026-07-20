using System;
using System.Collections.Generic;
using LiveBridge.Core.Models;

namespace LiveBridge.Core;

/// <summary>
/// Resolve qual ResolvedAction corresponde a um ControlEvent.
/// </summary>
public class ActionRouter
{
    private readonly StateManager _state;
    private bool _browserFocused = false;
    private readonly Dictionary<PhysicalControl, int> _lastEqValues = new();
    
    // Jog Wheel Nudge Throttling
    private int _jogLeftAccumulator = 0;
    private DateTime _lastJogLeftTime = DateTime.MinValue;

    private int ParseRelativeDelta(int value)
    {
        if (value >= 40 && value <= 88) return value - 64; // Center-64 (e.g., 65 -> +1, 63 -> -1)
        if (value >= 1 && value <= 30) return value; // Two's Complement Positive (e.g., 1 -> +1)
        if (value >= 98 && value <= 127) return value - 128; // Two's Complement Negative (e.g., 127 -> -1)
        return 0;
    }

    private int ProcessEqShift(PhysicalControl control, int currentValue)
    {
        if (!_lastEqValues.TryGetValue(control, out int lastValue))
            lastValue = currentValue;
        
        int delta = currentValue - lastValue;
        _lastEqValues[control] = currentValue;

        if (delta == 0) return 0;
        // Converte o delta linear para 7-bit Two's Complement (esperado pelo EncoderElement do Ableton)
        if (delta > 0) return Math.Min(delta, 63);
        return 128 + Math.Max(delta, -64);
    }

    public ActionRouter(StateManager state)
    {
        _state = state;
    }

    public ResolvedAction? Resolve(ControlEvent ev)
    {
        // =========================================================
        // TASK 1: Transport Básico (Play / Cue) -> Via Python Remote Script
        // =========================================================
        
        if (ev.Control == PhysicalControl.Play_Left || ev.Control == PhysicalControl.Play_Right)
        {
            if (ev.Value == 0) return null;

            if (ev.IsShiftActive)
            {
                // Shift + Play -> Stop All Clips (Panic Button)
                return new ResolvedAction(ActionType.RemoteScriptCmd, "Transport_StopAll", MidiValue: 127);
            }
            else
            {
                // Play -> Toggle Play/Pause (Spacebar behavior)
                return new ResolvedAction(ActionType.RemoteScriptCmd, "Transport_PlayToggle", MidiValue: 127);
            }
        }

        if (ev.Control == PhysicalControl.Cue_Left || ev.Control == PhysicalControl.Cue_Right)
        {
            if (ev.Value == 0) return null;

            if (ev.IsShiftActive)
            {
                // Shift + Cue -> Return to Arrangement
                return new ResolvedAction(ActionType.RemoteScriptCmd, "Transport_Arrangement", MidiValue: 127);
            }
            else
            {
                // Cue -> Play from current playhead position
                return new ResolvedAction(ActionType.RemoteScriptCmd, "Transport_Continue", MidiValue: 127);
            }
        }

        // =========================================================
        // TASK 2: Mixer & EQs Analógicos (Faders e Knobs)
        // =========================================================

        // Globais e Volumes (Crossfader controls Timeline Playhead / Needle)
        if (ev.Control == PhysicalControl.Crossfader)
        {
            return new ResolvedAction(ActionType.MidiCC, "Mixer_Crossfader", MidiChannel: 16, MidiCC: 9, MidiValue: ev.Value);
        }

        // Wildcard LevelDepth Knob (Controla o parâmetro atualmente selecionado/focado)
        if (ev.Control == PhysicalControl.LevelDepth)
        {
            return new ResolvedAction(ActionType.MidiCC, "Wildcard_LevelDepth", MidiChannel: 16, MidiCC: 47, MidiValue: ev.Value);
        }

        // Reloop/Exit Left (Gravação contextual de slot de clip)
        if (ev.Control == PhysicalControl.ReloopExit_Left)
        {
            return new ResolvedAction(ActionType.MidiCC, "Slot_Record_Toggle", MidiChannel: 16, MidiCC: 48, MidiValue: ev.Value);
        }

        // Reloop/Exit Right (Gravação global de Arrangement)
        if (ev.Control == PhysicalControl.ReloopExit_Right)
        {
            return new ResolvedAction(ActionType.MidiCC, "Arrangement_Record_Toggle", MidiChannel: 16, MidiCC: 49, MidiValue: ev.Value);
        }

        // FX On/Off (Loop Selection helper - Press, Hold and Drag via Crossfader)
        if (ev.Control == PhysicalControl.FxOnOff)
        {
            if (ev.Value == 127) // Press FX On/Off
            {
                return new ResolvedAction(ActionType.MidiCC, "FxOnOff_Normal", MidiChannel: 16, MidiCC: 41, MidiValue: 127);
            }
            else // Release FX On/Off
            {
                var action = new ResolvedAction(ActionType.MidiCC, "FxOnOff_Normal", MidiChannel: 16, MidiCC: 41, MidiValue: 0);
                
                // Dispara Ctrl+Shift+L de forma assíncrona após 80ms
                System.Threading.Tasks.Task.Delay(80).ContinueWith(_ => {
                    KeyboardSimulator.SendSelectLoop();
                });
                
                return action;
            }
        }
        
        if (ev.Control == PhysicalControl.Volume_Left || ev.Control == PhysicalControl.Volume_Right)
        {
            // Faders desativados temporariamente devido a mau contato físico
            return null;
        }

        // =========================================================
        // Smart Knobs: EQs e Filters (Tratados agora no Python de forma nativa)
        // =========================================================

        bool isEq = (ev.Control >= PhysicalControl.EQ_High_Left && ev.Control <= PhysicalControl.Trim_Left) ||
                    (ev.Control >= PhysicalControl.EQ_High_Right && ev.Control <= PhysicalControl.Trim_Right);

        if (isEq)
        {
            if (!_lastEqValues.ContainsKey(ev.Control)) _lastEqValues[ev.Control] = ev.Value;

            if (ev.IsShiftActive)
            {
                // Modo Fine-Tuning: Calcula o delta e envia como Two's Complement
                int relativeValue = ProcessEqShift(ev.Control, ev.Value);
                if (relativeValue == 0) return null; // Sem movimento

                int baseCC = ev.Control switch {
                    PhysicalControl.EQ_High_Left => 52, PhysicalControl.EQ_Mid_Left => 53, PhysicalControl.EQ_Low_Left => 54, PhysicalControl.Filter_Left => 55,
                    PhysicalControl.EQ_High_Right => 62, PhysicalControl.EQ_Mid_Right => 63, PhysicalControl.EQ_Low_Right => 64, PhysicalControl.Filter_Right => 65,
                    _ => 0
                };
                if (baseCC > 0) return new ResolvedAction(ActionType.MidiCC, $"Mix_{ev.Control}_Shift", MidiChannel: 16, MidiCC: baseCC, MidiValue: relativeValue);
            }
            else
            {
                // Modo Absoluto (Takeover normal)
                _lastEqValues[ev.Control] = ev.Value; // Manter sync do last value
                
                int baseCC = ev.Control switch {
                    PhysicalControl.Trim_Left => 21, PhysicalControl.EQ_High_Left => 22, PhysicalControl.EQ_Mid_Left => 23, PhysicalControl.EQ_Low_Left => 24, PhysicalControl.Filter_Left => 25,
                    PhysicalControl.Trim_Right => 31, PhysicalControl.EQ_High_Right => 32, PhysicalControl.EQ_Mid_Right => 33, PhysicalControl.EQ_Low_Right => 34, PhysicalControl.Filter_Right => 35,
                    _ => 0
                };
                if (baseCC > 0) return new ResolvedAction(ActionType.MidiCC, $"Mix_{ev.Control}", MidiChannel: 16, MidiCC: baseCC, MidiValue: ev.Value);
            }
        }

        // =========================================================
        // WORKFLOW & JOGS & BROWSER (Simulação de teclado ou API direta)
        // =========================================================

        // Left Jog Turn -> Drag/Nudge Selection (Shift) or ScrollHorizontal
        if (ev.Control == PhysicalControl.JogWheel_Turn_Left)
        {
            int delta = ParseRelativeDelta(ev.Value);
            if (delta == 0) return null;

            if (ev.IsShiftActive)
            {
                // Drag (Nudge) com Throttling para não entupir o buffer do teclado
                _jogLeftAccumulator += delta;
                
                // Dispara no max a cada 50ms, acumulando pelo menos 3 ticks para reduzir sensibilidade
                if (Math.Abs(_jogLeftAccumulator) >= 3 && (DateTime.Now - _lastJogLeftTime).TotalMilliseconds > 50)
                {
                    if (_jogLeftAccumulator > 0) KeyboardSimulator.SendRight();
                    else KeyboardSimulator.SendLeft();
                    
                    _jogLeftAccumulator = 0;
                    _lastJogLeftTime = DateTime.Now;
                }
                return null;
            }
            else
            {
                KeyboardSimulator.ScrollHorizontal(delta * 2);
                return new ResolvedAction(ActionType.MidiCC, "Jog_Left_Fine", MidiChannel: 16, MidiCC: 40, MidiValue: ev.Value);
            }
        }
        
        if (ev.Control == PhysicalControl.JogWheel_Touch_Left)
        {
            return null;
        }

        // Right Jog Turn -> Simula teclas + e - para Zoom do Arrangement
        if (ev.Control == PhysicalControl.JogWheel_Turn_Right)
        {
            int delta = ParseRelativeDelta(ev.Value);

            if (delta < 0)
            {
                for (int i = 0; i < Math.Abs(delta); i++)
                    KeyboardSimulator.SendZoomIn();
            }
            else if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                    KeyboardSimulator.SendZoomOut();
            }
            return null;
        }
        
        if (ev.Control == PhysicalControl.JogWheel_Touch_Right)
        {
            return null;
        }

        // Load Left (Toggle Foco Browser / Tracks)
        if (ev.Control == PhysicalControl.Load_Left)
        {
            if (ev.Value == 0) return null;

            if (ev.IsShiftActive) 
            {
                // Shift + Load Left -> Focus Clip View via Python API (CC 51)
                return new ResolvedAction(ActionType.MidiCC, "Focus_Clip_View", MidiChannel: 16, MidiCC: 51, MidiValue: 127);
            }

            _browserFocused = !_browserFocused;

            if (_browserFocused)
            {
                // Após focar o browser, aguarda um curto tempo para o Ableton processar
                // e envia a Seta Direita para cair direto no Content Pane (lista de arquivos/efeitos)
                System.Threading.Tasks.Task.Delay(100).ContinueWith(_ => {
                    KeyboardSimulator.SendRight();
                });
            }

            // Enviamos 127 SEMPRE no press, pois o listener em Python reage a qualquer valor > 0.
            // Se enviássemos 0 na alternância, o Python ignoraria o sinal e exigiria dois cliques para funcionar.
            return new ResolvedAction(ActionType.MidiCC, "Focus_Toggle", MidiChannel: 16, MidiCC: 43, MidiValue: 127);
        }

        // Load Right (Toggle Session / Arrangement)
        if (ev.Control == PhysicalControl.Load_Right)
        {
            if (ev.Value == 0) return null;
            return new ResolvedAction(ActionType.MidiCC, "View_Toggle", MidiChannel: 16, MidiCC: 44, MidiValue: 127);
        }

        // Headphone Cue Left (Solo track ativa)
        if (ev.Control == PhysicalControl.HeadphoneCue_Left)
        {
            if (ev.Value == 0) return null;
            return new ResolvedAction(ActionType.MidiCC, "Track_Solo", MidiChannel: 16, MidiCC: 45, MidiValue: 127);
        }

        // Headphone Cue Right (Mute track ativa)
        if (ev.Control == PhysicalControl.HeadphoneCue_Right)
        {
            if (ev.Value == 0) return null;
            return new ResolvedAction(ActionType.MidiCC, "Track_Mute", MidiChannel: 16, MidiCC: 46, MidiValue: 127);
        }

        if (ev.Control == PhysicalControl.BeatLeft)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendLeft();
            return null;
        }

        if (ev.Control == PhysicalControl.BeatRight)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendRight();
            return null;
        }

        // Loop In Left (Duplicate selected clip)
        if (ev.Control == PhysicalControl.LoopIn_Left)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendDuplicate();
            return null;
        }

        // Loop Out Left (Delete selected clip)
        if (ev.Control == PhysicalControl.LoopOut_Left)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendDelete();
            return null;
        }
        
        // Loop In Right (Loop Selection - Ctrl+L)
        if (ev.Control == PhysicalControl.LoopIn_Right)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendLoopSelection();
            return null;
        }

        // Loop Out Right (Consolidate - Ctrl+J)
        if (ev.Control == PhysicalControl.LoopOut_Right)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendConsolidate();
            return null;
        }

        // Loop Call Left Left (Undo)
        if (ev.Control == PhysicalControl.LoopCallLeft_Left)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendUndo();
            return null;
        }

        // Loop Call Right Left (Redo)
        if (ev.Control == PhysicalControl.LoopCallRight_Left)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendRedo();
            return null;
        }

        // Loop Call Left Right (Narrow Grid - Ctrl+1)
        if (ev.Control == PhysicalControl.LoopCallLeft_Right)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendNarrowGrid();
            return null;
        }

        // Loop Call Right Right (Widen Grid - Ctrl+2)
        if (ev.Control == PhysicalControl.LoopCallRight_Right)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendWidenGrid();
            return null;
        }

        // Beat Sync Left (Quantize / Metronome)
        if (ev.Control == PhysicalControl.Sync_Left)
        {
            if (ev.Value == 0) return null; // Só no press
            if (ev.IsShiftActive)
            {
                KeyboardSimulator.SendMetronome();
            }
            else
            {
                KeyboardSimulator.SendQuantize();
            }
            return null;
        }

        // Beat Sync Right (Escape)
        if (ev.Control == PhysicalControl.Sync_Right)
        {
            if (ev.Value == 0) return null;
            KeyboardSimulator.SendEsc();
            return null;
        }

        // Headphone Mixing (Volume Master alternativo - CC 38)
        if (ev.Control == PhysicalControl.HeadphoneMixing)
        {
            return new ResolvedAction(ActionType.MidiCC, "Master_Volume_Alt", MidiChannel: 16, MidiCC: 38, MidiValue: ev.Value);
        }

        // Tempo Slider Left (BPM Control - CC 39)
        if (ev.Control == PhysicalControl.TempoSlider_Left)
        {
            return new ResolvedAction(ActionType.MidiCC, "BPM_Control", MidiChannel: 16, MidiCC: 39, MidiValue: 127 - ev.Value);
        }

        // FxSelect (Criar Pistas)
        if (ev.Control == PhysicalControl.FxSelect)
        {
            if (ev.Value == 0) return null;
            if (ev.IsShiftActive)
                KeyboardSimulator.SendCreateMidiTrack();
            else
                KeyboardSimulator.SendCreateAudioTrack();
            return null;
        }

        // Browse Encoder Click
        if (ev.Control == PhysicalControl.BrowseEncoder_Click && ev.Value == 127)
        {
            bool isShift = ev.IsShiftActive || KeyboardSimulator.IsShiftKeyDown();
            if (isShift)
            {
                // Temporariamente solta o Shift virtual para enviar apenas o sinal da Seta Esquerda (Voltar pasta)
                KeyboardSimulator.SendShiftUp();
                KeyboardSimulator.SendLeft();
                // Se o Shift do teclado ainda estiver fisicamente ativo, restabelece o Shift virtual
                if (KeyboardSimulator.IsShiftKeyDown() || ev.IsShiftActive)
                    KeyboardSimulator.SendShiftDown();
            }
            else
            {
                if (!_browserFocused)
                {
                    KeyboardSimulator.SendSplitClip();
                }
                else
                {
                    KeyboardSimulator.SendEnter();
                }
            }
            return null;
        }

        // Browse Encoder Turn (Universal Navigation / Seleciona verticalmente se Shift estiver fisicamente mantido)
        if (ev.Control == PhysicalControl.BrowseEncoder_Turn)
        {
            int delta = ParseRelativeDelta(ev.Value);

            // Envia setas virtuais para navegar nas pastas ou tracks
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++) KeyboardSimulator.SendDown();
            }
            else
            {
                for (int i = 0; i < Math.Abs(delta); i++) KeyboardSimulator.SendUp();
            }
            return null;
        }

        // =========================================================
        // PAD MODES SWITCHING
        // =========================================================
        if (ev.Control == PhysicalControl.HotCueMode_Left)    return new ResolvedAction(ActionType.ModeChange, "Left:HotCue");
        if (ev.Control == PhysicalControl.BeatLoopMode_Left)  return new ResolvedAction(ActionType.ModeChange, "Left:BeatLoop");
        if (ev.Control == PhysicalControl.BeatJumpMode_Left)  return new ResolvedAction(ActionType.ModeChange, "Left:BeatJump");
        if (ev.Control == PhysicalControl.SamplerMode_Left)   return new ResolvedAction(ActionType.ModeChange, "Left:Sampler");

        if (ev.Control == PhysicalControl.HotCueMode_Right)   return new ResolvedAction(ActionType.ModeChange, "Right:HotCue");
        if (ev.Control == PhysicalControl.BeatLoopMode_Right) return new ResolvedAction(ActionType.ModeChange, "Right:BeatLoop");
        if (ev.Control == PhysicalControl.BeatJumpMode_Right) return new ResolvedAction(ActionType.ModeChange, "Right:BeatJump");
        if (ev.Control == PhysicalControl.SamplerMode_Right)  return new ResolvedAction(ActionType.ModeChange, "Right:Sampler");

        // =========================================================
        // PERFORMANCE PADS ROUTING
        // =========================================================
        if (ev.Control >= PhysicalControl.Pad1_Left && ev.Control <= PhysicalControl.Pad8_Left)
        {
            int padIndex = ev.Control - PhysicalControl.Pad1_Left;
            return RoutePad("Left", padIndex, ev.Value, ev.IsShiftActive, ev.DeckMode);
        }

        if (ev.Control >= PhysicalControl.Pad1_Right && ev.Control <= PhysicalControl.Pad8_Right)
        {
            int padIndex = ev.Control - PhysicalControl.Pad1_Right;
            return RoutePad("Right", padIndex, ev.Value, ev.IsShiftActive, ev.DeckMode);
        }

        Console.WriteLine($"[ActionRouter] {ev.Control} não implementado ainda.");
        return null;
    }

    private ResolvedAction? RoutePad(string deck, int padIndex, int val, bool isShift, string mode)
    {
        // 1. Sampler Mode (Public MIDI channel for Drum Racks: Ch 1 for Left, Ch 2 for Right)
        if (mode == "Sampler")
        {
            // Layout cromático: linha de cima = acidentes, linha de baixo = naturais
            // null = pad sem função (Mi e Si não têm sustenido; Pad 4/8 Direito = oitava)
            int?[] leftNotes  = { 1, 3, null, 6,  0, 2, 4,  5  }; // C#, D#, -, F#, C, D, E, F
            int?[] rightNotes = { 8, 10, null, null, 7, 9, 11, null }; // G#, A#, -, OctUp, G, A, B, OctDown

            // Pads 4 (índice 3) e 8 (índice 7) do Deck Direito controlam a oitava global
            if (deck == "Right")
            {
                if (padIndex == 3) { if (val > 0) _state.OctaveUp();   return null; }
                if (padIndex == 7) { if (val > 0) _state.OctaveDown(); return null; }
            }

            int?[] map = deck == "Left" ? leftNotes : rightNotes;
            int? semitone = map[padIndex];
            if (semitone == null) return null; // Pad sem função (Mi#, Si# ou oitava)

            int noteNumber = _state.CurrentOctave * 12 + semitone.Value;
            return new ResolvedAction(ActionType.MidiNote, $"Keyboard_{deck}_{padIndex}", MidiChannel: 1, MidiNote: noteNumber, MidiValue: val);
        }

        // 2. Hot Cue Mode — Teclado Cromático Linear (layout: linha de baixo → cima, esquerdo → direito)
        // Sequência: C, C#, D, D#, E, F, F#, G | G#, A, A#, B, C(+1), C#(+1), D(+1), D#(+1)
        // Nota: usa a mesma oitava definida no modo Sampler. Sem controle de oitava neste modo.
        if (mode == "HotCue")
        {
            // padIndex 4-7 = linha de baixo (Pads 5-8), padIndex 0-3 = linha de cima (Pads 1-4)
            int[] leftNotes  = {  4,  5,  6,  7,  0,  1,  2,  3 }; // E, F, F#, G, C, C#, D, D#
            int[] rightNotes = { 12, 13, 14, 15,  8,  9, 10, 11 }; // C+1, C#+1, D+1, D#+1, G#, A, A#, B

            int semitone   = deck == "Left" ? leftNotes[padIndex] : rightNotes[padIndex];
            int noteNumber = _state.CurrentOctave * 12 + semitone;
            return new ResolvedAction(ActionType.MidiNote, $"HotKey_{deck}_{padIndex}", MidiChannel: 1, MidiNote: noteNumber, MidiValue: val);
        }

        // 3. Beat Loop Mode (Dynamic native Looper control)
        if (mode == "BeatLoop")
        {
            int cc = deck == "Left" ? 70 + padIndex : 80 + padIndex;
            return new ResolvedAction(ActionType.MidiCC, $"Looper_{deck}_{padIndex}", MidiChannel: 16, MidiCC: cc, MidiValue: val);
        }

        // 4. Beat Jump Mode (Momentary macro FX control on selected track device)
        if (mode == "BeatJump")
        {
            int cc = deck == "Left" ? 90 + padIndex : 100 + padIndex;
            return new ResolvedAction(ActionType.MidiCC, $"DeviceFx_{deck}_{padIndex}", MidiChannel: 16, MidiCC: cc, MidiValue: val);
        }

        return null;
    }
}


