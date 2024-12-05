using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;

namespace PardofelisCore.Util;

public class KeyboardHook : IDisposable
{
    #region Win32 API

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

    #endregion

    #region 常量和委托

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    #endregion

    #region 事件

    public event EventHandler<KeyEventArgs> KeyDown;
    public event EventHandler<KeyEventArgs> KeyUp;

    #endregion

    #region 字段

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private bool _isDisposed;
    private readonly object _lockObject = new object();

    #endregion

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    #region 公共方法

    public void Start()
    {
        if (_hookID == IntPtr.Zero)
        {
            _hookID = SetHook(_proc);
            if (_hookID == IntPtr.Zero)
            {
                throw new Exception($"键盘钩子设置失败，错误代码：{Marshal.GetLastWin32Error()}");
            }
        }
    }

    public void Stop()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    public void RunMessageLoop()
    {
        MSG msg;
        while (GetMessage(out msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    #endregion

    #region 私有方法

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                KeyDown?.Invoke(this, new KeyEventArgs((Keys)vkCode));
            }
            else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
            {
                KeyUp?.Invoke(this, new KeyEventArgs((Keys)vkCode));
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    #endregion

    #region IDisposable实现

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // 释放托管资源
                Stop();
            }

            _isDisposed = true;
        }
    }

    ~KeyboardHook()
    {
        Dispose(false);
    }

    #endregion

    #region 内部结构

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public System.Drawing.Point point;
    }

    #endregion
}

// 按键事件参数类
public class KeyEventArgs : EventArgs
{
    public Keys Key { get; }
    public DateTime Time { get; }

    public KeyEventArgs(Keys key)
    {
        Key = key;
        Time = DateTime.Now;
    }
}

// 按键枚举
public enum Keys
{
    Tab = 0x09,
    Escape = 0x1B,
    Enter = 0x0D,
    Space = 0x20,
    Left = 0x25,
    Up = 0x26,
    Right = 0x27,
    Down = 0x28,
    A = 0x41,
    B = 0x42,
    C = 0x43,
    D = 0x44,
    E = 0x45,
    F = 0x46,
    G = 0x47,
    H = 0x48,
    I = 0x49,
    J = 0x4A,
    K = 0x4B,
    L = 0x4C,
    M = 0x4D,
    N = 0x4E,
    O = 0x4F,
    P = 0x50,
    Q = 0x51,
    R = 0x52,
    S = 0x53,
    T = 0x54,
    U = 0x55,
    V = 0x56,
    W = 0x57,
    X = 0x58,
    Y = 0x59,
    Z = 0x5A,
    // 可以根据需要添加更多按键
}

/*class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("开始监测按键...");
        Console.WriteLine("按 ESC 键退出程序");

        using (var hook = new KeyboardHook())
        {
            // 订阅按键事件
            hook.KeyDown += Hook_KeyDown;
            hook.KeyUp += Hook_KeyUp;

            // 启动钩子
            hook.Start();

            // 运行消息循环
            hook.RunMessageLoop();
        }
    }

    private static void Hook_KeyDown(object sender, KeyEventArgs e)
    {
        Console.WriteLine($"[{e.Time:HH:mm:ss.fff}] 按下按键: {e.Key}");
        if (e.Key == Keys.Escape)
        {
            Environment.Exit(0);
        }
    }

    private static void Hook_KeyUp(object sender, KeyEventArgs e)
    {
        Console.WriteLine($"[{e.Time:HH:mm:ss.fff}] 释放按键: {e.Key}");
    }
}*/

public class KeyboardHookEntry
{
    public static KeyboardHook Hook = new KeyboardHook();
    
    public static void RunHookInOtherThread()
    {
            // 订阅按键事件
            // Hook.KeyDown += Hook_KeyDown;
            // Hook.KeyUp += Hook_KeyUp;

            // 启动钩子
            Hook.Start();

            // 运行消息循环

            var hookThread = new Thread(() =>
            {
                Hook.RunMessageLoop();
            });


            hookThread.Start();
    }

    private static void Hook_KeyDown(object sender, KeyEventArgs e)
    {
        Log.Information($"[{e.Time:HH:mm:ss.fff}] Key Pressed: {e.Key}");
    }

    private static void Hook_KeyUp(object sender, KeyEventArgs e)
    {
        Log.Information($"[{e.Time:HH:mm:ss.fff}] Key Released: {e.Key}");
    }
}
