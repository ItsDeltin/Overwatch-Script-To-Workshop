using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

namespace OverwatchParser
{
    public class InputHandler
    {
        public const int SmallStep = 25;
        public const int MediumStep = 50;
        public const int BigStep = 250;

        public InputHandler(Process overwatchProcess)
        {
            OverwatchHandle = overwatchProcess.MainWindowHandle;
        }

        IntPtr OverwatchHandle;

        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;

        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_RBUTTONUP = 0x0205;

        const int WM_MOUSEMOVE = 0x0200;

        const int WM_ACTIVATE = 0x0006;

        const uint WM_KEYDOWN = 0x100;
        const uint WM_KEYUP = 0x0101;

        const int WM_CHAR = 0x0102;
        const int WM_UNICHAR = 0x0109;

        const int WM_SYSKEYDOWN = 0x0104;
        const int WM_SYSKEYUP = 0x0105;

        public static Keys[] GetNumberKeys(int value)
        {
            Keys[] numberKeys = new Keys[] { Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9 };

            List<Keys> keys = new List<Keys>();

            string get = value.ToString();
            for (int i = 0; i < get.Length; i++)
                if (get[i] == '-')
                    keys.Add(Keys.Subtract);
                else
                    keys.Add(numberKeys[Int32.Parse(get[i].ToString())]);

            return keys.ToArray();
        }

        // Some of Overwatch's input will not work unless Activate() is called beforehand.
        // The known instances are Opening chat and going to lobby after starting/restarting a game.
        public void Activate()
        {
            User32.PostMessage(OverwatchHandle, 0x0086, 1, 0); // 0x0086 = WM_NCACTIVATE
            User32.PostMessage(OverwatchHandle, 0x0007, 0, 0); // 0x0007 = WM_DEVICECHANGE
        }

        private void ScreenToClient(ref int x, ref int y)
        {
            Point p = new Point(x, y);
            User32.ScreenToClient(OverwatchHandle, ref p);
            x = p.X;
            y = p.Y;
        }

        private static int MakeLParam(int LoWord, int HiWord)
        {
            return (int)((HiWord << 16) | (LoWord & 0xFFFF));
        }

        // Left Click
        internal void LeftClick(int x, int y, int waitTime = 500)
        {
            ScreenToClient(ref x, ref y);

            User32.PostMessage(OverwatchHandle, WM_ACTIVATE, 2, 0);
            User32.PostMessage(OverwatchHandle, WM_MOUSEMOVE, 0, MakeLParam(x, y));
            User32.PostMessage(OverwatchHandle, WM_LBUTTONDOWN, 0, MakeLParam(x, y));
            User32.PostMessage(OverwatchHandle, WM_LBUTTONUP, 0, MakeLParam(x, y));
            Thread.Sleep(waitTime);
        }
        internal void LeftClick(Point point, int waitTime = 500) => LeftClick(point.X, point.Y, waitTime);

        // Right Click
        internal void RightClick(int x, int y, int waitTime = 500)
        {
            ScreenToClient(ref x, ref y);

            User32.PostMessage(OverwatchHandle, WM_ACTIVATE, 2, 0);
            User32.PostMessage(OverwatchHandle, WM_MOUSEMOVE, 0, MakeLParam(x, y));
            Thread.Sleep(100);
            User32.PostMessage(OverwatchHandle, WM_RBUTTONDOWN, 0, MakeLParam(x, y));
            User32.PostMessage(OverwatchHandle, WM_RBUTTONUP, 0, MakeLParam(x, y));
            Thread.Sleep(waitTime);
        }
        internal void RightClick(Point point, int waitTime = 500) => RightClick(point.X, point.Y, waitTime);

        // Move Mouse
        internal void MoveMouseTo(int x, int y)
        {
            ScreenToClient(ref x, ref y);
            User32.PostMessage(OverwatchHandle, WM_MOUSEMOVE, 0, MakeLParam(x, y));
        }
        internal void MoveMouseTo(Point point) => MoveMouseTo(point.X, point.Y);

        // Key Press
        internal void KeyPress(params Keys[] keys)
        {
            foreach (Keys key in keys)
            {
                User32.PostMessage(OverwatchHandle, WM_KEYDOWN, (IntPtr)(key), IntPtr.Zero);
                User32.PostMessage(OverwatchHandle, WM_KEYUP, (IntPtr)(key), IntPtr.Zero);
            }
        }

        // Key Down
        internal void KeyDown(params Keys[] keysToSend)
        {
            foreach (Keys key in keysToSend)
            {
                User32.PostMessage(OverwatchHandle, WM_KEYDOWN, (IntPtr)(key), IntPtr.Zero);
            }
        }

        // Key Up
        internal void KeyUp(params Keys[] keysToSend)
        {
            foreach (Keys key in keysToSend)
            {
                User32.PostMessage(OverwatchHandle, WM_KEYUP, (IntPtr)(key), IntPtr.Zero);
            }
        }

        // Alternate Key Input
        internal void AlternateInput(int keycode)
        {
            User32.PostMessage(OverwatchHandle, WM_KEYDOWN, keycode, 0);
            User32.PostMessage(OverwatchHandle, WM_KEYUP, keycode, 0);
        }

        // Text input
        internal void TextInput(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char letter = text[i];
                User32.PostMessage(OverwatchHandle, WM_UNICHAR, (int)letter, 0);
            }
        }

        // Alt
        internal void Alt(Keys key)
        {
            Activate();
            Thread.Sleep(25);
            User32.PostMessage(OverwatchHandle, WM_SYSKEYDOWN, 0x12, 1);
            User32.PostMessage(OverwatchHandle, WM_SYSKEYUP, (uint)key, 1);
            User32.PostMessage(OverwatchHandle, WM_KEYUP, 0x12, 0);
        }

        // Clipboard
        internal static string GetClipboard()
        {
            string clipboardText = null;
            Thread getClipboardThread = new Thread(() => clipboardText = Clipboard.GetText());
            getClipboardThread.SetApartmentState(ApartmentState.STA);
            getClipboardThread.Start();
            getClipboardThread.Join();
            return clipboardText;
        }
        internal static void SetClipboard(string text)
        {
            Thread setClipboardThread = new Thread(() => Clipboard.SetText(text));
            setClipboardThread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            setClipboardThread.Start();
            setClipboardThread.Join();
        }

        internal void SelectAll()
        {
            KeyDown(Keys.LControlKey);
            KeyDown(Keys.A);
            KeyUp(Keys.LControlKey);
        }

        internal void Copy()
        {
            KeyDown(Keys.LControlKey);
            KeyDown(Keys.C);
            KeyUp(Keys.LControlKey);
        }
    }

    internal static class User32
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rectangle rect);
        [DllImport("user32.dll")]
        internal static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        internal static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        internal static extern bool PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);
        [DllImport("user32.dll")]
        internal static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);
    }
}
