#r "C:\Users\User\.nuget\packages\melanchall.drywetmidi\7.1.1\lib\netstandard2.0\Melanchall.DryWetMidi.dll"
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Reflection;

class Program {
    static void Main() {
        Console.WriteLine("Properties:");
        foreach(var p in typeof(VirtualDevice).GetProperties()) Console.WriteLine(p.Name);
        Console.WriteLine("Methods:");
        foreach(var m in typeof(VirtualDevice).GetMethods()) Console.WriteLine(m.Name);
    }
}