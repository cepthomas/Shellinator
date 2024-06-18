using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;


// From https://github.com/MrM40/W-WinClipboard

// TODO clean up.

#pragma warning disable SYSLIB1054, CA1401, CA2101

namespace Win32BagOfTricks
{
    public static class Clipboard
    {
        // https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats
        public enum ClipboardFormats : int
        {
            CF_TEXT = 1,         // ANSI Text format. A null character signals the end of the data.
            CF_BITMAP = 2,       // A handle to a bitmap (HBITMAP).
            CF_WAVE = 12,        // Audio data in one of the standard wave formats.
            CF_UNICODETEXT = 13, // Unicode text format. A null character signals the end of the data.
        }

        #region API
        /// <summary>
        /// Get text from clipboard.
        /// </summary>
        /// <returns></returns>
        public static string? GetText()
        {
            IntPtr handle = default;
            IntPtr pointer = default;

            if (!IsClipboardFormatAvailable((int)ClipboardFormats.CF_UNICODETEXT))
            {
                return null;
            }

            TryOpenClipboard();

            try
            {
                handle = GetClipboardData((int)ClipboardFormats.CF_UNICODETEXT);
                if (handle == default)
                {
                    return null;
                }

                pointer = GlobalLock(handle);
                if (pointer == default)
                {
                    return null;
                }

                var size = GlobalSize(handle);
                var buff = new byte[size];

                Marshal.Copy(pointer, buff, 0, size);

                return Encoding.Unicode.GetString(buff).TrimEnd('\0');
            }
            finally
            {
                if (pointer != default)
                {
                    GlobalUnlock(handle);
                }

                CloseClipboard();
            }
        }

        /// <summary>
        /// Set text in clipboard.
        /// </summary>
        /// <param name="text"></param>
        /// <exception cref="Win32Exception"></exception>
        public static void SetText(string text)
        {
            TryOpenClipboard();

            EmptyClipboard();
            IntPtr hGlobal = default;

            try
            {
                var bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == default)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var target = GlobalLock(hGlobal);

                if (target == default)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    GlobalUnlock(target);
                }

                if (SetClipboardData((int)ClipboardFormats.CF_UNICODETEXT, hGlobal) == default)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                hGlobal = default;
            }
            finally
            {
                if (hGlobal != default)
                {
                    Marshal.FreeHGlobal(hGlobal);
                }

                CloseClipboard();
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="Win32Exception"></exception>
        static void TryOpenClipboard()
        {
            var num = 10;
            while (true)
            {
                if (OpenClipboard(default))
                {
                    break;
                }

                if (--num == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                Thread.Sleep(100);
            }
        }
        #endregion

        #region Native methods - private
        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("User32.dll", SetLastError = true)]
        static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern int GlobalSize(IntPtr hMem);
        #endregion
    }
}