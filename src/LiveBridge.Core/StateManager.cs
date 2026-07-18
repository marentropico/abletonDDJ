using LiveBridge.Core.Models;

namespace LiveBridge.Core;

/// <summary>
/// Gerencia o estado global da controladora: Shift ativo, modo de cada deck, etc.
/// Thread-safe via lock simples.
/// </summary>
public class StateManager
{
    private readonly object _lock = new();

    private bool _isShiftLeftActive;
    private bool _isShiftRightActive;
    private string _modeLeft = "SessionBrowser";
    private string _modeRight = "Mixing";
    private int _currentOctave = 3;

    public bool IsShiftActive
    {
        get { lock (_lock) return _isShiftLeftActive; }
    }

    public int CurrentOctave
    {
        get { lock (_lock) return _currentOctave; }
    }

    public void OctaveUp()
    {
        lock (_lock) { if (_currentOctave < 8) _currentOctave++; }
        Console.WriteLine($"[StateManager] Oitava: {_currentOctave}");
    }

    public void OctaveDown()
    {
        lock (_lock) { if (_currentOctave > 0) _currentOctave--; }
        Console.WriteLine($"[StateManager] Oitava: {_currentOctave}");
    }

    public string ModeLeft
    {
        get { lock (_lock) return _modeLeft; }
    }

    public string ModeRight
    {
        get { lock (_lock) return _modeRight; }
    }

    public void SetShift(PhysicalControl shiftControl, bool isPressed)
    {
        lock (_lock)
        {
            if (shiftControl == PhysicalControl.Shift_Left)
                _isShiftLeftActive = isPressed;
            else if (shiftControl == PhysicalControl.Shift_Right)
                _isShiftRightActive = isPressed;
        }
        Console.WriteLine($"[StateManager] Shift => Left:{_isShiftLeftActive} Right:{_isShiftRightActive}");
    }

    public void SetMode(string deck, string mode)
    {
        lock (_lock)
        {
            if (deck == "Left") _modeLeft = mode;
            else if (deck == "Right") _modeRight = mode;
        }
        Console.WriteLine($"[StateManager] Modo do Deck {deck} alterado para: {mode}");
    }

    public string GetMode(PhysicalControl control)
    {
        // Controles do lado esquerdo retornam o modo do deck esquerdo
        bool isLeft = control.ToString().EndsWith("_Left") ||
                      control == PhysicalControl.BrowseEncoder_Turn ||
                      control == PhysicalControl.BrowseEncoder_Click ||
                      control == PhysicalControl.Load_Left;
        return isLeft ? ModeLeft : ModeRight;
    }
}
