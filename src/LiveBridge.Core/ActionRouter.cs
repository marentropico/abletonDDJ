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
    private int _prevCrossfaderVal = 64;
    private readonly Dictionary<PhysicalControl, int> _lastEqValues = new();

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

        // Globais e Volumes (Crossfader controls Timeline + Horizontal Scroll Follow)
        if (ev.Control == PhysicalControl.Crossfader)
        {
            int diff = ev.Value - _prevCrossfaderVal;
            _prevCrossfaderVal = ev.Value;
            if (diff != 0)
            {
                // Mapeia o movimento do crossfader para rolar a tela horizontalmente
                KeyboardSimulator.ScrollHorizontal(diff * 2);
            }
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

        // Left Jog (Outer Ring) -> CC 40 (Relative Fine-Tuning de Timeline + Scroll Follow)
        if (ev.Control == PhysicalControl.JogWheel_Left)
        {
            int delta = ParseRelativeDelta(ev.Value);
            KeyboardSimulator.ScrollHorizontal(delta * 2);
            return new ResolvedAction(ActionType.MidiCC, "Jog_Left_Fine", MidiChannel: 16, MidiCC: 40, MidiValue: ev.Value);
        }

        // Right Jog -> Simula teclas + e - para Zoom do Arrangement
        if (ev.Control == PhysicalControl.JogWheel_Right)
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

        // Load Left (Toggle Foco Browser / Tracks)
        if (ev.Control == PhysicalControl.Load_Left)
        {
            if (ev.Value == 0) return null;
            _browserFocused = !_browserFocused;
            return new ResolvedAction(ActionType.MidiCC, "Focus_Toggle", MidiChannel: 16, MidiCC: 43, MidiValue: _browserFocused ? 127 : 0);
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

        // Beat FX Left / Right (Mover seleção de plugin na cadeia)
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

        // FxSelect (Criar Pistas)
        if (ev.Control == PhysicalControl.FxSelectDown || ev.Control == PhysicalControl.FxSelectUp)
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
            if (ev.IsShiftActive)
                KeyboardSimulator.SendLeft(); // Shift + Click -> Voltar pasta (Seta Esquerda)
            else
                KeyboardSimulator.SendEnter(); // Click -> Abrir pasta / Carregar (Enter)
            return null;
        }

        // Browse Encoder Turn (Universal Navigation)
        if (ev.Control == PhysicalControl.BrowseEncoder_Turn)
        {
            int delta = ParseRelativeDelta(ev.Value);

            // Envia setas virtuais para navegar nas pastas ou tracks
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++) KeyboardSimulator.SendDown();
            }
            else if (delta < 0)
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
            int note = 36 + padIndex; // C1 (36) to G1 (43)
            int channel = deck == "Left" ? 1 : 2;
            return new ResolvedAction(ActionType.MidiNote, $"Sampler_{deck}_{padIndex}", MidiChannel: channel, MidiNote: note, MidiValue: val);
        }

        // 2. Hot Cue Mode (Clip Launch on Track 1 for Left, Track 2 for Right)
        if (mode == "HotCue")
        {
            int cc = deck == "Left" ? 50 + padIndex : 60 + padIndex;
            int ccVal = isShift ? 1 : 127; // 1 = Stop/Delete, 127 = Launch
            return new ResolvedAction(ActionType.MidiCC, $"ClipLaunch_{deck}_{padIndex}", MidiChannel: 16, MidiCC: cc, MidiValue: val > 0 ? ccVal : 0);
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


