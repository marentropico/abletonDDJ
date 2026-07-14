using LiveBridge.Core.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Linq;

using BridgeMidiEvent = LiveBridge.Core.Models.MidiEvent;

namespace LiveBridge.Midi;

public class InputListener : IDisposable
{
    private InputDevice? _device;
    public event Action<BridgeMidiEvent>? OnRawMidiEvent;

    public void Start(string deviceName)
    {
        var devices = new List<InputDevice>();
        // Manually iterate to avoid DryWetMidi crash when a faulty driver is present
        for (int i = 0; i < 20; i++)
        {
            try {
                var d = InputDevice.GetByIndex(i);
                devices.Add(d);
            } catch (Exception) {
                // Ignore NODRIVER exceptions
            }
        }

        _device = devices.FirstOrDefault(d =>
            d.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase));

        if (_device == null)
        {
            Console.WriteLine("[InputListener] ERRO: Dispositivo '" + deviceName + "' nÃ£o encontrado.");
            Console.WriteLine("[InputListener] Dispositivos disponÃ­veis:");
            foreach (var d in devices)
                Console.WriteLine("  - " + d.Name);
            return;
        }

        _device.EventReceived += OnEventReceived;
        _device.StartEventsListening();
        Console.WriteLine("[InputListener] Escutando: " + _device.Name);
    }

    private void OnEventReceived(object? sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is NoteOnEvent noteOn)
        {
            var raw = new BridgeMidiEvent(
                Status: (byte)(0x90 | noteOn.Channel),
                Data1:  (byte)noteOn.NoteNumber,
                Data2:  (byte)noteOn.Velocity
            );
            OnRawMidiEvent?.Invoke(raw);
        }
        else if (e.Event is NoteOffEvent noteOff)
        {
            var raw = new BridgeMidiEvent(
                Status: (byte)(0x80 | noteOff.Channel),
                Data1:  (byte)noteOff.NoteNumber,
                Data2:  0
            );
            OnRawMidiEvent?.Invoke(raw);
        }
        else if (e.Event is ControlChangeEvent cc)
        {
            var raw = new BridgeMidiEvent(
                Status: (byte)(0xB0 | cc.Channel),
                Data1:  (byte)cc.ControlNumber,
                Data2:  (byte)cc.ControlValue
            );
            OnRawMidiEvent?.Invoke(raw);
        }
    }

    public void Dispose()
    {
        _device?.StopEventsListening();
        _device?.Dispose();
    }
}
