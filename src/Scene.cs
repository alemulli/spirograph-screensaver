using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SpirographScreensaver
{
    /// <summary>
    /// Draws 3–5 spirographs at random places, holds, fades everything out, then starts again.
    /// </summary>
    sealed class Scene : IDisposable
    {
        public static readonly Color Background = ColorTranslator.FromHtml("#0e0e10");

        const double PenSpeed = 700;      // px per second at 1080p; scaled to the screen height
        const double PauseBetween = 0.8;  // seconds after each pattern
        const double Hold = 5;            // seconds with all patterns shown
        const double FadeOut = 2.5;

        enum Phase { Drawing, Pausing, Holding, Fading }

        readonly Random rng = new Random(Guid.NewGuid().GetHashCode());
        readonly Size size;
        readonly Bitmap canvas;
        readonly Graphics g;
        readonly double speed;
        readonly List<(PointF centre, float radius)> placed = new List<(PointF, float)>();

        Phase phase;
        double phaseTime;
        int remaining;
        Pattern pattern;
        Geometry geometry;
        int progress;

        public Scene(Size size)
        {
            this.size = size;
            canvas = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height), PixelFormat.Format32bppPArgb);
            g = Graphics.FromImage(canvas);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            speed = PenSpeed * size.Height / 1080.0;
            Restart();
        }

        /// <summary>0 while patterns are visible, rising to 1 as the screen fades to the background.</summary>
        public float FadeAmount => phase == Phase.Fading ? (float)Math.Min(1, phaseTime / FadeOut) : 0;

        public Bitmap Canvas => canvas;

        void Restart()
        {
            g.Clear(Background);
            placed.Clear();
            remaining = rng.Next(3, 6);
            StartPattern();
        }

        void StartPattern()
        {
            remaining--;
            pattern = Pattern.Random(rng);
            var (centre, radius) = PickSpot();
            placed.Add((centre, radius));
            geometry = new Geometry(pattern, centre, radius);
            progress = 0;
            SetPhase(Phase.Drawing);
        }

        /// <summary>Tries several random spots and keeps the one that overlaps earlier patterns least.</summary>
        (PointF, float) PickSpot()
        {
            var shortSide = Math.Min(size.Width, size.Height);
            var radius = (float)(shortSide * (0.16 + rng.NextDouble() * 0.16));
            var margin = shortSide * 0.03f;
            var best = PointF.Empty;
            var bestScore = double.NegativeInfinity;

            for (var attempt = 0; attempt < 16; attempt++)
            {
                var spot = new PointF(
                    RandomBetween(radius + margin, size.Width - radius - margin),
                    RandomBetween(radius + margin, size.Height - radius - margin));
                var score = double.PositiveInfinity;
                foreach (var (centre, r) in placed)
                {
                    var gap = Math.Sqrt(Math.Pow(spot.X - centre.X, 2) + Math.Pow(spot.Y - centre.Y, 2)) - r - radius;
                    score = Math.Min(score, gap);
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = spot;
                }
            }
            return (best, radius);
        }

        float RandomBetween(float min, float max) =>
            max <= min ? (min + max) / 2 : (float)(min + rng.NextDouble() * (max - min));

        void SetPhase(Phase next)
        {
            phase = next;
            phaseTime = 0;
        }

        /// <summary>Advances by dt seconds. Returns true when the whole screen needs repainting.</summary>
        public bool Update(double dt)
        {
            phaseTime += dt;
            Dirty = RectangleF.Empty;
            switch (phase)
            {
                case Phase.Drawing:
                    var next = Math.Min(geometry.Steps, progress + Math.Max(1, (int)(speed * dt / geometry.StepLength)));
                    Stroke(progress, next);
                    progress = next;
                    if (progress >= geometry.Steps) SetPhase(remaining > 0 ? Phase.Pausing : Phase.Holding);
                    return false;
                case Phase.Pausing:
                    if (phaseTime >= PauseBetween) StartPattern();
                    return false;
                case Phase.Holding:
                    if (phaseTime >= Hold) SetPhase(Phase.Fading);
                    return false;
                default:
                    if (phaseTime >= FadeOut) Restart();
                    return true;
            }
        }

        /// <summary>Area touched by the latest stroke, so only that needs repainting.</summary>
        public RectangleF Dirty { get; private set; }

        void Stroke(int from, int to)
        {
            if (to <= from) return;
            // Smaller chunks give smoother colour transitions; the pen changes colour per chunk
            var chunk = Math.Max(2, geometry.Steps / (pattern.ColorRepeats * 400));
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

            for (var a = from; a < to; a += chunk)
            {
                var b = Math.Min(a + chunk, to);
                var points = new PointF[b - a + 1];
                for (var i = a; i <= b; i++)
                {
                    var p = geometry.Point(i);
                    points[i - a] = p;
                    minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y);
                    maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y);
                }
                var color = pattern.Gradient.At((a + b) / 2.0 / geometry.Steps * pattern.ColorRepeats);
                using (var pen = new Pen(Color.FromArgb((int)(pattern.Opacity * 255), color), pattern.Width))
                {
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, points);
                }
            }

            var pad = pattern.Width + 2;
            Dirty = RectangleF.FromLTRB(minX - pad, minY - pad, maxX + pad, maxY + pad);
        }

        public void Dispose()
        {
            g.Dispose();
            canvas.Dispose();
        }
    }
}
