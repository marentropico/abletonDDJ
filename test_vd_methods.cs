using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Reflection;

class Program {
    static void Main() {
        var methods = typeof(VirtualDevice).GetMethods();
        foreach(var m in methods) {
            Console.WriteLine(m.Name);
        }
    }
}