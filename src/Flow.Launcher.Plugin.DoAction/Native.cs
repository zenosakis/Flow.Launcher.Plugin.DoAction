using DoAction.Core;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Flow.Launcher.Plugin.DoAction;

internal static class Native
{
    internal static readonly UIntPtr Marker = new(0x444F4143u);
    internal delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] internal struct KeyboardEvent
    { public uint Key, Scan, Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct Message
    { public IntPtr Window; public uint Id; public UIntPtr WParam; public IntPtr LParam; public uint Time; public Point Point; public uint Private; }
    [StructLayout(LayoutKind.Sequential)] internal struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion
    { [FieldOffset(0)] public MouseInput Mouse; [FieldOffset(0)] public KeyInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseInput
    { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct KeyInput
    { public ushort Key, Scan; public uint Flags, Time; public UIntPtr Extra; }

    [DllImport("user32.dll", SetLastError = true)] internal static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string? name);
    [DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] internal static extern int GetMessage(out Message message, IntPtr window, uint min, uint max);
    [DllImport("user32.dll")] internal static extern bool PeekMessage(out Message message, IntPtr window, uint min, uint max, uint remove);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool PostThreadMessage(uint thread, uint message, UIntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);

    internal static void SendPair(Input down, Input up)
    {
        var inputs = new[] { down, up };
        if (SendInput(2, inputs, Marshal.SizeOf<Input>()) != 2)
        {
            // A partial submission must not leave a key/button held down.
            SendInput(1, new[] { up }, Marshal.SizeOf<Input>());
            throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("Unable to send input. Check the target application's permissions.", "입력을 전달하지 못했습니다. 대상 앱의 권한을 확인해 주세요."));
        }
    }

    internal static void Key(ushort key, bool unicode)
    {
        var down = new Input { Type = 1, Data = new InputUnion { Keyboard = new KeyInput
        { Key = unicode ? (ushort)0 : key, Scan = unicode ? key : (ushort)0, Flags = unicode ? 4u : 0u, Extra = Marker } } };
        var up = down;
        up.Data.Keyboard.Flags |= 2;
        SendPair(down, up);
    }

    internal static void Click(bool right)
    {
        var down = new Input { Data = new InputUnion { Mouse = new MouseInput { Flags = right ? 8u : 2u, Extra = Marker } } };
        var up = down;
        up.Data.Mouse.Flags = right ? 16u : 4u;
        SendPair(down, up);
    }
}
