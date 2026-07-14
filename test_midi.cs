using Melanchall.DryWetMidi.Multimedia;
using System;

class Program {
    static void Main() {
        Console.WriteLine("Trying to get devices...");
        for(int i=0; i<10; i++) {
            try {
                var d = InputDevice.GetByIndex(i);
                Console.WriteLine(i + ": " + d.Name);
            } catch(Exception ex) {
                Console.WriteLine(i + ": Exception " + ex.Message);
            }
        }
    }
}