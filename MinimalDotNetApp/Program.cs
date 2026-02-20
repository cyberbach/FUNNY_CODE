using System.Runtime.InteropServices;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    static extern nint DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    static extern nint DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll")]
    static extern bool PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int DrawText(IntPtr hDC, string lpStr, int nCount, ref RECT lpRect, uint uFormat);

    [DllImport("user32.dll")]
    static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

    [DllImport("user32.dll")]
    static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

    [DllImport("gdi32.dll")]
    static extern IntPtr GetStockObject(int fnObject);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetModuleHandle(string? lpModuleName);

    const int CW_USEDEFAULT = unchecked((int)0x80000000);
    const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    const uint WS_VISIBLE = 0x10000000;
    const int SW_SHOW = 5;
    const uint WM_DESTROY = 0x0002;
    const uint WM_PAINT = 0x000F;
    const uint WM_NCHITTEST = 0x0084;
    const uint WM_SIZE = 0x0005;
    const nint HTCAPTION = 2;
    const nint HTCLIENT = 1;
    const uint DT_CENTER = 0x00000001;
    const uint DT_VCENTER = 0x00000004;
    const uint DT_SINGLELINE = 0x00000020;
    const int DEFAULT_GUI_FONT = 17;

    [StructLayout(LayoutKind.Sequential)]
    struct WNDCLASS
    {
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string? lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[]? rgbReserved;
    }

    static WndProcDelegate? _wndProcDelegate;

    static nint WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        switch (uMsg)
        {
            case WM_PAINT:
                var ps = new PAINTSTRUCT();
                IntPtr hDC = BeginPaint(hWnd, out ps);
                GetClientRect(hWnd, out RECT rect);
                GetStockObject(DEFAULT_GUI_FONT);
                DrawText(hDC, "минимальное приложение", -1, ref rect, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
                EndPaint(hWnd, ref ps);
                return 0;

            case WM_NCHITTEST:
                nint result = DefWindowProc(hWnd, uMsg, wParam, lParam);
                if (result == HTCLIENT)
                    return HTCAPTION;
                return result;

            case WM_SIZE:
                InvalidateRect(hWnd, IntPtr.Zero, true);
                return 0;

            case WM_DESTROY:
                PostQuitMessage(0);
                return 0;
        }
        return DefWindowProc(hWnd, uMsg, wParam, lParam);
    }

    delegate nint WndProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    static void Main()
    {
        IntPtr hInstance = GetModuleHandle(null);

        _wndProcDelegate = WndProc;

        var wc = new WNDCLASS
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = hInstance,
            lpszClassName = "MinimalWindow",
            hbrBackground = (IntPtr)(6 + 1)
        };

        RegisterClass(ref wc);

        IntPtr hWnd = CreateWindowEx(0, "MinimalWindow", "MinimalDotNetApp",
            WS_OVERLAPPEDWINDOW | WS_VISIBLE,
            CW_USEDEFAULT, CW_USEDEFAULT, 400, 200,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        ShowWindow(hWnd, SW_SHOW);
        UpdateWindow(hWnd);

        while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }
}
