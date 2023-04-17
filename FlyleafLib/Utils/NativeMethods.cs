﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace FlyleafLib;

#pragma warning disable CA1401 // P/Invokes should not be visible
public static partial class Utils
{
    public static class NativeMethods
    {
        static NativeMethods()
        {
            if (IntPtr.Size == 4)
            {
                GetWindowLong = GetWindowLongPtr32;
                SetWindowLong = SetWindowLongPtr32;
            }
            else
            {
                GetWindowLong = GetWindowLongPtr64;
                SetWindowLong = SetWindowLongPtr64;
            }

            GetDPI(out DpiX, out DpiY);
        }

        public static Func<IntPtr, int, IntPtr, IntPtr> SetWindowLong;
        public static Func<IntPtr, int, IntPtr> GetWindowLong;

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] // , SetLastError = true
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("comctl32.dll")]
        public static extern bool SetWindowSubclass(IntPtr hWnd, IntPtr pfnSubclass, UIntPtr uIdSubclass, UIntPtr dwRefData);

        [DllImport("comctl32.dll")]
        public static extern bool RemoveWindowSubclass(IntPtr hWnd, IntPtr pfnSubclass, UIntPtr uIdSubclass);

        [DllImport("comctl32.dll")]
        public static extern IntPtr DefSubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, UInt32 uFlags);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);

        [DllImport("user32.dll")]
        public static extern int ShowCursor(bool bShow);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint uMilliseconds);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
        [FlagsAttribute]
        public enum EXECUTION_STATE :uint
        {
            ES_AWAYMODE_REQUIRED    = 0x00000040,
            ES_CONTINUOUS           = 0x80000000,
            ES_DISPLAY_REQUIRED     = 0x00000002,
            ES_SYSTEM_REQUIRED      = 0x00000001
        }

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("User32.dll")]
        public static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

        [DllImport("User32.dll")]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref RECT rectangle);

        [DllImport("user32.dll")]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        [DllImport("user32.dll")]
        public static extern void SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
            public WINDOWINFO(Boolean? filler) : this()
                => cbSize = (UInt32)Marshal.SizeOf(typeof(WINDOWINFO));

        }
        public struct RECT
        {
            public int Left     { get; set; }
            public int Top      { get; set; }
            public int Right    { get; set; }
            public int Bottom   { get; set; }
        }

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            SWP_ASYNCWINDOWPOS = 0x4000,
            SWP_DEFERERASE = 0x2000,
            SWP_DRAWFRAME = 0x0020,
            SWP_FRAMECHANGED = 0x0020,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOACTIVATE = 0x0010,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOMOVE = 0x0002,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOREDRAW = 0x0008,
            SWP_NOREPOSITION = 0x0200,
            SWP_NOSENDCHANGING = 0x0400,
            SWP_NOSIZE = 0x0001,
            SWP_NOZORDER = 0x0004,
            SWP_SHOWWINDOW = 0x0040,
        }

        [Flags]
        public enum WindowLongFlags : int
        {
             GWL_EXSTYLE = -20,
             GWLP_HINSTANCE = -6,
             GWLP_HWNDPARENT = -8,
             GWL_ID = -12,
             GWL_STYLE = -16,
             GWL_USERDATA = -21,
             GWL_WNDPROC = -4,
             DWLP_USER = 0x8,
             DWLP_MSGRESULT = 0x0,
             DWLP_DLGPROC = 0x4
        }

        [Flags]
        public enum WindowStyles : uint
        {
            WS_BORDER = 0x800000,
            WS_CAPTION = 0xc00000,
            WS_CHILD = 0x40000000,
            WS_CLIPCHILDREN = 0x2000000,
            WS_CLIPSIBLINGS = 0x4000000,
            WS_DISABLED = 0x8000000,
            WS_DLGFRAME = 0x400000,
            WS_GROUP = 0x20000,
            WS_HSCROLL = 0x100000,
            WS_MAXIMIZE = 0x1000000,
            WS_MAXIMIZEBOX = 0x10000,
            WS_MINIMIZE = 0x20000000,
            WS_MINIMIZEBOX = 0x20000,
            WS_OVERLAPPED = 0x0,
            WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_SIZEFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_POPUP = 0x80000000,
            WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
            WS_SIZEFRAME = 0x40000,
            WS_SYSMENU = 0x80000,
            WS_TABSTOP = 0x10000,
            WS_THICKFRAME = 0x00040000,
            WS_VISIBLE = 0x10000000,
            WS_VSCROLL = 0x200000
        }

        [Flags]
        public enum WindowStylesEx : uint
        {
            WS_EX_ACCEPTFILES = 0x00000010,
            WS_EX_APPWINDOW = 0x00040000,
            WS_EX_CLIENTEDGE = 0x00000200,
            WS_EX_COMPOSITED = 0x02000000,
            WS_EX_CONTEXTHELP = 0x00000400,
            WS_EX_CONTROLPARENT = 0x00010000,
            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_LAYERED = 0x00080000,
            WS_EX_LAYOUTRTL = 0x00400000,
            WS_EX_LEFT = 0x00000000,
            WS_EX_LEFTSCROLLBAR = 0x00004000,
            WS_EX_LTRREADING = 0x00000000,
            WS_EX_MDICHILD = 0x00000040,
            WS_EX_NOACTIVATE = 0x08000000,
            WS_EX_NOINHERITLAYOUT = 0x00100000,
            WS_EX_NOPARENTNOTIFY = 0x00000004,
            WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
            WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
            WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
            WS_EX_RIGHT = 0x00001000,
            WS_EX_RIGHTSCROLLBAR = 0x00000000,
            WS_EX_RTLREADING = 0x00002000,
            WS_EX_STATICEDGE = 0x00020000,
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_TOPMOST = 0x00000008,
            WS_EX_TRANSPARENT = 0x00000020,
            WS_EX_WINDOWEDGE = 0x00000100
        }

        public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        public delegate IntPtr SubclassWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);

        public static int SignedHIWORD(IntPtr n) => SignedHIWORD(unchecked((int)(long)n));
        public static int SignedLOWORD(IntPtr n) => SignedLOWORD(unchecked((int)(long)n));
        public static int SignedHIWORD(int n) => (short)((n >> 16) & 0xffff);
        public static int SignedLOWORD(int n) => (short)(n & 0xFFFF);

        #region DPI
        public static double DpiX, DpiY;
        const int LOGPIXELSX = 88, LOGPIXELSY = 90;
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int GetDeviceCaps(IntPtr hDC, int nIndex);
        public static void GetDPI(out double dpiX, out double dpiY) => GetDPI(IntPtr.Zero, out dpiX, out dpiY);
        public static void GetDPI(IntPtr handle, out double dpiX, out double dpiY)
        {
            Graphics GraphicsObject = Graphics.FromHwnd(handle); // DESKTOP Handle
            IntPtr dcHandle = GraphicsObject.GetHdc();
            dpiX = GetDeviceCaps(dcHandle, LOGPIXELSX) / 96.0;
            dpiY = GetDeviceCaps(dcHandle, LOGPIXELSY) / 96.0;
            GraphicsObject.ReleaseHdc(dcHandle);
            GraphicsObject.Dispose();
        }
        #endregion
    }
}
#pragma warning restore CA1401 // P/Invokes should not be visible
