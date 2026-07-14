using System;
using System.Runtime.InteropServices;

namespace LiveBridge.Core;

public static class KeyboardSimulator
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const uint KEYEVENTF_KEYUP = 0x0002;
    
    private const byte VK_UP = 0x26;
    private const byte VK_DOWN = 0x28;
    private const byte VK_LEFT = 0x25;
    private const byte VK_RIGHT = 0x27;
    private const byte VK_RETURN = 0x0D;
    
    private const byte VK_CONTROL = 0x11;
    private const byte VK_SHIFT_KEY = 0x10;
    private const byte VK_T = 0x54;

    public static void SendUp()
    {
        keybd_event(VK_UP, 0, 0, UIntPtr.Zero);
        keybd_event(VK_UP, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendDown()
    {
        keybd_event(VK_DOWN, 0, 0, UIntPtr.Zero);
        keybd_event(VK_DOWN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendLeft()
    {
        keybd_event(VK_LEFT, 0, 0, UIntPtr.Zero);
        keybd_event(VK_LEFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendRight()
    {
        keybd_event(VK_RIGHT, 0, 0, UIntPtr.Zero);
        keybd_event(VK_RIGHT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
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

    private const byte VK_OEM_PLUS = 0xBB;
    private const byte VK_OEM_MINUS = 0xBD;

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
    
    private const uint MOUSEEVENTF_HWHEEL = 0x01000;

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
}
