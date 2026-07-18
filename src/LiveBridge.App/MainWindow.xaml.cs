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

    // Banco de dados estático de mapeamentos do Ableton Live (idêntico ao app.js)
    private static readonly Dictionary<string, (string Title, string Desc, string Shift)> MappingInfo = new()
    {
        ["Play_Left"] = ("Play/Pause (Deck 1)", "Alterna a reprodução global do Ableton Live (toca ou pausa a música).", "Stop All Clips: Para imediatamente a reprodução de todos os clipes que estiverem tocando na Session View (Panic Button)."),
        ["Play_Right"] = ("Play/Pause (Deck 2)", "Alterna a reprodução global do Ableton Live (toca ou pausa a música).", "Stop All Clips: Para imediatamente a reprodução de todos os clipes que estiverem tocando na Session View (Panic Button)."),
        ["Cue_Left"] = ("CUE (Deck 1)", "Continua a reprodução da música exatamente a partir do local atual da agulha/marcador.", "Back to Arrangement: Restaura a reprodução para a Timeline do Arrangement View."),
        ["Cue_Right"] = ("CUE (Deck 2)", "Continua a reprodução da música exatamente a partir do local atual da agulha/marcador.", "Back to Arrangement: Restaura a reprodução para a Timeline do Arrangement View."),
        ["ReloopExit_Left"] = ("Reloop/Exit (Deck 1)", "Looper inteligente contextual:\n• Slot vazio: Arma a pista e grava novo clipe.\n• Slot gravando: Para gravação e inicia loop.\n• Slot parado/tocando: Inicia Overdub.", "N/A"),
        ["ReloopExit_Right"] = ("Reloop/Exit (Deck 2)", "Arrangement Record: Liga ou desliga a gravação global da timeline do projeto.", "N/A"),
        ["Sync_Left"] = ("Beat Sync (Deck 1)", "Quantização rápida: Quantiza as notas MIDI selecionadas no clipe ativo (atalho Ctrl+U).", "Metrônomo: Liga ou desliga o metrônomo do Ableton Live (atalho Ctrl+Option+M)."),
        ["Sync_Right"] = ("Beat Sync (Deck 2)", "Livre para mapeamento MIDI genérico no Ableton Live.", "N/A"),
        
        ["JogWheel_Left"] = ("Jog Wheel (Deck 1)", "Navegação na Timeline: Girar o anel externo simula o scroll horizontal do mouse para caminhar pelo arranjo com follow de tela ativo.", "N/A"),
        ["JogWheel_Right"] = ("Jog Wheel (Deck 2)", "Zoom de Timeline: Girar o anel externo simula as teclas + e - do teclado para controlar o Zoom Horizontal no Arrangement View.", "N/A"),
        
        ["Volume_Left"] = ("Volume (Canal 1)", "Fader físico desativado temporariamente na ponte C# devido a oscilações físicas de mau contato (hardware noise).", "N/A"),
        ["Volume_Right"] = ("Volume (Canal 2)", "Fader físico desativado temporariamente na ponte C# devido a oscilações físicas de mau contato (hardware noise).", "N/A"),
        ["Crossfader"] = ("Crossfader", "Needle Search: Move a agulha de reprodução (playhead) ao longo de todo o arranjo.", "Timeline Loop Maker: Segure o botão FX ON/OFF e arraste o Crossfader para definir e redimensionar dinamicamente o tamanho do Loop ativo."),
        ["Trim_Left"] = ("Gain Trim (Canal 1)", "Volume da Track: Controla o volume principal da pista que estiver selecionada/focada na tela.", "N/A"),
        ["Trim_Right"] = ("Gain Trim (Canal 2)", "Panning: Controla o Panning estéreo (balanço Esquerdo/Direito) da pista selecionada na tela.", "N/A"),
        ["HeadphoneCue_Left"] = ("Headphones CUE (Canal 1)", "Solo: Monitora (SOLO) a faixa selecionada.", "N/A"),
        ["HeadphoneCue_Right"] = ("Headphones CUE (Canal 2)", "Mute: Silencia (MUTE) a faixa selecionada.", "N/A"),
        
        ["EQ_High_Left"] = ("EQ High (Canal 1)", "Controla a primeira Macro/Parâmetro do plugin ou rack focado na pista ativa (EQ High).", "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão."),
        ["EQ_Mid_Left"] = ("EQ Mid (Canal 1)", "Controla a segunda Macro/Parâmetro do plugin ou rack focado na pista ativa (EQ Mid).", "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão."),
        ["EQ_Low_Left"] = ("EQ Low (Canal 1)", "Controla a terceira Macro/Parâmetro do plugin ou rack focado na pista ativa (EQ Low).", "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão."),
        ["Filter_Left"] = ("Filter (Canal 1)", "Controla a quarta Macro/Parâmetro do plugin ou rack focado na pista ativa (Filter Cutoff).", "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão."),
        ["EQ_High_Right"] = ("EQ High (Canal 2)", "Controla a quinta Macro/Parâmetro do plugin focado na pista ativa (EQ High 2).", "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão."),
        ["EQ_Mid_Right"] = ("EQ Mid (Canal 2)", "Controla a sexta Macro/Parâmetro do plugin focado na pista ativa (EQ Mid 2).", "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão."),
        ["EQ_Low_Right"] = ("EQ Low (Canal 2)", "Controla a sétima Macro/Parâmetro do plugin focado na pista ativa (EQ Low 2).", "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão."),
        ["Filter_Right"] = ("Filter (Canal 2)", "Controla a oitava Macro/Parâmetro do plugin focado na pista ativa (Filter 2).", "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão."),
        
        ["HeadphoneMixing"] = ("Headphones Mixing", "BPM Master: Controla dinamicamente a velocidade (BPM geral) do Ableton Live.", "N/A"),
        ["HeadphoneLevel"] = ("Headphones Level", "Knob analógico de hardware da controladora. Controla o volume de monitoramento físico nos fones.", "N/A"),
        ["MasterLevel"] = ("Master Level", "Knob analógico de hardware da controladora. Controla o volume master físico enviado para a saída de áudio.", "N/A"),
        ["MasterCue"] = ("Master CUE", "Envia o canal Master do Ableton Live para monitoramento de fone de ouvido.", "N/A"),
        ["TempoSlider_Left"] = ("Pitch Fader (Deck 1)", "Ajuste fino de BPM: Permite calibrar o BPM do projeto ativamente com base na escala analógica do fader.", "N/A"),
        ["TempoSlider_Right"] = ("Pitch Fader (Deck 2)", "Livre para mapeamentos MIDI manuais adicionais no Ableton Live.", "N/A"),
        
        ["Load_Left"] = ("Load Left", "Alternador de Foco: Alterna o foco do teclado do Ableton entre o Navegador (Browser lateral) e as Pistas (Tracks).", "Related Tracks: Abre a aba de faixas recomendadas."),
        ["Load_Right"] = ("Load Right", "Alternador de Telas: Alterna visualmente o Ableton entre a Session View (Grelha de Clipes) e o Arrangement View (Timeline).", "N/A"),
        ["LoopIn_Left"] = ("Loop In (Deck 1)", "Duplicar: Duplica instantaneamente o clipe, pista ou cena selecionada (simula atalho Ctrl+D).", "N/A"),
        ["LoopOut_Left"] = ("Loop Out (Deck 1)", "Deletar: Exclui o clipe, pista ou elemento selecionado (simula tecla Delete).", "N/A"),
        ["LoopCallLeft_Left"] = ("Loop Call ◁ (Deck 1)", "Undo: Desfaz a última alteração realizada no projeto (simula atalho Ctrl+Z).", "Loop 1/2X: Corta o tamanho do loop atual pela metade."),
        ["LoopCallRight_Left"] = ("Loop Call ▷ (Deck 1)", "Redo: Refaz a última ação desfeita no projeto (simula atalho Ctrl+Y).", "Loop 2X: Dobra o tamanho do loop atual."),
        ["BrowseEncoder_Click"] = ("Browse Selector", "Seletor de Browser:\n• Girar: Navega verticalmente pelas pastas, faixas ou plugins (simula setas Cima/Baixo).\n• Clicar: Carrega o sample/dispositivo ou abre pastas (simula Enter).", "Seta Esquerda: Volta um nível de diretório ou recolhe a pasta atual."),
        
        ["BeatLeft"] = ("Beat FX Button Left ◁", "Navegar Dispositivos: Move a seleção de dispositivos/efeitos para a esquerda na cadeia da pista ativa.", "N/A"),
        ["BeatRight"] = ("Beat FX Button Right ▷", "Navegar Dispositivos: Move a seleção de dispositivos/efeitos para a direita na cadeia da pista ativa.", "N/A"),
        ["FxSelectDown"] = ("FX Select Down ▽", "Criar faixa de Áudio: Cria uma nova pista de áudio no projeto (simula atalho Ctrl+T).", "Criar faixa MIDI: Cria uma nova pista MIDI no projeto (simula atalho Ctrl+Shift+T)."),
        ["FxSelectUp"] = ("FX Select Up △", "Criar faixa de Áudio: Cria uma nova pista de áudio no projeto (simula atalho Ctrl+T).", "Criar faixa MIDI: Cria uma nova pista MIDI no projeto (simula atalho Ctrl+Shift+T)."),
        ["FxChannelSelect"] = ("FX Channel Select", "Chave seletora física (1 / 2 / Master) de canal alvo para efeitos. Livre para mapeamento MIDI.", "N/A"),
        ["LevelDepth"] = ("Level/Depth Knob", "Knob Curinga: Controla diretamente o valor do parâmetro que estiver atualmente sob o foco do mouse no Ableton Live.", "N/A"),
        ["FxOnOff"] = ("FX ON/OFF Button", "Loop Helper:\n• Ao pressionar: Guarda a agulha atual e ativa o Loop.\n• Ao soltar: Trava a região como o loop ativo da música (simula atalho Ctrl+Shift+L).", "N/A"),
        
        ["HotCueMode_Left"] = ("Mode Button: Hot Cue (Deck 1)", "Alterna os pads do deck esquerdo para controlar o Lançamento de Clipes.", "Ativa modo Keyboard."),
        ["BeatLoopMode_Left"] = ("Mode Button: Beat Loop (Deck 1)", "Alterna os pads do deck esquerdo para controle do plugin de Looper.", "Ativa modo Pad FX 1."),
        ["BeatJumpMode_Left"] = ("Mode Button: Beat Jump (Deck 1)", "Alterna os pads do deck esquerdo para acionamento de efeitos momentâneos.", "Ativa modo Pad FX 2."),
        ["SamplerMode_Left"] = ("Mode Button: Sampler (Deck 1)", "Alterna os pads do deck esquerdo para tocar Drum Racks no canal MIDI 1.", "Ativa modo Key Shift."),
        ["HotCueMode_Right"] = ("Mode Button: Hot Cue (Deck 2)", "Alterna os pads do deck direito para controlar ativação e ligar/desligar botões de efeitos.", "Ativa modo Keyboard."),
        ["BeatLoopMode_Right"] = ("Mode Button: Beat Loop (Deck 2)", "Alterna os pads do deck direito para controle do plugin de Looper.", "Ativa modo Pad FX 1."),
        ["BeatJumpMode_Right"] = ("Mode Button: Beat Jump (Deck 2)", "Alterna os pads do deck direito para acionamento de efeitos momentâneos.", "Ativa modo Pad FX 2."),
        ["SamplerMode_Right"] = ("Mode Button: Sampler (Deck 2)", "Alterna os pads do deck direito para tocar Drum Racks no canal MIDI 2.", "Ativa modo Key Shift."),
        
        ["Pad1_Left"] = ("Pad 1 (Deck 1)", "Sampler: Nota C1. Hot Cue: Dispara clipe 1 da Track 1. Beat Loop: Record/Play/Overdub.", "Hot Cue: Para ou apaga o clipe."),
        ["Pad2_Left"] = ("Pad 2 (Deck 1)", "Sampler: Nota C#1. Hot Cue: Dispara clipe 2 da Track 1. Beat Loop: Looper Stop.", "Hot Cue: Para ou apaga o clipe."),
        ["Pad3_Left"] = ("Pad 3 (Deck 1)", "Sampler: Nota D1. Hot Cue: Dispara clipe 3 da Track 1. Beat Loop: Looper Undo.", "Hot Cue: Para ou apaga o clipe."),
        ["Pad4_Left"] = ("Pad 4 (Deck 1)", "Sampler: Nota D#1. Hot Cue: Dispara clipe 4 da Track 1. Beat Loop: Looper Clear.", "Hot Cue: Para ou apaga o clipe."),
        ["Pad5_Left"] = ("Pad 5 (Deck 1)", "Sampler: Nota E1. Hot Cue: Dispara clipe 5 da Track 1. Beat Loop: N/A.", "Hot Cue: Para ou apaga o clipe."),
        ["Pad6_Left"] = ("Pad 6 (Deck 1)", "Sampler: Nota F1. Hot Cue: Dispara clipe 6 da Track 1. Beat Loop: N/A.", "Hot Cue: Para ou apaga o clipe."),
        ["Pad7_Left"] = ("Pad 7 (Deck 1)", "Sampler: Nota F#1. Hot Cue: Dispara clipe 7 da Track 1. Beat Loop: N/A.", "Hot Cue: Para ou apaga o clipe."),
        ["Pad8_Left"] = ("Pad 8 (Deck 1)", "Sampler: Nota G1. Hot Cue: Dispara clipe 8 da Track 1. Beat Loop: N/A.", "Hot Cue: Para ou apaga o clipe."),
        
        ["Pad1_Right"] = ("Pad 1 (Deck 2)", "Sampler: Nota C1. Hot Cue: Inverte/liga botão ou interruptor 1 do plugin focado.", "N/A"),
        ["Pad2_Right"] = ("Pad 2 (Deck 2)", "Sampler: Nota C#1. Hot Cue: Inverte/liga botão ou interruptor 2 do plugin focado.", "N/A"),
        ["Pad3_Right"] = ("Pad 3 (Deck 2)", "Sampler: Nota D1. Hot Cue: Inverte/liga botão ou interruptor 3 do plugin focado.", "N/A"),
        ["Pad4_Right"] = ("Pad 4 (Deck 2)", "Sampler: Nota D#1. Hot Cue: Inverte/liga botão ou interruptor 4 do plugin focado.", "N/A"),
        ["Pad5_Right"] = ("Pad 5 (Deck 2)", "Sampler: Nota E1. Hot Cue: Inverte/liga botão ou interruptor 5 do plugin focado.", "N/A"),
        ["Pad6_Right"] = ("Pad 6 (Deck 2)", "Sampler: Nota F1. Hot Cue: Inverte/liga botão ou interruptor 6 do plugin focado.", "N/A"),
        ["Pad7_Right"] = ("Pad 7 (Deck 2)", "Sampler: Nota F#1. Hot Cue: Inverte/liga botão ou interruptor 7 do plugin focado.", "N/A"),
        ["Pad8_Right"] = ("Pad 8 (Deck 2)", "Sampler: Nota G1. Hot Cue: Inverte/liga botão ou interruptor 8 do plugin focado.", "N/A"),
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
                // Rotação apenas para eventos de movimento contínuo (CC 176/177)
                bool isRotationEvent = (status == 176 || status == 177);
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
