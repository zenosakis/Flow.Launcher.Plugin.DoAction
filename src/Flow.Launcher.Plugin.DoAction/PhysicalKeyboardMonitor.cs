using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DoAction.Core;

namespace Flow.Launcher.Plugin.DoAction;

internal sealed class PhysicalKeyboardMonitor : IDisposable
{
    private readonly Action stop;
    private readonly Thread thread;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Native.HookProc callback;
    private PhysicalKeyGate? gate;
    private IntPtr hook;
    private uint threadId;
    private volatile bool released;
    private int stopped;
    private volatile bool disposing;
    public bool InitialKeysReleased => released;

    public PhysicalKeyboardMonitor(Action stop)
    {
        this.stop = stop;
        callback = OnKey;
        thread = new Thread(Run) { IsBackground = true, Name = "DoAction physical keyboard monitor" };
        thread.Start();
    }

    public Task Ready => ready.Task;

    private void Run()
    {
        try
        {
            threadId = Native.GetCurrentThreadId();
            Native.PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
            var held = new List<int>();
            for (var key = 8; key < 256; key++)
                // The hook reports left/right modifier VKs, not their generic aliases.
                if (key is not (0x10 or 0x11 or 0x12))
                if ((Native.GetAsyncKeyState(key) & 0x8000) != 0) held.Add(key);
            gate = new PhysicalKeyGate(held);
            released = gate.InitialKeysReleased;
            hook = Native.SetWindowsHookEx(13, callback, Native.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("Unable to monitor the physical keyboard for stopping.", "키보드 중지 감시를 시작하지 못했습니다."));
            ready.TrySetResult();
            while (Native.GetMessage(out _, IntPtr.Zero, 0, 0) > 0) { }
        }
        catch (Exception ex) { ready.TrySetException(ex); }
        finally
        {
            if (hook != IntPtr.Zero) Native.UnhookWindowsHookEx(hook);
            if (!disposing && ready.Task.IsCompletedSuccessfully) stop();
        }
    }

    private IntPtr OnKey(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && gate != null)
        {
            var key = Marshal.PtrToStructure<Native.KeyboardEvent>(data);
            var down = message.ToInt64() is 0x100 or 0x104;
            var injected = (key.Flags & 0x10) != 0;
            if (gate.ShouldStop((int)key.Key, down, injected) && Interlocked.Exchange(ref stopped, 1) == 0)
                stop(); // Only cancellation flags; no UI, waits, or input injection in the hook.
            released = gate.InitialKeysReleased;
        }
        return Native.CallNextHookEx(hook, code, message, data);
    }

    public void Dispose()
    {
        disposing = true;
        if (threadId != 0) Native.PostThreadMessage(threadId, 0x12, UIntPtr.Zero, IntPtr.Zero);
        if (Thread.CurrentThread != thread) thread.Join(TimeSpan.FromSeconds(2));
        GC.KeepAlive(callback);
    }
}
