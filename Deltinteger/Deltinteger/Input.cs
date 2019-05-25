using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace Deltin.Deltinteger
{
    public static class InputSim
    {
        public static void SetClipboard(string text)
        {
            Thread setClipboardThread = new Thread(() => Clipboard.SetText(text));
            setClipboardThread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            setClipboardThread.Start();
            setClipboardThread.Join();
        }
    }
}
