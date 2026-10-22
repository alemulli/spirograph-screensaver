using System;
using System.Windows.Forms;

namespace SpirographScreensaver
{
    static class Program
    {
        /// <summary>
        /// Windows starts a screensaver with /s (run), /p &lt;hwnd&gt; (preview) or /c (settings).
        /// The flag may arrive as "/p 1234" or "/p:1234", in either case.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            UseRealPixels();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var flag = args.Length > 0 ? args[0].ToLowerInvariant().Trim() : "/c";
            var value = flag.Length > 3 && flag[2] == ':' ? flag.Substring(3) : args.Length > 1 ? args[1] : null;
            flag = flag.Length >= 2 ? flag.Substring(0, 2) : flag;

            switch (flag)
            {
                case "/s":
                    foreach (var screen in Screen.AllScreens)
                        new ScreensaverForm(screen.Bounds).Show();
                    Application.Run();
                    break;
                case "/p":
                    if (long.TryParse(value, out var hwnd))
                        Application.Run(new ScreensaverForm(new IntPtr(hwnd)));
                    break;
                default:
                    MessageBox.Show(
                        "Spirograph draws random patterns in random places, then fades them out and starts again. There are no settings to change.",
                        "Spirograph Screensaver",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;
            }
        }

        /// <summary>Without this, Windows scales the window on high-DPI screens and the lines come out blurry.</summary>
        static void UseRealPixels()
        {
            try
            {
                Native.SetProcessDpiAwarenessContext(new IntPtr(-4)); // Per-monitor v2, Windows 10 1703+
            }
            catch (EntryPointNotFoundException)
            {
                Native.SetProcessDPIAware();
            }
        }
    }
}
