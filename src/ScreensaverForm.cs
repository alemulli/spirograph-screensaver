using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpirographScreensaver
{
    sealed class ScreensaverForm : Form
    {
        const int MouseTolerance = 10;

        readonly bool preview;
        readonly IntPtr previewParent;
        readonly Timer timer = new Timer { Interval = 15 };
        readonly Stopwatch clock = new Stopwatch();
        Scene scene;
        Point? mouseStart;

        /// <summary>Full-screen saver on one monitor.</summary>
        public ScreensaverForm(Rectangle bounds) : this()
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            TopMost = true;
        }

        /// <summary>Small preview inside the Screen Saver Settings dialog.</summary>
        public ScreensaverForm(IntPtr parent) : this()
        {
            preview = true;
            previewParent = parent;
        }

        ScreensaverForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Scene.Background;
            DoubleBuffered = true;
            KeyPreview = true;
            timer.Tick += (s, e) => Tick();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (preview)
            {
                Native.SetParent(Handle, previewParent);
                Native.SetWindowLong(Handle, Native.GWL_STYLE, Native.GetWindowLong(Handle, Native.GWL_STYLE) | Native.WS_CHILD);
                Native.GetClientRect(previewParent, out var rect);
                Location = Point.Empty;
                Size = new Size(rect.Right, rect.Bottom);
            }
            else
            {
                Cursor.Hide();
            }
            scene = new Scene(ClientSize);
            clock.Start();
            timer.Start();
        }

        void Tick()
        {
            // The settings dialog closes the preview by destroying its parent window
            if (preview && !Native.IsWindow(previewParent))
            {
                Close();
                return;
            }
            var dt = Math.Min(0.1, clock.Elapsed.TotalSeconds);
            clock.Restart();
            if (scene.Update(dt)) Invalidate();
            else Invalidate(Rectangle.Ceiling(scene.Dirty));
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Skipped: OnPaint covers every pixel, and clearing first causes flicker
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (scene == null) return;
            e.Graphics.CompositingMode = CompositingMode.SourceCopy;
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(scene.Canvas, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
            var fade = scene.FadeAmount;
            if (fade > 0)
            {
                e.Graphics.CompositingMode = CompositingMode.SourceOver;
                using (var brush = new SolidBrush(Color.FromArgb((int)(fade * 255), Scene.Background)))
                    e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (preview) return;
            if (mouseStart == null) mouseStart = e.Location;
            else if (Math.Abs(e.X - mouseStart.Value.X) > MouseTolerance || Math.Abs(e.Y - mouseStart.Value.Y) > MouseTolerance)
                Application.Exit();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!preview) Application.Exit();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!preview) Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                scene?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    static class Native
    {
        public const int GWL_STYLE = -16;
        public const int WS_CHILD = 0x40000000;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hWnd, int index, int value);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    }
}
