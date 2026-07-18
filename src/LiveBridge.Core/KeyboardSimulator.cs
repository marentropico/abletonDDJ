using System;
using System.Runtime.InteropServices;

namespace LiveBridge.Core;

public static class KeyboardSimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_SHIFT = 0x10;

    public static bool IsShiftKeyDown()
    {
        return (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
    }

    public static void SendArrowKey(string direction, bool shift)
    {
        byte vKey = direction switch
        {
            "Left" => VK_LEFT,
            "Right" => VK_RIGHT,
            "Up" => VK_UP,
            "Down" => VK_DOWN,
            _ => 0
        };

        if (vKey == 0) return;

        keybd_event(vKey, 0, 0, UIntPtr.Zero);
        keybd_event(vKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    
    private const byte VK_UP = 0x26;
    private const byte VK_DOWN = 0x28;
    private const byte VK_LEFT = 0x25;
    private const byte VK_RIGHT = 0x27;
    private const byte VK_RETURN = 0x0D;
    
    private const byte VK_CONTROL = 0x11;
    private const byte VK_MENU = 0x12; // Alt key
    private const byte VK_SHIFT_KEY = 0x10;
    private const byte VK_T = 0x54;
    private const byte VK_D = 0x44;
    private const byte VK_DELETE = 0x2E;
    private const byte VK_Z = 0x5A;
    private const byte VK_Y = 0x59;
    private const byte VK_U = 0x55;
    private const byte VK_O = 0x4F;
    private const byte VK_L = 0x4C;

    public static void SendUp()
    {
        keybd_event(VK_UP, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
        keybd_event(VK_UP, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendDown()
    {
        keybd_event(VK_DOWN, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
        keybd_event(VK_DOWN, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendLeft()
    {
        keybd_event(VK_LEFT, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
        keybd_event(VK_LEFT, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendRight()
    {
        keybd_event(VK_RIGHT, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
        keybd_event(VK_RIGHT, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendEnter()
    {
        keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
        keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendCreateAudioTrack()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_T, 0, 0, UIntPtr.Zero);
        keybd_event(VK_T, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendCreateMidiTrack()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_SHIFT_KEY, 0, 0, UIntPtr.Zero);
        keybd_event(VK_T, 0, 0, UIntPtr.Zero);
        keybd_event(VK_T, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_SHIFT_KEY, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendConsolidate()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(0x4A, 0, 0, UIntPtr.Zero); // Tecla J
        keybd_event(0x4A, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendNarrowGrid()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(0x31, 0, 0, UIntPtr.Zero); // 1
        keybd_event(0x31, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendWidenGrid()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(0x32, 0, 0, UIntPtr.Zero); // 2
        keybd_event(0x32, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private const byte VK_OEM_PLUS = 0xBB;
    private const byte VK_OEM_MINUS = 0xBD;

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
    
    private const uint MOUSEEVENTF_HWHEEL = 0x01000;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    public static void SendMouseLeftDown()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
    }

    public static void SendMouseLeftUp()
    {
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    public static void SendShiftDown()
    {
        keybd_event(VK_SHIFT_KEY, 0, 0, UIntPtr.Zero);
    }

    public static void SendShiftUp()
    {
        keybd_event(VK_SHIFT_KEY, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendZoomIn()
    {
        keybd_event(VK_OEM_PLUS, 0, 0, UIntPtr.Zero);
        keybd_event(VK_OEM_PLUS, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendZoomOut()
    {
        keybd_event(VK_OEM_MINUS, 0, 0, UIntPtr.Zero);
        keybd_event(VK_OEM_MINUS, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void ScrollHorizontal(int clicks)
    {
        int amount = clicks * 120; // 120 = WHEEL_DELTA
        mouse_event(MOUSEEVENTF_HWHEEL, 0, 0, (uint)amount, UIntPtr.Zero);
    }

    public static void SendDuplicate()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_D, 0, 0, UIntPtr.Zero);
        keybd_event(VK_D, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendDelete()
    {
        keybd_event(VK_DELETE, 0, 0, UIntPtr.Zero);
        keybd_event(VK_DELETE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendUndo()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_Z, 0, 0, UIntPtr.Zero);
        keybd_event(VK_Z, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendRedo()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_Y, 0, 0, UIntPtr.Zero);
        keybd_event(VK_Y, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendQuantize()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_U, 0, 0, UIntPtr.Zero);
        keybd_event(VK_U, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendMetronome()
    {
        keybd_event(VK_O, 0, 0, UIntPtr.Zero);
        keybd_event(VK_O, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendSelectLoop()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_SHIFT_KEY, 0, 0, UIntPtr.Zero);
        keybd_event(VK_L, 0, 0, UIntPtr.Zero);
        keybd_event(VK_L, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_SHIFT_KEY, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendShiftLeftArrow()
    {
        keybd_event(VK_SHIFT_KEY, 0, 0, UIntPtr.Zero);
        keybd_event(VK_LEFT, 0, 0, UIntPtr.Zero);
        keybd_event(VK_LEFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_SHIFT_KEY, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendShiftRightArrow()
    {
        keybd_event(VK_SHIFT_KEY, 0, 0, UIntPtr.Zero);
        keybd_event(VK_RIGHT, 0, 0, UIntPtr.Zero);
        keybd_event(VK_RIGHT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_SHIFT_KEY, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendShiftUpArrow()
    {
        keybd_event(VK_SHIFT_KEY, 0, 0, UIntPtr.Zero);
        keybd_event(VK_UP, 0, 0, UIntPtr.Zero);
        keybd_event(VK_UP, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_SHIFT_KEY, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendShiftDownArrow()
    {
        keybd_event(VK_SHIFT_KEY, 0, 0, UIntPtr.Zero);
        keybd_event(VK_DOWN, 0, 0, UIntPtr.Zero);
        keybd_event(VK_DOWN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_SHIFT_KEY, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendSplitClip()
    {
        // Envia Ctrl + E (Split). É a melhor aproximação nativa para 
        // "selecionar" um trecho focado sem usar o mouse na timeline.
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(0x45, 0, 0, UIntPtr.Zero); // Tecla E
        keybd_event(0x45, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
