// Tile icons, drawn in code.
using System.Drawing.Drawing2D;

namespace Wplus;

static class Icons
{
    static Pen RoundPen(Color c, float w) => new(c, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

    static GraphicsPath Rr(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static void Device(Graphics g, PointF c, float h, Color col)
    {
        var r = new RectangleF(c.X - h * 0.26f, c.Y - h / 2, h * 0.52f, h);
        using (var p = Rr(r, h * 0.11f))
        using (var pen = RoundPen(col, Math.Max(h * 0.035f, 2f))) g.DrawPath(pen, p);
        using (var pen = RoundPen(col, Math.Max(h * 0.025f, 1.5f)))
            g.DrawLine(pen, c.X - h * 0.07f, r.Y + h * 0.07f, c.X + h * 0.07f, r.Y + h * 0.07f);
        float rr = h * 0.035f;
        using (var pen = new Pen(col, Math.Max(h * 0.02f, 1.2f)))
            g.DrawEllipse(pen, c.X - rr, r.Bottom - h * 0.08f - rr, rr * 2, rr * 2);
    }

    public static void Folder(Graphics g, PointF c, float h, Color col)
    {
        float w = h * 1.15f;
        var body = new RectangleF(c.X - w / 2, c.Y + h * 0.06f - h * 0.36f, w, h * 0.72f);
        var tab = new RectangleF(body.X, body.Y - h * 0.12f, w * 0.42f, h * 0.2f);
        using (var p = Rr(tab, h * 0.06f))
        using (var b = new SolidBrush(Color.FromArgb(col.A, (int)(col.R * 0.75), (int)(col.G * 0.75), (int)(col.B * 0.75)))) g.FillPath(b, p);
        using (var p = Rr(body, h * 0.08f))
        using (var b = new SolidBrush(col)) g.FillPath(b, p);
    }

    public static void Library(Graphics g, PointF c, float h, Color col)
    {
        var l = new PointF(c.X - h * 0.22f, c.Y + h * 0.28f);
        var r = new PointF(c.X + h * 0.28f, c.Y + h * 0.20f);
        float rr = h * 0.14f;
        using var b = new SolidBrush(col);
        g.FillEllipse(b, l.X - rr, l.Y - rr, rr * 2, rr * 2);
        g.FillEllipse(b, r.X - rr, r.Y - rr, rr * 2, rr * 2);
        var lt = new PointF(l.X + h * 0.12f, c.Y - h * 0.36f);
        var rt = new PointF(r.X + h * 0.12f, c.Y - h * 0.44f);
        using (var pen = new Pen(col, Math.Max(h * 0.06f, 2f)))
        {
            g.DrawLine(pen, l.X + h * 0.12f, l.Y, lt.X, lt.Y);
            g.DrawLine(pen, r.X + h * 0.12f, r.Y, rt.X, rt.Y);
        }
        g.FillPolygon(b, new[] { lt, rt, new PointF(rt.X, rt.Y + h * 0.14f), new PointF(lt.X, lt.Y + h * 0.14f) });
    }

    static readonly Color SuccessGreen = Color.FromArgb(0x13, 0xcd, 0x00);

    public static void Check(Graphics g, PointF c, float h, float alpha)
    {
        float rr = h * 0.42f;
        using (var b = new SolidBrush(Color.FromArgb((int)(255 * alpha), SuccessGreen))) g.FillEllipse(b, c.X - rr, c.Y - rr, rr * 2, rr * 2);
        using var pen = RoundPen(Color.FromArgb((int)(255 * alpha), Color.White), Math.Max(h * 0.07f, 2.5f));
        g.DrawLines(pen, new[]
        {
            new PointF(c.X - h * 0.18f, c.Y), new PointF(c.X - h * 0.04f, c.Y + h * 0.14f), new PointF(c.X + h * 0.2f, c.Y - h * 0.13f),
        });
    }

    public static void Warning(Graphics g, PointF c, float h, Color col)
    {
        using (var b = new SolidBrush(col))
            g.FillPolygon(b, new[] { new PointF(c.X, c.Y - h * 0.4f), new PointF(c.X + h * 0.44f, c.Y + h * 0.34f), new PointF(c.X - h * 0.44f, c.Y + h * 0.34f) });
        var white = Color.FromArgb(col.A, Color.White);
        using (var pen = RoundPen(white, Math.Max(h * 0.07f, 2.5f))) g.DrawLine(pen, c.X, c.Y - h * 0.14f, c.X, c.Y + h * 0.1f);
        float rr = h * 0.045f;
        using (var b = new SolidBrush(white)) g.FillEllipse(b, c.X - rr, c.Y + h * 0.22f - rr, rr * 2, rr * 2);
    }

    public static void Lock(Graphics g, PointF c, float h, Color col)
    {
        var body = new RectangleF(c.X - h * 0.3f, c.Y + h * 0.14f - h * 0.23f, h * 0.6f, h * 0.46f);
        using (var p = Rr(body, h * 0.08f))
        using (var b = new SolidBrush(col)) g.FillPath(b, p);
        var shackle = new RectangleF(c.X - h * 0.19f, c.Y - h * 0.16f - h * 0.23f, h * 0.38f, h * 0.46f);
        using (var p = Rr(shackle, h * 0.19f))
        using (var pen = new Pen(col, Math.Max(h * 0.07f, 2.5f))) g.DrawPath(pen, p);
    }

    static readonly Color[] SpinnerColors =
    {
        Color.FromArgb(0x17, 0x71, 0xfc), Color.FromArgb(0x3b, 0x99, 0xfc), Color.FromArgb(0x97, 0x97, 0x97),
    };

    public static void Spinner(Graphics g, PointF c, float h, double t)
    {
        for (int i = 0; i < 3; i++)
        {
            float phase = (float)(t * 2 * Math.PI / 1.2) - i * 0.9f;
            float scale = 0.65f + 0.35f * Math.Abs(MathF.Sin(phase));
            float rr = h * 0.1f * scale;
            float x = c.X + (i - 1) * h * 0.3f;
            using var b = new SolidBrush(SpinnerColors[i]);
            g.FillEllipse(b, x - rr, c.Y - rr, rr * 2, rr * 2);
        }
    }

    public static void Arrow(Graphics g, PointF top, float len, Color col)
    {
        using var pen = RoundPen(col, 3f);
        var bottom = new PointF(top.X, top.Y + len);
        g.DrawLine(pen, top, bottom);
        g.DrawLine(pen, bottom, new PointF(bottom.X - len * 0.3f, bottom.Y - len * 0.3f));
        g.DrawLine(pen, bottom, new PointF(bottom.X + len * 0.3f, bottom.Y - len * 0.3f));
    }
}
