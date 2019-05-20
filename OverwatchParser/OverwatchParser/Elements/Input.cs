using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;

namespace OverwatchParser
{
    public class InputSim
    {
        public static Process OverwatchProcess = Process.GetProcessesByName("Overwatch")[0];
        private static IntPtr Handle { get { return OverwatchProcess.MainWindowHandle; } }

        public const int SmallStep = 35;
        public const int MediumStep = 150;
        public const int BigStep = 500;

        const uint WM_KEYDOWN = 0x100;
        const uint WM_KEYUP = 0x0101;

        const int WM_UNICHAR = 0x0109; // Used by TextInput()

        const int WM_SYSKEYDOWN = 0x0104;
        const int WM_SYSKEYUP = 0x0105;

        public static void NumberInput(double value)
        {
            Keys[] numberKeys = new Keys[] { Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9 };

            List<Keys> keys = new List<Keys>();

            string get = value.ToString();
            for (int i = 0; i < get.Length; i++)
                if (get[i] == '-')
                    keys.Add(Keys.Subtract);
                else if (get[i] == '.')
                    keys.Add(Keys.OemPeriod);
                else
                    keys.Add(numberKeys[Int32.Parse(get[i].ToString())]);

            for (int i = 0; i < keys.Count; i++)
            {
                //using (Bitmap beforePress = BitBlt())
                //{
                    KeyDown(keys[i]);
                    Thread.Sleep(MsFromWait(Wait.Short));
                  //  WaitForImageChange(beforePress, MsFromWait(Wait.Short));
                //}
            }
        }

        public static void Press(Keys key, Wait wait, int count = 1)
        {
            //using (Bitmap beforePress = BitBlt())
            //{
            for (int i = 0; i < count; i++)
            {
                KeyPress(key);
                Thread.Sleep(MsFromWait(wait));
            }
              //  WaitForImageChange(beforePress, MsFromWait(wait));
            //}
        }

        // Text input
        public static void TextInput(string text, Wait wait)
        {
            //using (Bitmap beforeType = BitBlt())
            //{
                for (int i = 0; i < text.Length; i++)
                {
                    char letter = text[i];
                    User32.PostMessage(Handle, WM_UNICHAR, letter, 0);
                }
                Thread.Sleep(MsFromWait(wait));
                //WaitForImageChange(beforeType, MsFromWait(wait));
            //}
        }

        public static void SelectEnumMenuOption(Type enumType, object enumValue)
        {
            Array enumValues = Enum.GetValues(enumType);

            if (!enumValues.GetValue(0).Equals(enumValue))
            {
                int enumPos = Array.IndexOf(enumValues, enumValue);

                KeyPress(Keys.Space);
                Thread.Sleep(MediumStep);
                KeyPress(Keys.Enter);
                Thread.Sleep(MediumStep);

                Press(Keys.Down, Wait.Short, enumPos);

                KeyPress(Keys.Space);
                Thread.Sleep(MediumStep);
            }
        }

        // Some of Overwatch's input will not work unless Activate() is called beforehand.
        // The known instances are Opening chat and going to lobby after starting/restarting a game.
        public static void Activate()
        {
            User32.PostMessage(Handle, 0x0086, 1, 0); // 0x0086 = WM_NCACTIVATE
            User32.PostMessage(Handle, 0x0007, 0, 0); // 0x0007 = WM_DEVICECHANGE
        }

        public static void SelectEnumMenuOption<T>(T enumValue) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            SelectEnumMenuOption(typeof(T), enumValue);
        }

        public static void WaitForNextUpdate(Wait wait)
        {
            using (Bitmap beforePress = BitBlt())
            {
                Thread.Sleep(MsFromWait(wait));
                WaitForImageChange(beforePress, MsFromWait(wait));
            }
        }

        private static bool WaitForImageChange(Bitmap bmp, int maxTime)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (bmp)
                while (sw.ElapsedMilliseconds <= maxTime)
                    using (Bitmap current = BitBlt())
                    {
                        if (!CompareMemCmp(bmp, current))
                            return true;
                        Thread.Sleep(SmallStep);
                    }
            return false;
        }

        private static bool CompareMemCmp(Bitmap b1, Bitmap b2)
        {
            if ((b1 == null) != (b2 == null)) return false;
            if (b1.Size != b2.Size) return false;

            var bd1 = b1.LockBits(new Rectangle(new Point(0, 0), b1.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bd2 = b2.LockBits(new Rectangle(new Point(0, 0), b2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                IntPtr bd1scan0 = bd1.Scan0;
                IntPtr bd2scan0 = bd2.Scan0;

                int stride = bd1.Stride;
                int len = stride * b1.Height;

                return Msvcrt.memcmp(bd1scan0, bd2scan0, len) == 0;
            }
            finally
            {
                b1.UnlockBits(bd1);
                b2.UnlockBits(bd2);
            }
        }

        private static int MsFromWait(Wait wait)
        {
            switch (wait)
            {
                case Wait.Short:
                    return SmallStep;
                case Wait.Medium:
                    return MediumStep;
                case Wait.Long:
                    return BigStep;
            }
            return 0;
        }

        // Key Press
        private static void KeyPress(Keys key)
        {
            User32.PostMessage(Handle, WM_KEYDOWN, (IntPtr)key, IntPtr.Zero);
            User32.PostMessage(Handle, WM_KEYUP, (IntPtr)key, IntPtr.Zero);
        }

        // Key Down
        private static void KeyDown(Keys key)
        {
            User32.PostMessage(Handle, WM_KEYDOWN, (IntPtr)key, IntPtr.Zero);
        }

        // Key Up
        private static void KeyUp(Keys key)
        {
            User32.PostMessage(Handle, WM_KEYUP, (IntPtr)key, IntPtr.Zero);
        }

        // Alternate Key Input
        private static void AlternateInput(int keycode)
        {
            User32.PostMessage(Handle, WM_KEYDOWN, keycode, 0);
            User32.PostMessage(Handle, WM_KEYUP, keycode, 0);
        }

        public static void Alt(Keys key)
        {
            Thread.Sleep(25);
            User32.PostMessage(Handle, WM_SYSKEYDOWN, 0x12, 1);
            User32.PostMessage(Handle, WM_SYSKEYUP, (uint)key, 1);
            User32.PostMessage(Handle, WM_KEYUP, 0x12, 0);
        }

        public static string GetClipboard()
        {
            string clipboardText = null;
            Thread getClipboardThread = new Thread(() => clipboardText = Clipboard.GetText());
            getClipboardThread.SetApartmentState(ApartmentState.STA);
            getClipboardThread.Start();
            getClipboardThread.Join();
            return clipboardText;
        }

        public static void SetClipboard(string text)
        {
            Thread setClipboardThread = new Thread(() => Clipboard.SetText(text));
            setClipboardThread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            setClipboardThread.Start();
            setClipboardThread.Join();
        }

        public static void SelectAll()
        {
            KeyDown(Keys.LControlKey);
            KeyDown(Keys.A);
            KeyUp(Keys.LControlKey);
        }

        public static void Copy()
        {
            KeyDown(Keys.LControlKey);
            KeyDown(Keys.C);
            KeyUp(Keys.LControlKey);
        }

        private static Bitmap BitBlt()
        {
            try
            {
                // get the hDC of the target window
                IntPtr hdcSrc = User32.GetDC(Handle);
                // get the size
                Rectangle rectangle = new Rectangle();
                User32.GetWindowRect(Handle, ref rectangle);
                int width = rectangle.Width;
                int height = rectangle.Height;
                // create a device context we can copy to
                IntPtr hdcDest = Gdi32.CreateCompatibleDC(hdcSrc);
                // create a bitmap we can copy it to,
                // using GetDeviceCaps to get the width/height
                IntPtr hBitmap = Gdi32.CreateCompatibleBitmap(hdcSrc, width, height);
                // select the bitmap object
                IntPtr hOld = Gdi32.SelectObject(hdcDest, hBitmap);
                // bitblt over
                Gdi32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, (uint)Gdi32.TernaryRasterOperations.SRCCOPY | (uint)Gdi32.TernaryRasterOperations.CAPTUREBLT);
                // restore selection
                Gdi32.SelectObject(hdcDest, hOld);

                Bitmap bmp = Image.FromHbitmap(hBitmap);

                // clean up 
                Gdi32.DeleteDC(hdcDest);
                User32.ReleaseDC(Handle, hdcSrc);
                // free up the Bitmap object
                Gdi32.DeleteObject(hBitmap);

                return bmp;
            }
            catch (ExternalException ex)
            {
                // Failed to capture window, usually because it was closed.
#warning todo: error handle this bad boy
                throw ex;
            }
        }
    }

    public enum Wait
    {
        Short,
        Medium,
        Long
    }
}
