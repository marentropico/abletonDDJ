using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LiveBridge.Core.Models;
using LiveBridge.Core;
using LiveBridge.Midi;

namespace LiveBridge.App;

public record CalibrationControl(
    string Name,
    string Description,
    bool IsContinuous,
    bool IsShiftCombo
);

// --- Noise gate: CC51 no canal 1 (fader esquerdo com mau contato) ---
file static class HardwareNoise
{
    public static bool IsNoise(byte status, byte data1, string step) =>
        status == 176 && data1 == 51 && step != "VolumeSlider_Left" && step != "Filter_Left";
}

public class Program
{
    private static string GetDocsDir()
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "docs")))
                return Path.Combine(dir, "docs");
            dir = Directory.GetParent(dir)?.FullName;
        }
        return "docs";
    }

    private static readonly string DOCS_DIR    = GetDocsDir();
    private static readonly string JSON_PATH   = Path.Combine(DOCS_DIR, "mappings_calibrated.json");
    private const string DDJ_IN      = "DDJ-400";
    private const string DDJ_OUT     = "DDJ-400";
    private const string VIRTUAL_OUT = "loopMIDI Port";

    private static Dictionary<(byte, byte), PhysicalControl> _lookup = new();
    private static readonly HashSet<PhysicalControl> _pressedWithShift = new();

    // -----------------------------------------------------------------
    // LISTA COMPLETA  (usada pelo wizard completo)
    // -----------------------------------------------------------------
    private static readonly List<CalibrationControl> AllSteps = new()
    {
        new("BrowseEncoder_TurnLeft",  "Gire o SELECTOR para a esquerda",                         true,  false),
        new("BrowseEncoder_TurnRight", "Gire o SELECTOR para a direita",                           true,  false),
        new("BrowseEncoder_Click",     "Clique o SELECTOR",                                        false, false),
        new("Load_Left",               "LOAD Deck Esquerdo",                                       false, false),
        new("Load_Left_Shift",         "SHIFT -> LOAD Deck Esquerdo",                              false, true),
        new("Load_Right",              "LOAD Deck Direito",                                        false, false),
        new("Load_Right_Shift",        "SHIFT -> LOAD Deck Direito",                               false, true),
        // Deck L
        new("Play_Left",               "PLAY/PAUSE Deck Esquerdo",                                false, false),
        new("Play_Left_Shift",         "SHIFT -> PLAY/PAUSE Deck Esquerdo",                       false, true),
        new("Cue_Left",                "CUE Deck Esquerdo",                                       false, false),
        new("Cue_Left_Shift",          "SHIFT -> CUE Deck Esquerdo",                              false, true),
        new("BeatSync_Left",           "BEAT SYNC Deck Esquerdo",                                 false, false),
        new("BeatSync_Left_Shift",     "SHIFT -> BEAT SYNC Deck Esquerdo",                        false, true),
        new("LoopIn_Left",             "LOOP IN Deck Esquerdo",                                   false, false),
        new("LoopIn_Left_Shift",       "SHIFT -> LOOP IN Deck Esquerdo",                          false, true),
        new("LoopOut_Left",            "LOOP OUT Deck Esquerdo",                                  false, false),
        new("LoopOut_Left_Shift",      "SHIFT -> LOOP OUT Deck Esquerdo",                         false, true),
        new("ReloopExit_Left",         "RELOOP/EXIT Deck Esquerdo",                               false, false),
        new("ReloopExit_Left_Shift",   "SHIFT -> RELOOP/EXIT Deck Esquerdo",                      false, true),
        new("LoopCallLeft_Left",       "LOOP CALL < Deck Esquerdo",                               false, false),
        new("LoopCallLeft_Left_Shift", "SHIFT -> LOOP CALL < Deck Esquerdo",                      false, true),
        new("LoopCallRight_Left",      "LOOP CALL > Deck Esquerdo",                               false, false),
        new("LoopCallRight_Left_Shift","SHIFT -> LOOP CALL > Deck Esquerdo",                      false, true),
        new("TempoSlider_Left",        "TEMPO/PITCH Deck Esquerdo (cima->baixo)",                  true,  false),
        new("HotCueMode_Left",         "Botão modo HOT CUE Deck Esquerdo",                        false, false),
        new("HotCueMode_Left_Shift",   "SHIFT -> HOT CUE Deck Esquerdo (ativa KEYBOARD mode)",    false, true),
        new("BeatLoopMode_Left",       "Botão modo BEAT LOOP Deck Esquerdo",                      false, false),
        new("BeatLoopMode_Left_Shift", "SHIFT -> BEAT LOOP Deck Esquerdo (ativa PAD FX 1)",        false, true),
        new("BeatJumpMode_Left",       "Botão modo BEAT JUMP Deck Esquerdo",                      false, false),
        new("BeatJumpMode_Left_Shift", "SHIFT -> BEAT JUMP Deck Esquerdo (ativa PAD FX 2)",        false, true),
        new("SamplerMode_Left",        "Botão modo SAMPLER Deck Esquerdo",                        false, false),
        new("SamplerMode_Left_Shift",  "SHIFT -> SAMPLER Deck Esquerdo (ativa KEY SHIFT)",         false, true),
        new("Pad1_Left",               "PAD 1 Deck Esquerdo",                                     false, false),
        new("Pad1_Left_Shift",         "SHIFT -> PAD 1 Deck Esquerdo",                             false, true),
        new("Pad2_Left",               "PAD 2 Deck Esquerdo",                                     false, false),
        new("Pad2_Left_Shift",         "SHIFT -> PAD 2 Deck Esquerdo",                             false, true),
        new("Pad3_Left",               "PAD 3 Deck Esquerdo",                                     false, false),
        new("Pad3_Left_Shift",         "SHIFT -> PAD 3 Deck Esquerdo",                             false, true),
        new("Pad4_Left",               "PAD 4 Deck Esquerdo",                                     false, false),
        new("Pad4_Left_Shift",         "SHIFT -> PAD 4 Deck Esquerdo",                             false, true),
        new("Pad5_Left",               "PAD 5 Deck Esquerdo",                                     false, false),
        new("Pad5_Left_Shift",         "SHIFT -> PAD 5 Deck Esquerdo",                             false, true),
        new("Pad6_Left",               "PAD 6 Deck Esquerdo",                                     false, false),
        new("Pad6_Left_Shift",         "SHIFT -> PAD 6 Deck Esquerdo",                             false, true),
        new("Pad7_Left",               "PAD 7 Deck Esquerdo",                                     false, false),
        new("Pad7_Left_Shift",         "SHIFT -> PAD 7 Deck Esquerdo",                             false, true),
        new("Pad8_Left",               "PAD 8 Deck Esquerdo",                                     false, false),
        new("Pad8_Left_Shift",         "SHIFT -> PAD 8 Deck Esquerdo",                             false, true),
        new("JogWheel_Outer_TurnLeft",    "Borda EXTERNA Jog Esquerdo -> esquerda",                true,  false),
        new("JogWheel_Outer_TurnRight",   "Borda EXTERNA Jog Esquerdo -> direita",                 true,  false),
        new("JogWheel_Top_Touch",         "TOQUE LEVE no prato do Jog Esquerdo (só encostar, não girar)", false, false),
        new("JogWheel_Top_Scratch",       "PRESSIONE+GIRE o prato do Jog Esquerdo (scratch)",    true,  false),
        // Deck R
        new("Play_Right",              "PLAY/PAUSE Deck Direito",                                 false, false),
        new("Play_Right_Shift",        "SHIFT -> PLAY/PAUSE Deck Direito",                        false, true),
        new("Cue_Right",               "CUE Deck Direito",                                        false, false),
        new("Cue_Right_Shift",         "SHIFT -> CUE Deck Direito",                               false, true),
        new("BeatSync_Right",          "BEAT SYNC Deck Direito",                                  false, false),
        new("BeatSync_Right_Shift",    "SHIFT -> BEAT SYNC Deck Direito",                         false, true),
        new("LoopIn_Right",            "LOOP IN Deck Direito",                                    false, false),
        new("LoopIn_Right_Shift",      "SHIFT -> LOOP IN Deck Direito",                           false, true),
        new("LoopOut_Right",           "LOOP OUT Deck Direito",                                   false, false),
        new("LoopOut_Right_Shift",     "SHIFT -> LOOP OUT Deck Direito",                          false, true),
        new("ReloopExit_Right",        "RELOOP/EXIT Deck Direito",                                false, false),
        new("ReloopExit_Right_Shift",  "SHIFT -> RELOOP/EXIT Deck Direito",                       false, true),
        new("LoopCallLeft_Right",      "LOOP CALL < Deck Direito",                                false, false),
        new("LoopCallLeft_Right_Shift","SHIFT -> LOOP CALL < Deck Direito",                       false, true),
        new("LoopCallRight_Right",     "LOOP CALL > Deck Direito",                                false, false),
        new("LoopCallRight_Right_Shift","SHIFT -> LOOP CALL > Deck Direito",                      false, true),
        new("TempoSlider_Right",       "TEMPO/PITCH Deck Direito (cima->baixo)",                   true,  false),
        new("HotCueMode_Right",        "Botão modo HOT CUE Deck Direito",                         false, false),
        new("HotCueMode_Right_Shift",  "SHIFT -> HOT CUE Deck Direito (KEYBOARD mode)",           false, true),
        new("BeatLoopMode_Right",      "Botão modo BEAT LOOP Deck Direito",                       false, false),
        new("BeatLoopMode_Right_Shift","SHIFT -> BEAT LOOP Deck Direito (PAD FX 1)",              false, true),
        new("BeatJumpMode_Right",      "Botão modo BEAT JUMP Deck Direito",                       false, false),
        new("BeatJumpMode_Right_Shift","SHIFT -> BEAT JUMP Deck Direito (PAD FX 2)",              false, true),
        new("SamplerMode_Right",       "Botão modo SAMPLER Deck Direito",                         false, false),
        new("SamplerMode_Right_Shift", "SHIFT -> SAMPLER Deck Direito (KEY SHIFT)",               false, true),
        new("Pad1_Right",              "PAD 1 Deck Direito",                                      false, false),
        new("Pad1_Right_Shift",        "SHIFT -> PAD 1 Deck Direito",                              false, true),
        new("Pad2_Right",              "PAD 2 Deck Direito",                                      false, false),
        new("Pad2_Right_Shift",        "SHIFT -> PAD 2 Deck Direito",                              false, true),
        new("Pad3_Right",              "PAD 3 Deck Direito",                                      false, false),
        new("Pad3_Right_Shift",        "SHIFT -> PAD 3 Deck Direito",                              false, true),
        new("Pad4_Right",              "PAD 4 Deck Direito",                                      false, false),
        new("Pad4_Right_Shift",        "SHIFT -> PAD 4 Deck Direito",                              false, true),
        new("Pad5_Right",              "PAD 5 Deck Direito",                                      false, false),
        new("Pad5_Right_Shift",        "SHIFT -> PAD 5 Deck Direito",                              false, true),
        new("Pad6_Right",              "PAD 6 Deck Direito",                                      false, false),
        new("Pad6_Right_Shift",        "SHIFT -> PAD 6 Deck Direito",                              false, true),
        new("Pad7_Right",              "PAD 7 Deck Direito",                                      false, false),
        new("Pad7_Right_Shift",        "SHIFT -> PAD 7 Deck Direito",                              false, true),
        new("Pad8_Right",              "PAD 8 Deck Direito",                                      false, false),
        new("Pad8_Right_Shift",        "SHIFT -> PAD 8 Deck Direito",                              false, true),
        new("JogWheel_Outer_TurnLeft_Right",  "Borda EXTERNA Jog Direito -> esquerda",             true,  false),
        new("JogWheel_Outer_TurnRight_Right", "Borda EXTERNA Jog Direito -> direita",              true,  false),
        new("JogWheel_Top_Touch_Right",  "TOQUE LEVE no prato do Jog Direito (só encostar, não girar)", false, false),
        new("JogWheel_Top_Scratch_Right","PRESSIONE+GIRE o prato do Jog Direito (scratch)",      true,  false),
        // Mixer
        new("Trim_Left",          "TRIM canal esquerdo (mín->máx)",                                true,  false),
        new("EQ_High_Left",       "EQ HIGH esquerdo (mín->máx)",                                   true,  false),
        new("EQ_Mid_Left",        "EQ MID esquerdo (mín->máx)",                                    true,  false),
        new("EQ_Low_Left",        "EQ LOW esquerdo (mín->máx)",                                    true,  false),
        new("Filter_Left",        "FILTER esquerdo (esq->dir)",                                    true,  false),
        new("HeadphoneCue_Left",  "HEADPHONE CUE canal esquerdo",                                 false, false),
        new("VolumeSlider_Left",  "VOLUME FADER esquerdo (baixo->cima)",                           true,  false),
        new("Trim_Right",         "TRIM canal direito (mín->máx)",                                 true,  false),
        new("EQ_High_Right",      "EQ HIGH direito (mín->máx)",                                    true,  false),
        new("EQ_Mid_Right",       "EQ MID direito (mín->máx)",                                     true,  false),
        new("EQ_Low_Right",       "EQ LOW direito (mín->máx)",                                     true,  false),
        new("Filter_Right",       "FILTER direito (esq->dir)",                                     true,  false),
        new("HeadphoneCue_Right", "HEADPHONE CUE canal direito",                                  false, false),
        new("VolumeSlider_Right", "VOLUME FADER direito (baixo->cima)",                            true,  false),
        new("Crossfader",         "CROSSFADER (esq->dir)",                                         true,  false),
        new("HeadphoneMixing",    "HEADPHONES MIXING (mín->máx)",                                  true,  false),
        new("MasterCue",          "MASTER CUE",                                                   false, false),
        // FX
        new("BeatLeft",           "BEAT <",                                                        false, false),
        new("BeatRight",          "BEAT >",                                                        false, false),
        new("FxSelect",           "FX SELECT",                                                    false, false),
        new("FxChannelSelect",    "FX CHANNEL SELECT (comute 1->2->Master)",                        true,  false),
        new("LevelDepth",         "LEVEL/DEPTH (mín->máx)",                                        true,  false),
        new("FxOnOff",            "FX ON/OFF",                                                    false, false),
    };

    // -----------------------------------------------------------------
    // PATCH LIST: etapas a refazer/acrescentar nesta sessão de patch
    // -----------------------------------------------------------------
    private static readonly List<string> PatchStepNames = new()
    {
    // Jog Wheel: papéis trocados - refazer com instruções mais claras
        "JogWheel_Top_Touch",
        "JogWheel_Top_Scratch",
        "JogWheel_Top_Touch_Right",
        "JogWheel_Top_Scratch_Right",
        // Pad Mode + Shift: etapas novas que não existiam no wizard anterior
        "HotCueMode_Left_Shift",
        "BeatLoopMode_Left_Shift",
        "BeatJumpMode_Left_Shift",
        "SamplerMode_Left_Shift",
        "HotCueMode_Right_Shift",
        "BeatLoopMode_Right_Shift",
        "BeatJumpMode_Right_Shift",
        "SamplerMode_Right_Shift",
    };

    // -----------------------------------------------------------------
    // MAIN
    // -----------------------------------------------------------------
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new System.Windows.Application();
        var mainWindow = new MainWindow();

        _ = Task.Run(() => RunProductionMode(mainWindow));

        app.Run(mainWindow);
    }

    // -----------------------------------------------------------------
    // PATCH WIZARD
    // -----------------------------------------------------------------
    private static async Task RunPatch()
    {
        // 1. Aplica correções automáticas ao JSON existente
        var existing = LoadJsonDict();
        ApplyAutomaticFixes(existing);
        SaveJsonDict(existing, $"Patch iniciado em {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        
        Console.WriteLine();

        // 2. Filtra da AllSteps apenas as etapas do PatchList
        var patchSteps = AllSteps.Where(s => PatchStepNames.Contains(s.Name)).ToList();

        // 3. Roda o wizard apenas com essas etapas, fazendo merge no JSON
        await RunWizard(patchSteps, merge: true);
    }

    // -----------------------------------------------------------------
    // WIZARD CORE  (usada tanto pelo completo quanto pelo patch)
    // -----------------------------------------------------------------
    private static async Task RunWizard(List<CalibrationControl> steps, bool merge)
    {
        Directory.CreateDirectory(DOCS_DIR);
        string rawPath = Path.Combine(DOCS_DIR, $"calibration_raw_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        using var raw  = new StreamWriter(rawPath, false, System.Text.Encoding.UTF8) { AutoFlush = true };
        raw.WriteLine($"# DDJ-LiveBridge - Raw Calibration Log | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        raw.WriteLine($"# Modo: {(merge ? "PATCH" : "COMPLETO")}");
        raw.WriteLine($"# Formato: [HH:mm:ss.fff] [ETAPA] Status(dec/hex) Data1(dec/hex) Data2\n");

        var listener = new InputListener();
        object lk = new(); MidiEvent? latest = null; bool hasNew = false;
        listener.OnRawMidiEvent += e => { lock(lk) { latest = e; hasNew = true; } };
        listener.Start(DDJ_IN);

        // Carrega resultados existentes para merge
        var results = merge ? LoadJsonDict() : new Dictionary<string, object>();

        Console.Clear();
        
        Console.WriteLine("  [N/Enter] Confirmar  |  [B] Voltar  |  [R] Limpar  |  [Esc] Cancelar");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Etapas SHIFT: espere '✔ SHIFT DETECTADO!' antes de apertar o botão.\n");
        Console.ResetColor();

        int idx = 0;
        while (idx < steps.Count)
        {
            var step = steps[idx];
            bool shiftReady = !step.IsShiftCombo;
            MidiEvent? captured = null;

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"  [{idx + 1}/{steps.Count}] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(step.Name);
            if (results.ContainsKey(step.Name))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(" (já existe - será substituído)");
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  👉 {step.Description}");
            Console.ResetColor();

            lock(lk) { latest = null; hasNew = false; }

            bool done = false;
            while (!done)
            {
                await Task.Delay(30);

                MidiEvent? ev = null;
                lock(lk) { if (hasNew) { ev = latest; hasNew = false; } }

                if (ev != null)
                {
                    // Log bruto SEMPRE
                    raw.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{step.Name}] " +
                                  $"Status={ev.Status}(0x{ev.Status:X2}) " +
                                  $"Data1={ev.Data1}(0x{ev.Data1:X2}) " +
                                  $"Data2={ev.Data2}");

                    bool isShiftBtn = (ev.Data1 == 63) && (ev.Status == 144 || ev.Status == 145);

                    // Noise gate
                    if (HardwareNoise.IsNoise(ev.Status, ev.Data1, step.Name))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"  [RUÍDO CC51 filtrado]");
                        raw.WriteLine("  >> RUÍDO DESCARTADO");
                        Console.ResetColor();
                        continue;
                    }

                    // Fase 1 de Shift: espera o botão Shift físico
                    if (step.IsShiftCombo && !shiftReady)
                    {
                        if (isShiftBtn && ev.Data2 > 0)
                        {
                            shiftReady = true;
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine("  ✔ SHIFT DETECTADO! Agora aperte o botão alvo (sem soltar Shift).");
                            raw.WriteLine("  >> Shift confirmado. Aguardando botão alvo.");
                            Console.ResetColor();
                        }
                        continue;
                    }

                    // Ignora o próprio Shift após confirmado
                    if (isShiftBtn) continue;

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  -> 0x{ev.Status:X2}({ev.Status}) / 0x{ev.Data1:X2}({ev.Data1}) / val={ev.Data2}");
                    raw.WriteLine("  >> CAPTURADO");
                    Console.ResetColor();

                    captured = ev;
                    results[step.Name] = new
                    {
                        Status       = ev.Status,
                        Data1        = ev.Data1,
                        IsContinuous = step.IsContinuous,
                        IsShiftCombo = step.IsShiftCombo
                    };
                }

                if (!Console.KeyAvailable) continue;
                var k = Console.ReadKey(intercept: true);

                if (k.Key is ConsoleKey.N or ConsoleKey.Enter)
                {
                    if (results.ContainsKey(step.Name))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  ✔ Salvo!\n");
                        raw.WriteLine("  >> CONFIRMADO\n");
                        Console.ResetColor();
                        idx++; done = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  ⚠ Interaja com o controle primeiro.");
                        Console.ResetColor();
                    }
                }
                else if (k.Key == ConsoleKey.R)
                {
                    captured = null; shiftReady = !step.IsShiftCombo;
                    results.Remove(step.Name);
                    lock(lk) { latest = null; hasNew = false; }
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  🧹 Limpo.\n");
                    raw.WriteLine("  >> REINICIADO");
                    Console.ResetColor();
                }
                else if (k.Key == ConsoleKey.B && idx > 0)
                {
                    idx--; done = true;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  <- Voltando...\n");
                    Console.ResetColor();
                }
                else if (k.Key == ConsoleKey.Escape)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[Wizard] Cancelado.");
                    Console.ResetColor();
                    listener.Dispose();
                    return;
                }
            }
        }

        listener.Dispose();
        SaveJsonDict(results, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n🎉 Concluído!");
        Console.WriteLine($"  Mapeamento: {JSON_PATH}");
        Console.WriteLine($"  Log bruto:  {rawPath}");
        Console.ResetColor();
        Console.WriteLine("Pressione qualquer tecla...");
        Console.ReadKey();
    }

    // -----------------------------------------------------------------
    // HELPERS JSON
    // -----------------------------------------------------------------
    private static Dictionary<string, object> LoadJsonDict()
    {
        if (!File.Exists(JSON_PATH)) return new();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(JSON_PATH));
            var d = new Dictionary<string, object>();
            foreach (var p in doc.RootElement.GetProperty("Controls").EnumerateObject())
            {
                d[p.Name] = new
                {
                    Status       = p.Value.GetProperty("Status").GetByte(),
                    Data1        = p.Value.GetProperty("Data1").GetByte(),
                    IsContinuous = p.Value.TryGetProperty("IsContinuous", out var ic) && ic.GetBoolean(),
                    IsShiftCombo = p.Value.TryGetProperty("IsShiftCombo", out var sc) && sc.GetBoolean(),
                };
            }
            return d;
        }
        catch { return new(); }
    }

    private static void ApplyAutomaticFixes(Dictionary<string, object> d)
    {
        // Trim_Right: por simetria com Trim_Left (Status 176, D1 36) -> Status 177, D1 36
        d["Trim_Right"] = new { Status = (byte)177, Data1 = (byte)36, IsContinuous = true, IsShiftCombo = false };

        // JogWheel_Top_Touch Esquerdo: era CC (176/33), deveria ser NoteOn do sensor de toque.
        // O evento NoteOn estava erroneamente em JogWheel_Top_Scratch (Status 144, D1 54).
        // Aplicamos a troca automaticamente.
        d["JogWheel_Top_Touch"]   = new { Status = (byte)144, Data1 = (byte)54, IsContinuous = false, IsShiftCombo = false };
        d["JogWheel_Top_Scratch"] = new { Status = (byte)176, Data1 = (byte)33, IsContinuous = true,  IsShiftCombo = false };
        // JogWheel Direito: Touch estava correto (145/54), Scratch pegou CC. Mantemos Touch e corrigimos Scratch.
        d["JogWheel_Top_Touch_Right"]   = new { Status = (byte)145, Data1 = (byte)54, IsContinuous = false, IsShiftCombo = false };
        d["JogWheel_Top_Scratch_Right"] = new { Status = (byte)177, Data1 = (byte)33, IsContinuous = true,  IsShiftCombo = false };
    }

    private static void SaveJsonDict(Dictionary<string, object> d, string date)
    {
        Directory.CreateDirectory(DOCS_DIR);
        var json = JsonSerializer.Serialize(new { CalibrationDate = date, Controls = d },
                   new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(JSON_PATH, json);
    }

    // -----------------------------------------------------------------
    // PRODUCTION MODE
    // -----------------------------------------------------------------
    private static void LoadCalibratedMappings()
    {
        _lookup.Clear();
        if (!File.Exists(JSON_PATH))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[Config] JSON não encontrado. Fallback mínimo.");
            Console.ResetColor();
            _lookup[(144, 11)]  = PhysicalControl.Play_Left;
            _lookup[(145, 11)]  = PhysicalControl.Play_Right;
            _lookup[(144, 12)]  = PhysicalControl.Cue_Left;
            _lookup[(145, 12)]  = PhysicalControl.Cue_Right;
            _lookup[(144, 63)]  = PhysicalControl.Shift_Left;
            _lookup[(145, 63)]  = PhysicalControl.Shift_Right;
            return;
        }

        using var doc  = JsonDocument.Parse(File.ReadAllText(JSON_PATH));
        var controls   = doc.RootElement.GetProperty("Controls");

        // Mapeamentos nome->enum que não seguem o padrão TryParse direto
        var specials = new Dictionary<string, PhysicalControl>
        {
            ["BrowseEncoder_TurnLeft"]       = PhysicalControl.BrowseEncoder_Turn,
            ["BrowseEncoder_TurnRight"]      = PhysicalControl.BrowseEncoder_Turn,
            ["BrowseEncoder_Click"]          = PhysicalControl.BrowseEncoder_Click,
            ["JogWheel_Outer_TurnLeft"]      = PhysicalControl.JogWheel_Left,
            ["JogWheel_Outer_TurnRight"]     = PhysicalControl.JogWheel_Left,
            ["JogWheel_Top_Touch"]           = PhysicalControl.JogWheel_Left,
            ["JogWheel_Top_Scratch"]         = PhysicalControl.JogWheel_Left,
            ["JogWheel_Outer_TurnLeft_Right"]  = PhysicalControl.JogWheel_Right,
            ["JogWheel_Outer_TurnRight_Right"] = PhysicalControl.JogWheel_Right,
            ["JogWheel_Top_Touch_Right"]     = PhysicalControl.JogWheel_Right,
            ["JogWheel_Top_Scratch_Right"]   = PhysicalControl.JogWheel_Right,
            ["VolumeSlider_Left"]            = PhysicalControl.Volume_Left,
            ["VolumeSlider_Right"]           = PhysicalControl.Volume_Right,
            ["HotCueMode_Left_Shift"]        = PhysicalControl.KeyboardMode_Left,
            ["BeatLoopMode_Left_Shift"]      = PhysicalControl.PadFX1_Left,
            ["BeatJumpMode_Left_Shift"]      = PhysicalControl.PadFX2_Left,
            ["SamplerMode_Left_Shift"]       = PhysicalControl.KeyShiftMode_Left,
            ["HotCueMode_Right_Shift"]       = PhysicalControl.KeyboardMode_Right,
            ["BeatLoopMode_Right_Shift"]     = PhysicalControl.PadFX1_Right,
            ["BeatJumpMode_Right_Shift"]     = PhysicalControl.PadFX2_Right,
            ["SamplerMode_Right_Shift"]      = PhysicalControl.KeyShiftMode_Right,
            ["BeatSync_Left"]                = PhysicalControl.Sync_Left,
            ["BeatSync_Left_Shift"]          = PhysicalControl.Sync_Left,
            ["BeatSync_Right"]               = PhysicalControl.Sync_Right,
            ["BeatSync_Right_Shift"]         = PhysicalControl.Sync_Right,
        };

        int count = 0;
        foreach (var prop in controls.EnumerateObject())
        {
            byte st   = prop.Value.GetProperty("Status").GetByte();
            byte d1   = prop.Value.GetProperty("Data1").GetByte();
            var  name = prop.Name;

            PhysicalControl pc = PhysicalControl.Unknown;
            if (specials.TryGetValue(name, out pc) ||
                Enum.TryParse(name.Replace("_Shift",""), out pc) && pc != PhysicalControl.Unknown)
            {
                _lookup[(st, d1)] = pc;
                count++;
            }
        }

        // Garante Shift físico
        _lookup[(144, 63)] = PhysicalControl.Shift_Left;
        _lookup[(145, 63)] = PhysicalControl.Shift_Right;

        // Garante Jog Wheels robustamente (pois enviam CC33, CC34 e NoteOn54 simultaneamente)
        _lookup[(176, 33)] = PhysicalControl.JogWheel_Left;
        _lookup[(176, 34)] = PhysicalControl.JogWheel_Left;
        _lookup[(144, 54)] = PhysicalControl.JogWheel_Left;
        
        _lookup[(177, 33)] = PhysicalControl.JogWheel_Right;
        _lookup[(177, 34)] = PhysicalControl.JogWheel_Right;
        _lookup[(145, 54)] = PhysicalControl.JogWheel_Right;

        // Garante os botões BeatLeft e BeatRight sob Shift físico (enviam notas 102 e 107 no status 148)
        _lookup[(148, 102)] = PhysicalControl.BeatLeft;
        _lookup[(148, 107)] = PhysicalControl.BeatRight;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Config] {count} controles carregados | Calibração: {doc.RootElement.GetProperty("CalibrationDate").GetString()}\n");
        Console.ResetColor();
    }

    private static bool TryDetectPad(byte status, byte data1, out PhysicalControl control, out string mode)
    {
        control = PhysicalControl.Unknown;
        mode = "Sampler";

        // Left Deck Pads
        if (status == 151) // Channel 8 (Pads Deck 1)
        {
            if (data1 >= 0 && data1 <= 7)
            {
                control = (PhysicalControl)((int)PhysicalControl.Pad1_Left + data1);
                mode = "HotCue";
                return true;
            }
            if (data1 >= 48 && data1 <= 55)
            {
                control = (PhysicalControl)((int)PhysicalControl.Pad1_Left + (data1 - 48));
                mode = "Sampler";
                return true;
            }
        }
        else if (status == 144) // Channel 1 (Keyboard Mode Deck 1)
        {
            if (data1 >= 0 && data1 <= 7)
            {
                control = (PhysicalControl)((int)PhysicalControl.Pad1_Left + data1);
                mode = "Keyboard";
                return true;
            }
        }

        // Right Deck Pads
        if (status == 153) // Channel 10 (Pads Deck 2)
        {
            if (data1 >= 0 && data1 <= 7)
            {
                control = (PhysicalControl)((int)PhysicalControl.Pad1_Right + data1);
                mode = "HotCue";
                return true;
            }
            if (data1 >= 48 && data1 <= 55)
            {
                control = (PhysicalControl)((int)PhysicalControl.Pad1_Right + (data1 - 48));
                mode = "Sampler";
                return true;
            }
        }
        else if (status == 145) // Channel 2 (Keyboard Mode Deck 2)
        {
            if (data1 >= 0 && data1 <= 7)
            {
                control = (PhysicalControl)((int)PhysicalControl.Pad1_Right + data1);
                mode = "Keyboard";
                return true;
            }
        }

        return false;
    }

    private static void RunProductionMode(MainWindow mainWindow) {
    try {

        LoadCalibratedMappings();

        var sm  = new StateManager();
        var ar  = new ActionRouter(sm);
        var il  = new InputListener();
        var oe = new OutputEmitter();

        string logDir  = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);
        string logFile = Path.Combine(logDir, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        using var lw   = new StreamWriter(logFile, false, System.Text.Encoding.UTF8) { AutoFlush = true };

        void Log(string msg, ConsoleColor c = ConsoleColor.Gray)
        {
            Console.ForegroundColor = c; Console.WriteLine(msg); Console.ResetColor();
            lw.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
        }

        oe.Connect(VIRTUAL_OUT, DDJ_OUT);

        il.OnRawMidiEvent += (raw) =>
        {
            PhysicalControl ctrl = PhysicalControl.Unknown;
            string padMode = "";

            if (TryDetectPad(raw.Status, raw.Data1, out var padCtrl, out var detectedMode))
            {
                ctrl = padCtrl;
                string deck = ctrl.ToString().EndsWith("_Right") ? "Right" : "Left";
                sm.SetMode(deck, detectedMode);
                padMode = detectedMode;
            }
            else
            {
                _lookup.TryGetValue((raw.Status, raw.Data1), out ctrl);
                padMode = sm.GetMode(ctrl);
            }

            if (ctrl != PhysicalControl.Unknown)
            {
                // Determina o estado Shift ativo na hora do processamento
                bool eventShiftActive = sm.IsShiftActive;

                if (ctrl == PhysicalControl.Shift_Left || ctrl == PhysicalControl.Shift_Right)
                {
                    sm.SetShift(ctrl, raw.Data2 > 0);

                    if (sm.IsShiftActive)
                        KeyboardSimulator.SendShiftDown();
                    else
                        KeyboardSimulator.SendShiftUp();

                    mainWindow.UpdateMidiControl(ctrl.ToString(), raw.Data2, raw.Status);
                    return;
                }

                if (raw.Data2 > 0) // Press ou CC/Knob move
                {
                    if (eventShiftActive)
                        _pressedWithShift.Add(ctrl);
                    else
                        _pressedWithShift.Remove(ctrl);
                }
                else // Release (raw.Data2 == 0)
                {
                    if (_pressedWithShift.Contains(ctrl))
                    {
                        eventShiftActive = true;
                        _pressedWithShift.Remove(ctrl);
                    }
                }

                var s = eventShiftActive ? " [SHIFT]" : "";
                Log($"[OK] {ctrl}{s}  val={raw.Data2}  (0x{raw.Status:X2}/0x{raw.Data1:X2})", ConsoleColor.Green);
                mainWindow.UpdateMidiControl(ctrl.ToString(), raw.Data2, raw.Status);

                var mode = padMode;
                var ev   = new ControlEvent(ctrl, raw.Data2, eventShiftActive, mode);
                var act  = ar.Resolve(ev);
                if (act == null) return;

                if (act.Type == ActionType.ModeChange)
                {
                    var p = act.Command.Split(':');
                    if (p.Length == 2) sm.SetMode(p[0], p[1]);
                    return;
                }

                oe.Emit(act);
            }
            else
            {
                Log($"[??] 0x{raw.Status:X2}/0x{raw.Data1:X2} D2={raw.Data2} CH={raw.Status & 0x0F}", ConsoleColor.DarkYellow);
                oe.SendRawMidi(raw.Status, raw.Data1, raw.Data2);
                return;
            }
        };

                
        il.Start(DDJ_IN);
        Log($"\n✔ Modo Produção ativo. Log: {logFile}\n[Q] para sair.\n", ConsoleColor.Cyan);

        while (true)
        {
            
            Task.Delay(50).Wait();
        }

        Log("[Bridge] Encerrando...", ConsoleColor.Yellow);
        il.Dispose(); oe.Dispose();
    } catch (Exception ex) {
        File.WriteAllText("crash_log.txt", ex.ToString());
    }
}
}



















