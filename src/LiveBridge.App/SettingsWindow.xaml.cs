using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DispatcherTimer = System.Windows.Threading.DispatcherTimer;
using System.Windows.Media.Animation;

namespace LiveBridge.App;

/// <summary>
/// Item de controle para a listagem no SettingsWindow.
/// </summary>
public class ControlItem
{
    public string Id           { get; set; } = "";
    public string DisplayName  { get; set; } = "";
    public string Category     { get; set; } = "";
    public string ActionDescription { get; set; } = "";
    public bool   IsShift      { get; set; }
    public bool   Calibrated   { get; set; }
    public bool   HasAction    { get; set; }
    public byte   MidiStatus   { get; set; }
    public byte   MidiData1    { get; set; }
    public bool   IsContinuous { get; set; }

    // ── Bindings de cor para a View ──
    public Brush StatusColor => HasAction && Calibrated ? new SolidColorBrush(Color.FromRgb(34,  197, 94))   // verde
                              : Calibrated              ? new SolidColorBrush(Color.FromRgb(234, 179,  8))    // amarelo
                                                       : new SolidColorBrush(Color.FromRgb(239,  68, 68));   // vermelho

    public Brush ActionColor  => HasAction ? new SolidColorBrush(Color.FromRgb(203, 213, 225))  // slate-300
                                           : new SolidColorBrush(Color.FromRgb(100, 116, 139)); // slate-500 (dim)

    public Brush CategoryColor => Category switch
    {
        "Transport"  => new SolidColorBrush(Color.FromArgb(30,  56, 189, 248)),
        "Pads"       => new SolidColorBrush(Color.FromArgb(30, 168,  85, 247)),
        "Mixer"      => new SolidColorBrush(Color.FromArgb(30, 234, 179,   8)),
        "Navigation" => new SolidColorBrush(Color.FromArgb(30,  34, 197,  94)),
        "Editing"    => new SolidColorBrush(Color.FromArgb(30, 249, 115,  22)),
        "BeatFX"     => new SolidColorBrush(Color.FromArgb(30, 239,  68,  68)),
        "JogWheel"   => new SolidColorBrush(Color.FromArgb(30, 148, 163, 184)),
        _            => new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
    };
}

public partial class SettingsWindow : Window
{
    // ── Eventos para comunicação com Program.cs ──
    public event Action<string>? MidiLearnRequested; // payload = controlId
    public event Action?         MidiLearnCancelled;

    private readonly List<ControlItem> _allItems = new();
    private readonly ObservableCollection<ControlItem> _displayItems = new();

    private bool _showShift = false;
    private string _categoryFilter = "Todas";
    private ControlItem? _selectedItem;
    private bool _isLearning = false;

    // Animação de pulso para o MIDI Learn
    private readonly DispatcherTimer _pulseTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private bool _pulseOn = true;

    public SettingsWindow()
    {
        InitializeComponent();
        ControlList.ItemsSource = _displayItems;

        _pulseTimer.Tick += (s, e) =>
        {
            LearnPulse.Fill = _pulseOn
                ? new SolidColorBrush(Color.FromRgb(56, 189, 248))
                : new SolidColorBrush(Color.FromArgb(60, 56, 189, 248));
            _pulseOn = !_pulseOn;
        };

        Loaded += (s, e) =>
        {
            LoadRegistry();
            RefreshList();
            HighlightTab();
        };
    }

    // ────────────────────────────────────────────────────────────────
    // Carregamento do control_registry.json
    // ────────────────────────────────────────────────────────────────
    private void LoadRegistry()
    {
        _allItems.Clear();

        string registryPath = FindFile("control_registry.json");
        string calibPath    = FindFile("mappings_calibrated.json");

        // Carrega calibração (sinal MIDI conhecido por controle)
        var calibData = new Dictionary<string, (byte status, byte data1, bool isCont)>();
        if (File.Exists(calibPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(calibPath));
                foreach (var p in doc.RootElement.GetProperty("Controls").EnumerateObject())
                {
                    byte st   = p.Value.GetProperty("Status").GetByte();
                    byte d1   = p.Value.GetProperty("Data1").GetByte();
                    bool cont = p.Value.TryGetProperty("IsContinuous", out var ic) && ic.GetBoolean();
                    calibData[p.Name] = (st, d1, cont);
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Settings] Erro ao ler calibração: {ex.Message}"); }
        }

        // Carrega registry
        if (!File.Exists(registryPath))
        {
            Console.WriteLine("[Settings] control_registry.json não encontrado.");
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(registryPath));
            foreach (var prop in doc.RootElement.GetProperty("controls").EnumerateObject())
            {
                var v = prop.Value;
                bool isShift    = v.TryGetProperty("isShift",    out var sh) && sh.GetBoolean();
                bool calibrated = v.TryGetProperty("calibrated", out var ca) && ca.GetBoolean();
                string? action  = v.TryGetProperty("action", out var ac) && ac.ValueKind != JsonValueKind.Null
                                    ? ac.GetString() : null;

                calibData.TryGetValue(prop.Name, out var calib);

                _allItems.Add(new ControlItem
                {
                    Id              = prop.Name,
                    DisplayName     = v.TryGetProperty("display",  out var dn) ? dn.GetString() ?? prop.Name : prop.Name,
                    Category        = v.TryGetProperty("category", out var ct) ? ct.GetString() ?? "Other"   : "Other",
                    IsShift         = isShift,
                    Calibrated      = calibrated || calibData.ContainsKey(prop.Name),
                    HasAction       = action != null,
                    ActionDescription = action ?? "— sem ação mapeada —",
                    MidiStatus      = calib.status,
                    MidiData1       = calib.data1,
                    IsContinuous    = calib.isCont,
                });
            }
        }
        catch (Exception ex) { Console.WriteLine($"[Settings] Erro ao ler registry: {ex.Message}"); }

        UpdateStats();
    }

    private void UpdateStats()
    {
        int mapped   = _allItems.Count(i => i.HasAction && i.Calibrated);
        int partial  = _allItems.Count(i => !i.HasAction && i.Calibrated);
        int noCalib  = _allItems.Count(i => !i.Calibrated);

        StatMapped.Text   = $"{mapped} mapeados";
        StatPartial.Text  = $"{partial} sem ação";
        StatUnmapped.Text = $"{noCalib} não calibrados";
    }

    private void RefreshList()
    {
        _displayItems.Clear();
        var query = _allItems
            .Where(i => i.IsShift == _showShift)
            .Where(i => _categoryFilter == "Todas" || i.Category == _categoryFilter)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.DisplayName);

        foreach (var item in query)
            _displayItems.Add(item);
    }

    // ────────────────────────────────────────────────────────────────
    // Chamado pelo Program.cs quando um sinal MIDI Learn é recebido
    // ────────────────────────────────────────────────────────────────
    public void OnMidiLearnCaptured(byte status, byte data1)
    {
        if (!_isLearning || _selectedItem == null) return;

        Dispatcher.Invoke(() =>
        {
            _selectedItem.MidiStatus  = status;
            _selectedItem.MidiData1   = data1;
            _selectedItem.Calibrated  = true;

            LearnStatus.Text = $"✔ Capturado: 0x{status:X2} / 0x{data1:X2} ({status}/{data1}) — pressione Confirmar ou Cancelar";
            BtnLearn.Content     = "✔ Confirmar";
            BtnLearn.Visibility  = Visibility.Visible;
            BtnCancelLearn.Visibility = Visibility.Visible;
        });
    }

    // ────────────────────────────────────────────────────────────────
    // Tab & Filter handlers
    // ────────────────────────────────────────────────────────────────
    private void BtnTabPure_Click(object sender, RoutedEventArgs e)  { _showShift = false; RefreshList(); HighlightTab(); }
    private void BtnTabShift_Click(object sender, RoutedEventArgs e) { _showShift = true;  RefreshList(); HighlightTab(); }

    private void HighlightTab()
    {
        var active   = new SolidColorBrush(Color.FromArgb(30,  56, 189, 248));
        var inactive = Brushes.Transparent;
        var activeFg   = new SolidColorBrush(Color.FromRgb(56, 189, 248));
        var inactiveFg = new SolidColorBrush(Color.FromRgb(100, 116, 139));

        BtnTabPure.Background  = _showShift ? inactive : active;
        BtnTabShift.Background = _showShift ? active   : inactive;
        BtnTabPure.Foreground  = _showShift ? inactiveFg : activeFg;
        BtnTabShift.Foreground = _showShift ? activeFg   : inactiveFg;
    }

    private void CategoryFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        _categoryFilter = (CategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todas";
        RefreshList();
    }

    // ────────────────────────────────────────────────────────────────
    // Seleção na lista
    // ────────────────────────────────────────────────────────────────
    private void ControlList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ControlList.SelectedItem is not ControlItem item) return;
        _selectedItem = item;

        if (_isLearning) return; // ignora troca de seleção durante aprendizado

        // Atualiza painel de detalhe
        DetailTitle.Text = item.DisplayName;
        DetailMidi.Text  = item.Calibrated
            ? $"MIDI: Status=0x{item.MidiStatus:X2}({item.MidiStatus})  Data1=0x{item.MidiData1:X2}({item.MidiData1})  {(item.IsContinuous ? "Contínuo" : "Botão")}"
            : "MIDI: Não calibrado";

        // Mostra botão de MIDI Learn se não tem calibração ou tem calibração mas quer re-mapear
        BtnLearn.Visibility      = Visibility.Visible;
        BtnCancelLearn.Visibility = Visibility.Collapsed;
        BtnLearn.Content         = item.Calibrated ? "↺  Re-mapear" : "⊕  Aprender Botão";
    }

    // ────────────────────────────────────────────────────────────────
    // MIDI Learn
    // ────────────────────────────────────────────────────────────────
    private void BtnLearn_Click(object sender, RoutedEventArgs e)
    {
        // Se já capturou (botão virou "Confirmar"), salva e sai do modo
        if (_isLearning && BtnLearn.Content?.ToString()?.StartsWith("✔") == true)
        {
            CommitLearn();
            return;
        }

        if (_selectedItem == null) return;

        _isLearning = true;

        // UI: entra no modo learn
        DetailPanel.Visibility   = Visibility.Collapsed;
        MidiLearnPanel.Visibility = Visibility.Visible;
        BtnCancelLearn.Visibility = Visibility.Visible;
        LearnStatus.Text = $"Aguardando sinal da controladora para '{_selectedItem.DisplayName}'...";
        BtnLearn.Visibility = Visibility.Collapsed;

        _pulseTimer.Start();

        MidiLearnRequested?.Invoke(_selectedItem.Id);
    }

    private void BtnCancelLearn_Click(object sender, RoutedEventArgs e)
    {
        CancelLearn();
    }

    private void CommitLearn()
    {
        if (_selectedItem == null) return;

        // Salva no registry em memória
        _selectedItem.Calibrated = true;
        RefreshList();
        UpdateStats();

        // Persistir no mappings_calibrated.json
        SaveCalibrationEntry(_selectedItem);

        CancelLearn();

        DetailTitle.Text = $"✔ '{_selectedItem.DisplayName}' mapeado com sucesso!";
        DetailMidi.Text  = $"MIDI: 0x{_selectedItem.MidiStatus:X2} / 0x{_selectedItem.MidiData1:X2}";
    }

    private void CancelLearn()
    {
        _isLearning = false;
        _pulseTimer.Stop();
        LearnPulse.Fill = new SolidColorBrush(Color.FromRgb(56, 189, 248));

        DetailPanel.Visibility    = Visibility.Visible;
        MidiLearnPanel.Visibility = Visibility.Collapsed;
        BtnCancelLearn.Visibility = Visibility.Collapsed;
        BtnLearn.Visibility       = Visibility.Visible;
        BtnLearn.Content          = "⊕  Aprender Botão";

        MidiLearnCancelled?.Invoke();
    }

    private void SaveCalibrationEntry(ControlItem item)
    {
        try
        {
            string path = FindFile("mappings_calibrated.json");
            if (!File.Exists(path)) return;

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            // Reconstrói o dict e atualiza a entrada
            var dict = new Dictionary<string, object>();
            foreach (var p in doc.RootElement.GetProperty("Controls").EnumerateObject())
                dict[p.Name] = new { Status = p.Value.GetProperty("Status").GetByte(), Data1 = p.Value.GetProperty("Data1").GetByte(), IsContinuous = p.Value.TryGetProperty("IsContinuous", out var ic) && ic.GetBoolean(), IsShiftCombo = p.Value.TryGetProperty("IsShiftCombo", out var sc) && sc.GetBoolean() };

            dict[item.Id] = new { Status = item.MidiStatus, Data1 = item.MidiData1, IsContinuous = item.IsContinuous, IsShiftCombo = item.IsShift };

            var date = doc.RootElement.TryGetProperty("CalibrationDate", out var d) ? d.GetString() ?? "" : "";
            var json = System.Text.Json.JsonSerializer.Serialize(new { CalibrationDate = date, Controls = dict },
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            Console.WriteLine($"[Settings] Calibração salva: {item.Id} = 0x{item.MidiStatus:X2}/0x{item.MidiData1:X2}");
        }
        catch (Exception ex) { Console.WriteLine($"[Settings] Erro ao salvar calibração: {ex.Message}"); }
    }

    // ────────────────────────────────────────────────────────────────
    // Utilitários
    // ────────────────────────────────────────────────────────────────
    private static string FindFile(string filename)
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            string candidate = Path.Combine(dir, "docs", filename);
            if (File.Exists(candidate)) return candidate;
            dir = Directory.GetParent(dir)?.FullName ?? "";
        }
        return Path.Combine("docs", filename);
    }

    private void BtnCloseSettings_Click(object sender, RoutedEventArgs e) => Close();
}
