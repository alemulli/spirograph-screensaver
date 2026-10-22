using System;
using System.Drawing;

namespace SpirographScreensaver
{
    sealed class Gradient
    {
        public string Name;
        public Color[] Stops;

        public Gradient(string name, params string[] stops)
        {
            Name = name;
            Stops = Array.ConvertAll(stops, ColorTranslator.FromHtml);
        }

        /// <summary>Colour at position f around the looping gradient.</summary>
        public Color At(double f)
        {
            var pos = (f % 1 + 1) % 1 * Stops.Length;
            var i = (int)pos;
            var a = Stops[i % Stops.Length];
            var b = Stops[(i + 1) % Stops.Length];
            var m = pos - i;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * m),
                (int)(a.G + (b.G - a.G) * m),
                (int)(a.B + (b.B - a.B) * m));
        }
    }

    sealed class Pattern
    {
        public bool Inside;
        public int Ring;
        public int Wheel;
        /// <summary>Pen hole distance from the wheel centre, as a fraction of the wheel radius</summary>
        public double Hole;
        public double Rotation;
        public Gradient Gradient;
        public int ColorRepeats;
        public float Width;
        public float Opacity;

        // Same set as the web app
        public static readonly Gradient[] Gradients =
        {
            new Gradient("Rainbow", "#ff3b3b", "#ff9f1c", "#ffe03b", "#3ddc5f", "#2fb8ff", "#5b5bff", "#c03bff"),
            new Gradient("Galaxy", "#3a0ca3", "#7b2ff7", "#f107a3", "#4361ee", "#4cc9f0", "#c8b6ff"),
            new Gradient("Chameleon", "#1f9d55", "#9be15d", "#f9d423", "#f76b1c", "#14b8a6", "#2563eb"),
            new Gradient("Sunset", "#ff5e62", "#ff9966", "#ffd56b", "#c850c0", "#4158d0"),
            new Gradient("Ocean", "#03256c", "#1768ac", "#06bee1", "#7ee8c7", "#2541b2"),
            new Gradient("Fire", "#7a0000", "#d00000", "#ff6d00", "#ffb700", "#ff6d00", "#d00000"),
        };

        public int Laps => Wheel / Gcd(Ring, Wheel);

        static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

        public static Pattern Random(Random rng)
        {
            Pattern p;
            do
            {
                var inside = rng.NextDouble() < 0.7;
                var ring = rng.Next(40, 151);
                p = new Pattern
                {
                    Inside = inside,
                    Ring = ring,
                    Wheel = rng.Next(8, (inside ? ring - 8 : 90) + 1),
                    Hole = rng.Next(30, 111) / 100.0,
                };
            } while (p.Laps < 3 || p.Laps > 30);

            p.Rotation = rng.NextDouble() * 360;
            p.Gradient = Gradients[rng.Next(Gradients.Length)];
            p.ColorRepeats = rng.Next(1, 7);
            p.Width = rng.Next(2, 6) / 2f;
            p.Opacity = rng.Next(75, 101) / 100f;
            return p;
        }
    }

    /// <summary>A pattern placed on screen: centre and fitted radius in pixels.</summary>
    sealed class Geometry
    {
        const int MaxSteps = 240_000;
        const double StepPx = 1.5;

        public readonly int Steps;
        /// <summary>On-screen distance the pen moves per step, in px</summary>
        public readonly double StepLength;

        readonly double c, d, k, scale, cos, sin, tMax;
        readonly bool inside;
        readonly PointF centre;

        public Geometry(Pattern p, PointF centre, float radius)
        {
            this.centre = centre;
            inside = p.Inside;
            double R = p.Ring, r = p.Wheel;
            d = p.Hole * r;
            c = inside ? R - r : R + r;
            k = c / r;
            var fit = inside ? Math.Max(R, Math.Abs(c) + d) : Math.Max(R + 2 * r, c + d);
            scale = radius / fit;
            tMax = 2 * Math.PI * p.Laps;
            var pathPx = tMax * scale * (Math.Abs(c) + d * Math.Abs(k));
            Steps = (int)Math.Min(MaxSteps, Math.Max(400, Math.Ceiling(pathPx / StepPx)));
            StepLength = pathPx / Steps;
            var rot = p.Rotation * Math.PI / 180;
            cos = Math.Cos(rot);
            sin = Math.Sin(rot);
        }

        public PointF Point(int i)
        {
            var t = (double)i / Steps * tMax;
            var x = c * Math.Cos(t) + (inside ? d : -d) * Math.Cos(k * t);
            var y = c * Math.Sin(t) - d * Math.Sin(k * t);
            return new PointF(
                (float)(centre.X + (x * cos - y * sin) * scale),
                (float)(centre.Y + (x * sin + y * cos) * scale));
        }
    }
}
