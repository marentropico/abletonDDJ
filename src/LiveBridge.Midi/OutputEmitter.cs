using LiveBridge.Core.Models;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveBridge.Midi;

public class OutputEmitter : IDisposable
{
    
    private OutputDevice? _virtualPort;
    private OutputDevice? _ddj400Out;

    private static readonly Dictionary<string, int> CommandCcMap = new()
    {
        ["Transport_PlayToggle"] = 10,
        ["Transport_Continue"]   = 11,
        ["Transport_StopAll"]    = 12,
        ["Transport_Arrangement"]= 13,
        ["Nav_Up"]               = 80,
        ["Nav_Down"]             = 81,
        ["Nav_Enter"]            = 82,
        ["Nav_TabNext"]          = 83,
        ["Session_LaunchClip"]   = 84,
        ["Session_LaunchScene"]  = 85,
    };

    private OutputDevice? FindOutputDeviceSafe(string nameSubstring)
    {
        for (int i = 0; i < 20; i++)
        {
            try {
                var d = OutputDevice.GetByIndex(i);
                if (d.Name.Contains(nameSubstring, StringComparison.OrdinalIgnoreCase))
                    return d;
            } catch { }
        }
        return null;
    }

    public void Connect(string virtualPortName, string ddjOutputName)
    {
        _virtualPort = FindOutputDeviceSafe(virtualPortName);

        if (_virtualPort == null)
            Console.WriteLine("[OutputEmitter] AVISO: Porta virtual '" + virtualPortName + "' não encontrada. Rode o loopMIDI.");
        else
            Console.WriteLine("[OutputEmitter] Conectado à porta externa: " + _virtualPort.Name);

        _ddj400Out = FindOutputDeviceSafe(ddjOutputName);

        if (_ddj400Out == null)
            Console.WriteLine("[OutputEmitter] AVISO: SaÃ­da DDJ '" + ddjOutputName + "' nÃ£o encontrada. LEDs desabilitados.");
        else
            Console.WriteLine("[OutputEmitter] SaÃ­da DDJ-400: " + _ddj400Out.Name);
    }

    public void SendRawMidi(byte status, byte data1, byte data2)
    {
        if (_virtualPort == null) return;
        
        if ((status & 0xF0) == 0x90)
            _virtualPort.SendEvent(new NoteOnEvent((SevenBitNumber)data1, (SevenBitNumber)data2) { Channel = (FourBitNumber)(status & 0x0F) });
        else if ((status & 0xF0) == 0x80)
            _virtualPort.SendEvent(new NoteOffEvent((SevenBitNumber)data1, (SevenBitNumber)data2) { Channel = (FourBitNumber)(status & 0x0F) });
        else if ((status & 0xF0) == 0xB0)
            _virtualPort.SendEvent(new ControlChangeEvent((SevenBitNumber)data1, (SevenBitNumber)data2) { Channel = (FourBitNumber)(status & 0x0F) });
    }

    public void Emit(ResolvedAction action)
    {
        switch (action.Type)
        {
            case ActionType.MidiCC: EmitMidiCC(action); break;
            case ActionType.MidiNote: EmitMidiNote(action); break;
            case ActionType.RemoteScriptCmd: EmitRemoteScriptCommand(action.Command, action.MidiValue); break;
            case ActionType.LedFeedback: EmitLedFeedback(action); break;
            case ActionType.ModeChange: break;
        }
    }

    private void EmitMidiCC(ResolvedAction action)
    {
        if (_virtualPort == null) return;
        var cc = new ControlChangeEvent
        {
            Channel       = (FourBitNumber)(action.MidiChannel - 1),
            ControlNumber = (SevenBitNumber)action.MidiCC,
            ControlValue  = (SevenBitNumber)action.MidiValue
        };
        _virtualPort.SendEvent(cc);
    }

    private void EmitMidiNote(ResolvedAction action)
    {
        if (_virtualPort == null) return;
        Melanchall.DryWetMidi.Core.MidiEvent noteEvent = action.MidiValue > 0 
            ? new NoteOnEvent { Channel = (FourBitNumber)(action.MidiChannel - 1), NoteNumber = (SevenBitNumber)action.MidiNote, Velocity = (SevenBitNumber)action.MidiValue }
            : new NoteOffEvent { Channel = (FourBitNumber)(action.MidiChannel - 1), NoteNumber = (SevenBitNumber)action.MidiNote, Velocity = (SevenBitNumber)0 };
        _virtualPort.SendEvent(noteEvent);
    }

    private void EmitRemoteScriptCommand(string command, int value)
    {
        if (_virtualPort == null) return;
        if (!CommandCcMap.TryGetValue(command, out var ccNumber)) return;
        var cc = new ControlChangeEvent
        {
            Channel       = (FourBitNumber)15,
            ControlNumber = (SevenBitNumber)ccNumber,
            ControlValue  = (SevenBitNumber)(value > 0 ? 127 : 0)
        };
        _virtualPort.SendEvent(cc);
    }

    private void EmitLedFeedback(ResolvedAction action)
    {
        if (_ddj400Out == null) return;
        var note = new NoteOnEvent
        {
            Channel  = (FourBitNumber)(action.MidiChannel - 1),
            NoteNumber = (SevenBitNumber)action.MidiNote,
            Velocity   = (SevenBitNumber)action.MidiValue
        };
        _ddj400Out.SendEvent(note);
    }

    public void Dispose()
    {
        
        _virtualPort?.Dispose();
        _ddj400Out?.Dispose();
    }
}



