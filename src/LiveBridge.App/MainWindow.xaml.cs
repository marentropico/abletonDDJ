using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using DispatcherTimer = System.Windows.Threading.DispatcherTimer;

namespace LiveBridge.App;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, double> _jogAngles = new() { ["JogWheel_Left"] = 0, ["JogWheel_Right"] = 0 };
    private Dictionary<string, JsonElement> _calibrationMappings = new();
    private readonly HashSet<string> _activeToggles = new();
    private readonly HashSet<string> _pressedWithShift = new();
    private bool _isShiftActive = false;

    // Exposto para o Program.cs enfileirar sinais MIDI Learn capturados
    public string? MidiLearnTarget { get; set; }
    private SettingsWindow? _settingsWindow;

    // Fonte da verdade sincronizada com ActionRouter.cs — atualizado em v1.0.1
    // null/"N/A" significa que o controle não tem roteamento implementado nesta build.
    private static readonly Dictionary<string, (string Title, string Desc, string Shift)> MappingInfo = new()
    {
        // ── Transporte ────────────────────────────────────────────
        ["Play_Left"]  = ("Play / Pause (Deck 1)", "Alterna a reprodução global (Transport_PlayToggle via Remote Script).", "Stop All Clips: para todos os clipes ativos na Session View."),
        ["Play_Right"] = ("Play / Pause (Deck 2)", "Alterna a reprodução global (Transport_PlayToggle via Remote Script).", "Stop All Clips: para todos os clipes ativos na Session View."),
        ["Cue_Left"]   = ("CUE (Deck 1)", "Continua a reprodução a partir da posição atual da agulha (Transport_Continue).", "Back to Arrangement: retorna a reprodução para a timeline linear."),
        ["Cue_Right"]  = ("CUE (Deck 2)", "Continua a reprodução a partir da posição atual da agulha (Transport_Continue).", "Back to Arrangement: retorna a reprodução para a timeline linear."),
        ["ReloopExit_Left"]  = ("Reloop/Exit (Deck 1)", "Gravação contextual de slot de clipe: vazio=nova gravação, gravando=para e toca em loop, clipe existente=overdub. (CC 48, Ch 16)", "Sem mapeamento ativo."),
        ["ReloopExit_Right"] = ("Reloop/Exit (Deck 2)", "Gravação de Arrangement: liga/desliga a gravação geral na timeline. (CC 49, Ch 16)", "Sem mapeamento ativo."),
        ["Sync_Left"]  = ("Beat Sync (Deck 1)", "Quantiza as notas MIDI do clipe ativo (Ctrl+U).", "Metrônomo: liga/desliga o metrônomo do Ableton (tecla O)."),
        ["Sync_Right"] = ("Beat Sync (Deck 2)", "Sem mapeamento ativo nesta build.", "Sem mapeamento ativo."),

        // ── Jog Wheels ───────────────────────────────────────
        ["JogWheel_Left"]  = ("Jog Wheel (Deck 1)", "Borda externa: navegação fina na timeline (ScrollHorizontal + CC 40, Ch 16). Prato: sem mapeamento ativo.", "Sem mapeamento ativo."),
        ["JogWheel_Right"] = ("Jog Wheel (Deck 2)", "Borda externa: Zoom horizontal do Arrangement (teclas + e -). Prato: sem mapeamento ativo.", "Sem mapeamento ativo."),

        // ── Mixer ──────────────────────────────────────────────
        ["Volume_Left"]  = ("Channel Fader (Deck 1)", "Desativado: mau contato físico no hardware. Sem mapeamento ativo.", "Sem mapeamento ativo."),
        ["Volume_Right"] = ("Channel Fader (Deck 2)", "Desativado: mau contato físico no hardware. Sem mapeamento ativo.", "Sem mapeamento ativo."),
        ["Crossfader"]   = ("Crossfader", "Posiciona a agulha de reprodução na timeline do Arrangement. (CC 9, Ch 16)\nCom FX ON/OFF segurado: define o tamanho do loop ativo.", "Sem mapeamento ativo."),
        ["Trim_Left"]    = ("Trim / Gain (Deck 1)", "Volume da pista selecionada (CC 21, Ch 16). Com SHIFT: Ajuste Fino (25% velocidade, CC 52, Ch 16).", "Ajuste Fino: altera o parâmetro com 25% de velocidade."),
        ["Trim_Right"]   = ("Trim / Gain (Deck 2)", "PAN da pista selecionada (CC 31, Ch 16). Com SHIFT: Ajuste Fino (CC 62, Ch 16).", "Ajuste Fino: altera o parâmetro com 25% de velocidade."),
        ["EQ_High_Left"] = ("EQ High (Deck 1)", "Parâmetro 1 do plugin/rack focado (CC 22, Ch 16). Com SHIFT: Ajuste Fino relativo (CC 52, Ch 16).", "Ajuste Fino: sensibilidade a 25%."),
        ["EQ_Mid_Left"]  = ("EQ Mid (Deck 1)",  "Parâmetro 2 do plugin/rack focado (CC 23, Ch 16). Com SHIFT: Ajuste Fino relativo (CC 53, Ch 16).", "Ajuste Fino: sensibilidade a 25%."),
        ["EQ_Low_Left"]  = ("EQ Low (Deck 1)",  "Parâmetro 3 do plugin/rack focado (CC 24, Ch 16). Com SHIFT: Ajuste Fino relativo (CC 54, Ch 16).", "Ajuste Fino: sensibilidade a 25%."),
        ["Filter_Left"]  = ("Filter (Deck 1)",   "Parâmetro 4 do plugin/rack focado (CC 25, Ch 16). Com SHIFT: Ajuste Fino relativo (CC 55, Ch 16).", "Ajuste Fino: sensibilidade a 25%."),
        ["EQ_High_Right"] = ("EQ High (Deck 2)", "Parâmetro 5 do plugin/rack focado (CC 32, Ch 16). Com SHIFT: Ajuste Fino relativo (CC 62, Ch 16).", "Ajuste Fino: sensibilidade a 25%."),
        ["EQ_Mid_Right"]  = ("EQ Mid (Deck 2)",  "Parâmetro 6 do plugin/rack focado (CC 33, Ch 16). Com SHIFT: Ajuste Fino relativo (CC 63, Ch 16).", "Ajuste Fino: sensibilidade a 25%."),
        ["EQ_Low_Right"]  = ("EQ Low (Deck 2)",  "Parâmetro 7 do plugin/rack focado (CC 34, Ch 16). Com SHIFT: Ajuste Fino relativo (CC 64, Ch 16).", "Ajuste Fino: sensibilidade a 25%."),
        ["Filter_Right"]  = ("Filter (Deck 2)",  "Parâmetro 8 do plugin/rack focado (CC 35, Ch 16). Com SHIFT: Ajuste Fino relativo (CC 65, Ch 16).", "Ajuste Fino: sensibilidade a 25%."),
        ["HeadphoneCue_Left"]  = ("Headphone Cue (Deck 1)", "SOLO da pista selecionada na tela (CC 45, Ch 16).", "Sem mapeamento ativo."),
        ["HeadphoneCue_Right"] = ("Headphone Cue (Deck 2)", "MUTE da pista selecionada na tela (CC 46, Ch 16).", "Sem mapeamento ativo."),
        ["HeadphoneMixing"]    = ("Headphone Mixing",  "Volume Master alternativo (CC 38, Ch 16).", "Sem mapeamento ativo."),
        ["HeadphoneLevel"]     = ("Headphone Level",   "Controle análogo físico de volume de fone. Sem mapeamento via software ativo.", "Sem mapeamento ativo."),
        ["MasterLevel"]        = ("Master Level",      "Controle análogo físico de volume master. Sem mapeamento via software ativo.", "Sem mapeamento ativo."),
        ["MasterCue"]          = ("Master CUE",        "Sem mapeamento ativo nesta build.", "Sem mapeamento ativo."),
        ["TempoSlider_Left"]   = ("Tempo Slider (Deck 1)", "Controla o BPM global do projeto (CC 39, Ch 16 — valor invertido).", "Sem mapeamento ativo."),
        ["TempoSlider_Right"]  = ("Tempo Slider (Deck 2)", "Sem mapeamento ativo nesta build.", "Sem mapeamento ativo."),

        // ── Navegação e Workflow ──────────────────────────────
        ["Load_Left"]  = ("Load Deck A", "Alterna o foco entre as Pistas e o Browser do Ableton (CC 43, Ch 16). Ao focar o Browser, salta automaticamente para o Content Pane.", "Desabilitado (evita desalinhamento do estado de foco)."),
        ["Load_Right"] = ("Load Deck B", "Alterna a tela entre Session View e Arrangement View (CC 44, Ch 16).", "Sem mapeamento ativo."),
        ["LoopIn_Left"]         = ("Loop In (Deck 1)",      "Duplicar o clipe ou pista selecionada (Ctrl+D).", "Sem mapeamento ativo."),
        ["LoopOut_Left"]        = ("Loop Out (Deck 1)",     "Deletar o clipe ou pista selecionada (Delete).", "Sem mapeamento ativo."),
        ["LoopCallLeft_Left"]   = ("Loop Call \u25c1 (Deck 1)", "Desfazer a última ação (Ctrl+Z).", "Sem mapeamento ativo."),
        ["LoopCallRight_Left"]  = ("Loop Call \u25b7 (Deck 1)", "Refazer a última ação (Ctrl+Y).", "Sem mapeamento ativo."),
        ["LoopIn_Right"]        = ("Loop In (Deck 2)",      "Sem mapeamento ativo nesta build.", "Sem mapeamento ativo."),
        ["LoopOut_Right"]       = ("Loop Out (Deck 2)",     "Sem mapeamento ativo nesta build.", "Sem mapeamento ativo."),
        ["LoopCallLeft_Right"]  = ("Loop Call \u25c1 (Deck 2)", "Sem mapeamento ativo nesta build.", "Sem mapeamento ativo."),
        ["LoopCallRight_Right"] = ("Loop Call \u25b7 (Deck 2)", "Sem mapeamento ativo nesta build.", "Sem mapeamento ativo."),
        ["BrowseEncoder_Click"] = ("Browse Selector (Click)", "No Browser: Abre pasta/carrega efeito (Enter).\nNa Timeline: Corta/Seleciona clipe sob a agulha (Ctrl+E).", "Volta um diretório / recolhe pasta atual (Seta Esquerda)."),

        // ── Beat FX ─────────────────────────────────────────────
        ["BeatLeft"]       = ("Beat \u25c0 (FX)", "Navega para o dispositivo/efeito anterior na cadeia da pista ativa (Seta Esquerda).", "Com Shift físico: navega expandindo seleção (Shift+Seta)."),
        ["BeatRight"]      = ("Beat \u25b6 (FX)", "Navega para o próximo dispositivo/efeito na cadeia da pista ativa (Seta Direita).", "Com Shift físico: navega expandindo seleção (Shift+Seta)."),
        ["FxSelect"]       = ("FX Select", "Cria nova Pista de Áudio no projeto (Ctrl+T).", "Cria nova Pista MIDI (Ctrl+Shift+T)."),
        ["FxChannelSelect"] = ("FX Channel Select", "Chave analógica física (1/2/Master). Sem mapeamento via software ativo.", "Sem mapeamento ativo."),
        ["LevelDepth"]     = ("Level/Depth Knob", "Controla o parâmetro atualmente focado pelo mouse no Ableton (CC 47, Ch 16 — Wildcard).", "Sem mapeamento ativo."),
        ["FxOnOff"]        = ("FX ON/OFF", "Segurar + Crossfader: seleciona região de loop. Soltar: ativa o loop (Ctrl+Shift+L). (CC 41, Ch 16)", "Sem mapeamento ativo."),

        // ── Modos de Pads ─────────────────────────────────────
        ["HotCueMode_Left"]    = ("Hot Cue Mode (Deck 1)",   "Ativa modo Teclado Cromático Linear nos pads (layout sequencial C..D#+1 na oitava atual).", "Sem mapeamento ativo."),
        ["BeatLoopMode_Left"]  = ("Beat Loop Mode (Deck 1)", "Ativa modo Beat Loop nos pads (CC 70-77, Ch 16).", "Sem mapeamento ativo."),
        ["BeatJumpMode_Left"]  = ("Beat Jump Mode (Deck 1)", "Ativa modo Beat Jump nos pads (CC 90-97, Ch 16).", "Sem mapeamento ativo."),
        ["SamplerMode_Left"]   = ("Sampler Mode (Deck 1)",   "Ativa modo Teclado Cromático de Piano nos pads (layout C,D,E,F | C#,D#,-,F# com controle de oitava via Deck Direito).", "Sem mapeamento ativo."),
        ["HotCueMode_Right"]   = ("Hot Cue Mode (Deck 2)",   "Ativa modo Teclado Cromático Linear nos pads do Deck Direito.", "Sem mapeamento ativo."),
        ["BeatLoopMode_Right"] = ("Beat Loop Mode (Deck 2)", "Ativa modo Beat Loop nos pads (CC 80-87, Ch 16).", "Sem mapeamento ativo."),
        ["BeatJumpMode_Right"] = ("Beat Jump Mode (Deck 2)", "Ativa modo Beat Jump nos pads (CC 100-107, Ch 16).", "Sem mapeamento ativo."),
        ["SamplerMode_Right"]  = ("Sampler Mode (Deck 2)",   "Ativa modo Teclado Cromático de Piano nos pads do Deck Direito. Pad 4 = Subir oitava, Pad 8 = Descer oitava.", "Sem mapeamento ativo."),

        // ── Pads Individuais ──────────────────────────────────
        ["Pad1_Left"]  = ("Pad 1 (Deck 1)",  "Sampler: C# (Dó Sustenido). Hot Cue: E (Mi). Beat Loop: CC 70, Ch 16. Beat Jump: CC 90, Ch 16.", "Sem mapeamento ativo."),
        ["Pad2_Left"]  = ("Pad 2 (Deck 1)",  "Sampler: D# (Ré Sustenido). Hot Cue: F (Fá). Beat Loop: CC 71, Ch 16. Beat Jump: CC 91, Ch 16.", "Sem mapeamento ativo."),
        ["Pad3_Left"]  = ("Pad 3 (Deck 1)",  "Sampler: sem função (Mi# não existe). Hot Cue: F# (Fá Sust.). Beat Loop: CC 72. Beat Jump: CC 92.", "Sem mapeamento ativo."),
        ["Pad4_Left"]  = ("Pad 4 (Deck 1)",  "Sampler: F# (Fá Sustenido). Hot Cue: G (Sol). Beat Loop: CC 73. Beat Jump: CC 93.", "Sem mapeamento ativo."),
        ["Pad5_Left"]  = ("Pad 5 (Deck 1)",  "Sampler: C (Dó). Hot Cue: C (Dó). Beat Loop: CC 74. Beat Jump: CC 94.", "Sem mapeamento ativo."),
        ["Pad6_Left"]  = ("Pad 6 (Deck 1)",  "Sampler: D (Ré). Hot Cue: C# (Dó Sust.). Beat Loop: CC 75. Beat Jump: CC 95.", "Sem mapeamento ativo."),
        ["Pad7_Left"]  = ("Pad 7 (Deck 1)",  "Sampler: E (Mi). Hot Cue: D (Ré). Beat Loop: CC 76. Beat Jump: CC 96.", "Sem mapeamento ativo."),
        ["Pad8_Left"]  = ("Pad 8 (Deck 1)",  "Sampler: F (Fá). Hot Cue: D# (Ré Sust.). Beat Loop: CC 77. Beat Jump: CC 97.", "Sem mapeamento ativo."),
        ["Pad1_Right"] = ("Pad 1 (Deck 2)",  "Sampler: G# (Sol Sust.). Hot Cue: C+1 (Dó Oitava+). Beat Loop: CC 80. Beat Jump: CC 100.", "Sem mapeamento ativo."),
        ["Pad2_Right"] = ("Pad 2 (Deck 2)",  "Sampler: A# (Lá Sust.). Hot Cue: C#+1. Beat Loop: CC 81. Beat Jump: CC 101.", "Sem mapeamento ativo."),
        ["Pad3_Right"] = ("Pad 3 (Deck 2)",  "Sampler: sem função (Si# não existe). Hot Cue: D+1. Beat Loop: CC 82. Beat Jump: CC 102.", "Sem mapeamento ativo."),
        ["Pad4_Right"] = ("Pad 4 (Deck 2)",  "Sampler: Subir Oitava (+1). Hot Cue: D#+1. Beat Loop: CC 83. Beat Jump: CC 103.", "Sem mapeamento ativo."),
        ["Pad5_Right"] = ("Pad 5 (Deck 2)",  "Sampler: G (Sol). Hot Cue: G# (Sol Sust.). Beat Loop: CC 84. Beat Jump: CC 104.", "Sem mapeamento ativo."),
        ["Pad6_Right"] = ("Pad 6 (Deck 2)",  "Sampler: A (Lá). Hot Cue: A (Lá). Beat Loop: CC 85. Beat Jump: CC 105.", "Sem mapeamento ativo."),
        ["Pad7_Right"] = ("Pad 7 (Deck 2)",  "Sampler: B (Si). Hot Cue: A# (Lá Sust.). Beat Loop: CC 86. Beat Jump: CC 106.", "Sem mapeamento ativo."),
        ["Pad8_Right"] = ("Pad 8 (Deck 2)",  "Sampler: Descer Oitava (-1). Hot Cue: B (Si). Beat Loop: CC 87. Beat Jump: CC 107.", "Sem mapeamento ativo."),
    };

    private readonly Dictionary<string, Canvas> _controlsMap = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => {
            MapVisualControls(this);
        };
        LoadCalibrationMappings();
    }

    private void MapVisualControls(DependencyObject parent)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Canvas c && !string.IsNullOrEmpty(c.Name))
            {
                _controlsMap[c.Name] = c;
            }
            MapVisualControls(child);
        }
    }

    private void LoadCalibrationMappings()
    {
        string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "mappings_calibrated.json");
        // Fallback para pasta de desenvolvimento se necessário
        if (!System.IO.File.Exists(jsonPath))
        {
            jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "docs", "mappings_calibrated.json");
        }

        if (System.IO.File.Exists(jsonPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
                var controls = doc.RootElement.GetProperty("Controls");
                foreach (var prop in controls.EnumerateObject())
                {
                    _calibrationMappings[prop.Name] = prop.Value.Clone();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar calibração: {ex.Message}");
            }
        }
    }

    // Método exposto para receber atualizações MIDI vindas do InputListener
    public void UpdateMidiControl(string control, int value, int status = 0)
    {
        Dispatcher.Invoke(() =>
        {
            LogConsole.Text = $"Recebido: {control} | Valor: {value}";

            // Lógica para rastrear o estado do SHIFT
            if (control == "Shift_Left" || control == "Shift_Right")
            {
                _isShiftActive = value > 0;
            }
            
            // Lógica especial de flash do Browse Encoder
            if (control == "BrowseEncoder_TurnLeft" || control == "BrowseEncoder_TurnRight")
            {
                if (_controlsMap.TryGetValue("BrowseEncoder_Click", out var browseCanvas))
                {
                    var borderChild = browseCanvas.Children.OfType<Shape>().FirstOrDefault(s => s is Ellipse || s is Rectangle);
                    if (borderChild != null)
                    {
                        Color flashColor = control == "BrowseEncoder_TurnLeft" ? Color.FromRgb(46, 166, 255) : Color.FromRgb(46, 255, 166);
                        borderChild.Effect = new DropShadowEffect { BlurRadius = 15, Color = flashColor, ShadowDepth = 0, Opacity = 0.95 };
                        borderChild.Stroke = new SolidColorBrush(flashColor);
                        
                        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
                        timer.Tick += (senderTimer, eTimer) =>
                        {
                            borderChild.Effect = null;
                            borderChild.Stroke = new SolidColorBrush(Colors.Black);
                            timer.Stop();
                        };
                        timer.Start();
                    }
                }
                return;
            }

            // Redireciona eventos do Jog Wheel para seu respectivo canvas do deck (JogWheel_Left ou JogWheel_Right)
            string controlName = control;
            if (controlName.StartsWith("JogWheel_"))
            {
                controlName = (controlName.Contains("Right") || controlName.EndsWith("_Right")) ? "JogWheel_Right" : "JogWheel_Left";
            }

            // Localiza o Canvas associado ao controle
            if (!_controlsMap.TryGetValue(controlName, out var canvas)) return;

            // Detectar o tipo do controle analisando o nome ou seus filhos
            bool isKnob = canvas.Children.OfType<Line>().Any(l => l.Name == "" || l.Name == null) && canvas.Children.OfType<Ellipse>().Count() >= 2;
            bool isFader = canvas.Name.Contains("Slider") || canvas.Name == "Crossfader" || canvas.Name.Contains("Volume");
            bool isJog = canvas.Name.Contains("JogWheel");
            bool isBtn = !isKnob && !isFader && !isJog;

            if (isBtn)
            {
                var borderChild = canvas.Children.OfType<Shape>().FirstOrDefault(s => s is Ellipse || s is Rectangle);
                if (borderChild != null)
                {
                    bool isBlueBtn = canvas.Name.Contains("Play") || canvas.Name.Contains("FxOnOff");
                    bool isOrangeBtn = canvas.Name.Contains("Sync") || canvas.Name.Contains("Load") || canvas.Name.Contains("Cue") || canvas.Name.Contains("Loop") || canvas.Name.Contains("Mode");

                    // Determina se o botão é do tipo alternador (Toggle)
                    bool isToggleType = canvas.Name.Contains("Play") || canvas.Name.Contains("Sync") || canvas.Name.Contains("FxOnOff");

                    bool eventShiftActive = _isShiftActive;
                    if (value > 0)
                    {
                        if (eventShiftActive)
                            _pressedWithShift.Add(canvas.Name);
                        else
                            _pressedWithShift.Remove(canvas.Name);

                        if (isToggleType && !eventShiftActive)
                        {
                            if (_activeToggles.Contains(canvas.Name))
                                _activeToggles.Remove(canvas.Name);
                            else
                                _activeToggles.Add(canvas.Name);
                        }
                    }
                    else // value == 0
                    {
                        if (_pressedWithShift.Contains(canvas.Name))
                        {
                            eventShiftActive = true;
                            _pressedWithShift.Remove(canvas.Name);
                        }
                    }

                    bool shouldLightUp = isToggleType ? _activeToggles.Contains(canvas.Name) : (value > 0);

                    if (shouldLightUp)
                    {
                        Color glowColor = Colors.White;
                        if (eventShiftActive)
                        {
                            // Shift ativo -> Brilho roxo neon premium
                            glowColor = Color.FromRgb(212, 40, 255);
                            borderChild.Fill = (Brush)FindResource("purpleGrad");
                        }
                        else
                        {
                            if (isBlueBtn)
                            {
                                glowColor = Color.FromRgb(46, 166, 255);
                                borderChild.Fill = (Brush)FindResource("blueGrad");
                            }
                            else if (isOrangeBtn)
                            {
                                glowColor = Color.FromRgb(255, 159, 28);
                                borderChild.Fill = (Brush)FindResource("orangeGrad");
                            }
                        }
                        
                        borderChild.Effect = new DropShadowEffect { BlurRadius = 15, Color = glowColor, ShadowDepth = 0, Opacity = 0.95 };
                        borderChild.Stroke = new SolidColorBrush(glowColor);
                    }
                    else
                    {
                        borderChild.Effect = null;
                        
                        // Restaura o preenchimento padrão
                        if (borderChild is Rectangle)
                        {
                            borderChild.Fill = canvas.Name.Contains("Pad") ? (Brush)FindResource("padGrad") : (Brush)FindResource("btnGrad");
                        }
                        else // Ellipse
                        {
                            borderChild.Fill = (Brush)FindResource("btnRoundGrad");
                        }

                        // Restaura o contorno padrão
                        if (isBlueBtn) borderChild.Stroke = new SolidColorBrush(Color.FromRgb(10, 63, 102));
                        else if (isOrangeBtn) borderChild.Stroke = new SolidColorBrush(Color.FromRgb(122, 61, 0));
                        else borderChild.Stroke = new SolidColorBrush(Colors.Black);
                    }
                }
            }
            else if (isKnob)
            {
                // Rotacionar o ponteiro (Line)
                var line = canvas.Children.OfType<Line>().FirstOrDefault();
                if (line != null)
                {
                    double angle = ((value - 64) / 63.0) * 135.0;
                    line.RenderTransform = new RotateTransform(angle, line.X1, line.Y1);
                }
            }
            else if (isFader)
            {
                // Faders e Sliders
                var track = canvas.Children.OfType<Rectangle>().FirstOrDefault();
                var cap = canvas.Children.OfType<Rectangle>().ElementAtOrDefault(1); // 0 é o trilho, 1 é o cap
                if (track != null && cap != null)
                {
                    if (track.Height > track.Width)
                    {
                        // Fader Vertical (TempoSlider move em direção contrária aos faders de Volume)
                        double maxDist = (track.Height - cap.Height) / 2.0;
                        double multiplier = canvas.Name.Contains("TempoSlider") ? 1.0 : -1.0;
                        double offset = multiplier * ((value - 64) / 63.0) * maxDist;
                        cap.RenderTransform = new TranslateTransform(0, offset);
                    }
                    else
                    {
                        // Crossfader Horizontal
                        double maxDist = (track.Width - cap.Width) / 2.0;
                        double offset = ((value - 64) / 63.0) * maxDist;
                        cap.RenderTransform = new TranslateTransform(offset, 0);
                    }
                }
            }
            else if (isJog)
            {
                // Rotação apenas para eventos de Turn (CC)
                bool isRotationEvent = control.Contains("Turn") || control.Contains("Scratch") || control.EndsWith("_Turn_Left") || control.EndsWith("_Turn_Right");
                if (isRotationEvent)
                {
                    double delta = 0;
                    if (value > 0 && value < 64) delta = value;
                    else if (value >= 64) delta = -(128 - value);

                    _jogAngles[canvas.Name] += (delta * 3.0);
                }

                double cx = canvas.Name == "JogWheel_Left" ? 255.0 : 1325.0;
                double cy = 385.0;

                foreach (UIElement child in canvas.Children)
                {
                    // Ignora o anel externo e o box de logo centrado
                    if (child is Ellipse el && el.Width > 400) continue;
                    if (child is Rectangle) continue;
                    
                    double localCx = cx;
                    double localCy = cy;
                    
                    double left = Canvas.GetLeft(child);
                    double top = Canvas.GetTop(child);
                    
                    if (!double.IsNaN(left)) localCx -= left;
                    if (!double.IsNaN(top)) localCy -= top;
                    
                    child.RenderTransform = new RotateTransform(_jogAngles[canvas.Name], localCx, localCy);
                }
            }
        });
    }

    // Eventos de Clique nos Elementos
    private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var canvas = sender as Canvas;
        if (canvas == null || string.IsNullOrEmpty(canvas.Name)) return;

        string controlId = canvas.Name;
        
        if (MappingInfo.TryGetValue(controlId, out var info))
        {
            InfoTitle.Text = info.Title;
            InfoDesc.Text = info.Desc;
            InfoShift.Text = info.Shift;
        }
        else
        {
            InfoTitle.Text = controlId.Replace("_", " ");
            InfoDesc.Text = "Controle físico da DDJ-400 no Ableton Live.";
            InfoShift.Text = "N/A";
        }

        // Calibração MIDI
        string calKey = controlId;
        if (controlId == "BrowseEncoder_Click") calKey = "BrowseEncoder_Click";
        else if (controlId == "JogWheel_Left") calKey = "JogWheel_Top_Touch";
        else if (controlId == "JogWheel_Right") calKey = "JogWheel_Top_Touch_Right";
        else if (controlId == "Volume_Left") calKey = "VolumeSlider_Left";
        else if (controlId == "Volume_Right") calKey = "VolumeSlider_Right";

        if (_calibrationMappings.TryGetValue(calKey, out var cal) || _calibrationMappings.TryGetValue(controlId, out cal))
        {
            byte status = cal.GetProperty("Status").GetByte();
            byte cc = cal.GetProperty("Data1").GetByte();
            bool isContinuous = cal.GetProperty("IsContinuous").GetBoolean();

            MidiStatus.Text = $"Status: 0x{status:X2} ({status})";
            MidiCc.Text = $"Data1 (CC): 0x{cc:X2} ({cc})";
            MidiType.Text = $"Contínuo: {(isContinuous ? "Sim (Fader / Knob)" : "Não (Botão / Pad)")}";
        }
        else
        {
            MidiStatus.Text = "Status: N/A";
            MidiCc.Text = "Data1 (CC): N/A";
            MidiType.Text = "Mapeamento Virtual";
        }

        InfoPanel.Visibility = Visibility.Visible;
    }

    private void Element_MouseEnter(object sender, MouseEventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas == null) return;

        // Efeito de hover suave
        var shapes = canvas.Children.OfType<Shape>();
        foreach (var shape in shapes)
        {
            if (shape.Effect == null) // apenas se não estiver ativo
            {
                shape.Opacity = 0.9;
            }
        }
    }

    private void Element_MouseLeave(object sender, MouseEventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas == null) return;

        var shapes = canvas.Children.OfType<Shape>();
        foreach (var shape in shapes)
        {
            shape.Opacity = 1.0;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        InfoPanel.Visibility = Visibility.Collapsed;
    }

    // Fechar painel ao clicar no fundo (apenas se não clicou num elemento filho)
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        // Só fecha o painel se o clique foi na área externa (não num Canvas interativo)
        if (e.Source is not Canvas)
            InfoPanel.Visibility = Visibility.Collapsed;
    }

    // ── Botão de Configurações ──────────────────────────────────────
    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow != null && _settingsWindow.IsLoaded)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow();
        _settingsWindow.Owner = this;

        // Conecta o MIDI Learn: quando o SettingsWindow pede, registra o alvo
        _settingsWindow.MidiLearnRequested += controlId =>
        {
            MidiLearnTarget = controlId;
        };
        _settingsWindow.MidiLearnCancelled += () =>
        {
            MidiLearnTarget = null;
        };

        _settingsWindow.Show();
    }

    // Chamado pelo Program.cs ao capturar sinal MIDI quando em modo Learn
    public void NotifyMidiLearn(byte status, byte data1)
    {
        if (MidiLearnTarget == null) return;
        Dispatcher.Invoke(() => _settingsWindow?.OnMidiLearnCaptured(status, data1));
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        SaveScreenshot();
    }

    private void SaveScreenshot()
    {
        try
        {
            UpdateLayout();
            double width  = ActualWidth  > 0 ? ActualWidth  : 1200;
            double height = ActualHeight > 0 ? ActualHeight : 800;

            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)width, (int)height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(this);

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

            // Usa caminho relativo ao executável — sem dependência de path de conversa hardcoded
            string screenshotDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
            Directory.CreateDirectory(screenshotDir);
            string path = System.IO.Path.Combine(screenshotDir, $"ui_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            using var stream = File.Create(path);
            encoder.Save(stream);
            Console.WriteLine($"[Screenshot] Salvo em: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Screenshot] Erro: {ex.Message}");
        }
    }
}
