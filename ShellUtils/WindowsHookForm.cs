using System;
using System.Windows.Forms;


namespace ShellUtils
{
    public class WindowsHookForm : Form
    {
        // Hotkey fields:
        private const int WM_HOTKEY_MESSAGESG_ID = 0x0312;
        private const int ALT = 0x0001;
        private const int CTRL = 0x0002;
        private const int SHIFT = 0x0004;
        private const int WIN = 0x0008;
        private int keyVisibleWindows = (int)Keys.W;
        private int keyAllWindows = (int)Keys.A;

        // Windows fields:
        private readonly int windowMessage_ShellHook;

        // Events
        public event Action<IntPtr> WindowCreatedEvent;
        public event Action<IntPtr> WindowActivatedEvent;
        public event Action<IntPtr> WindowDestroyedEvent;
        public event Action KeypressArrangeVisibleEvent;
        public event Action KeypressArrangeAllEvent;

        public WindowsHookForm()
        {
            // windows handler
            windowMessage_ShellHook = WindowsApi.RegisterWindowMessage("SHELLHOOK");
            WindowsApi.RegisterShellHookWindow(this.Handle);

            // keys handler
            RegisterKeyHandler(this, keyVisibleWindows, ALT + CTRL + SHIFT);
            RegisterKeyHandler(this, keyAllWindows, ALT + CTRL + SHIFT);
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == windowMessage_ShellHook)
            {
                // handle windows
                WindowsApi.ShellEvents shellEvent = (WindowsApi.ShellEvents)message.WParam.ToInt32();
                IntPtr windowHandle = message.LParam;

                switch (shellEvent)
                {
                    case WindowsApi.ShellEvents.HSHELL_WINDOWCREATED:
                        WindowCreatedEvent?.Invoke(windowHandle);
                        break;
                    case WindowsApi.ShellEvents.HSHELL_WINDOWACTIVATED:
                        WindowActivatedEvent?.Invoke(windowHandle);
                        break;
                    case WindowsApi.ShellEvents.HSHELL_WINDOWDESTROYED:
                        WindowDestroyedEvent?.Invoke(windowHandle);
                        break;
                }
            }
            else if (message.Msg == WM_HOTKEY_MESSAGESG_ID)
            {
                // handle keys
                int key = (int)((long)message.LParam >> 16);
                int mod = (int)((long)message.LParam & 0xFFFF);
                if (mod == ALT + CTRL + SHIFT)
                {
                    if (key == keyVisibleWindows)
                        KeypressArrangeVisibleEvent?.Invoke();
                    else if (key == keyAllWindows)
                        KeypressArrangeAllEvent?.Invoke();
                }
            }
            base.WndProc(ref message);
        }

        protected override void Dispose(bool disposing)
        {
            // windows
            try
            {
                WindowsApi.DeregisterShellHookWindow(this.Handle);
            }
            catch { }

            // keys
            try
            {
                UnregisterKeyHandler(this, keyVisibleWindows, ALT + CTRL + SHIFT);
                UnregisterKeyHandler(this, keyAllWindows, ALT + CTRL + SHIFT);
            }
            catch { }
            base.Dispose(disposing);
        }

        public static bool RegisterKeyHandler(Form form, int key, int mod = 0) => WindowsApi.RegisterHotKey(form.Handle, mod ^ key ^ form.Handle.ToInt32(), mod, key);
        public static bool UnregisterKeyHandler(Form form, int key, int mod = 0) => WindowsApi.UnregisterHotKey(form.Handle, mod ^ key ^ form.Handle.ToInt32());

    }
}
