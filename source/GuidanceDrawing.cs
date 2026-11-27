using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    public sealed class GuidanceMark
    {
        public Rectangle Box;
        public string Text;
        public Color Colour;
        public bool Corner, Compact, Sliding, Pulse;
        public bool Outline = true;
        public int ArrowX, ArrowY;
        public GuidanceMark Offset(int x, int y)
        {
            Rectangle box = Box; box.Offset(x, y);
            return new GuidanceMark { Box = box, Text = Text, Colour = Colour, Corner = Corner, Compact = Compact, Outline = Outline, Sliding=Sliding, Pulse=Pulse, ArrowX=ArrowX, ArrowY=ArrowY };
        }
    }

    // Layout and painting are independent of any windows, so they can be
    // checked on bitmaps without displaying an overlay or touching the game.
    public static class GuidanceDrawing
    {
        public static Rectangle Cell(DetectedPuzzle puzzle, int index)
        {
            int pitch = puzzle.Kind == PuzzleKind.Sliding ? 56 : puzzle.Kind == PuzzleKind.Towers ? 44 : 38;
            int size = puzzle.Kind == PuzzleKind.Sliding ? 49 : puzzle.Kind == PuzzleKind.Towers ? 42 : 30;
            return new Rectangle(puzzle.Grid.X + (int)Math.Round(index % 5 * pitch * puzzle.Scale), puzzle.Grid.Y + (int)Math.Round(index / 5 * pitch * puzzle.Scale), (int)Math.Round(size * puzzle.Scale), (int)Math.Round(size * puzzle.Scale));
        }
        public static List<GuidanceMark> SliderMarks(DetectedPuzzle puzzle,IList<int> moves,int firstNumber=1,int maximum=4)
        {
            var marks=new List<GuidanceMark>();var used=new HashSet<int>();
            for(int i=0;i<Math.Min(maximum,moves.Count);i++)
            {
                if(!used.Add(moves[i]))break;
                Color[] colours={Color.Lime,Color.Cyan,Color.Orange,Color.MediumPurple};
                marks.Add(new GuidanceMark{Box=Cell(puzzle,moves[i]),Text=(firstNumber+i).ToString(),Colour=colours[i%colours.Length],Sliding=true});
            }
            return marks;
        }
        // Kavros cycles 5, blank, then 1. Blank is always a step.
        public static int TowerClicksRemaining(int current, int target)
        {
            if (current < 0 || current > 5 || target < 1 || target > 5) throw new ArgumentOutOfRangeException("Tower heights must be blank or 1-5.");
            if (current == 0) return target;
            return (target - current + 6) % 6;
        }
        public static List<GuidanceMark> TowerMarks(DetectedPuzzle puzzle, int[] solution, bool countdown = false)
        {
            List<GuidanceMark> result = new List<GuidanceMark>();
            for (int i = 0; i < 25; i++)
            {
                int value = countdown ? TowerClicksRemaining(puzzle.Towers.Cells[i], solution[i]) : solution[i];
                result.Add(new GuidanceMark { Box = Cell(puzzle, i), Text = value.ToString(), Colour = countdown && value == 0 ? Color.Lime : Color.White, Corner = true, Compact = true, Outline = false });
            }
            return result;
        }
        public static float CompactFontSize(Rectangle box) { return Math.Max(8F, Math.Min(13F, box.Height * 0.24F)); }
        public static Rectangle LabelBounds(GuidanceMark mark)
        {
            if (mark.Compact)
            {
                float size = CompactFontSize(mark.Box);
                int inset = (int)Math.Ceiling(Math.Max(2F, mark.Box.Width * 0.055F));
                int width = (int)Math.Ceiling(size * 0.85F), height = (int)Math.Ceiling(size + 3);
                return new Rectangle(mark.Box.Right - inset - width, mark.Box.Top + inset, width, height);
            }
            return mark.Corner ? new Rectangle(mark.Box.X + 1, mark.Box.Y + 1, Math.Max(12, mark.Box.Width / 2), Math.Max(15, mark.Box.Height / 2)) : mark.Box;
        }
        // Decide the window work for one tick. An unchanged puzzle does not show,
        // hide, move, or paint again. A borderless window's reported Bounds are
        // not used here: they can differ from the rectangle that was set, which
        // used to move and clear the overlay on every tick.
        public static void Present(bool visible, bool placedKnown, Rectangle placed, bool allow, Rectangle bounds, bool marksChanged, out bool show, out bool hide, out bool move, out bool paint, out bool nextKnown, out Rectangle nextPlaced)
        {
            show=false; hide=false; move=false; paint=false; nextKnown=placedKnown; nextPlaced=placed;
            if(!allow)
            {
                if(visible)hide=true;
                nextKnown=false;
                return;
            }
            move=!placedKnown||placed!=bounds;
            if(!visible)show=true;
            if(move){nextKnown=true;nextPlaced=bounds;}
            paint=marksChanged||move;
        }
        public static bool SameMarks(IList<GuidanceMark> first, IList<GuidanceMark> second)
        {
            if (first.Count != second.Count) return false;
            for (int i = 0; i < first.Count; i++)
            {
                GuidanceMark a = first[i], b = second[i];
                if (a.Box != b.Box || a.Text != b.Text || a.Colour.ToArgb() != b.Colour.ToArgb() || a.Corner != b.Corner || a.Compact != b.Compact || a.Outline != b.Outline || a.Sliding!=b.Sliding || a.Pulse!=b.Pulse || a.ArrowX!=b.ArrowX || a.ArrowY!=b.ArrowY) return false;
            }
            return true;
        }
        public static bool HideForCapture(bool captureExcluded, PuzzleKind kind)
        {
            // Screenshots and recordings must show the overlay. Capture reads
            // the game window, then paints over these marks, so nothing is hidden.
            return false;
        }
        public static Rectangle OpaquePatch(GuidanceMark mark)
        {
            if (mark.Sliding)
            {
                int size = Math.Max(12, Math.Min(22, (int)Math.Round(mark.Box.Width * 0.40)));
                return new Rectangle(mark.Box.Left + 1, mark.Box.Top + 1, size, size);
            }
            return LabelBounds(mark);
        }
        // Number badges, and the thin box stroke, without covering the tile picture.
        // A pulsed slider step grows 8px and draws a 7px stroke on that edge,
        // so the bright ring reaches 12px outside the cell. The gold arrow is
        // covered as well. Capture exclusion hides the window entirely; this
        // is the fallback that still has to paint over those pixels.
        public static void AddOpaque(GuidanceMark mark, IList<Rectangle> patches)
        {
            patches.Add(OpaquePatch(mark));
            if (mark.Sliding && (mark.Pulse || mark.ArrowX != 0 || mark.ArrowY != 0))
            {
                Rectangle ring = Rectangle.Inflate(mark.Box, 12, 12);
                int band = 12;
                if (ring.Width <= band * 2 || ring.Height <= band * 2) patches.Add(ring);
                else
                {
                    patches.Add(new Rectangle(ring.Left, ring.Top, ring.Width, band));
                    patches.Add(new Rectangle(ring.Left, ring.Bottom - band, ring.Width, band));
                    patches.Add(new Rectangle(ring.Left, ring.Top, band, ring.Height));
                    patches.Add(new Rectangle(ring.Right - band, ring.Top, band, ring.Height));
                }
                if (mark.ArrowX != 0 || mark.ArrowY != 0)
                {
                    int x1 = mark.Box.Left + mark.Box.Width / 2, y1 = mark.Box.Top + mark.Box.Height / 2;
                    int x2 = x1 + mark.ArrowX * mark.Box.Width, y2 = y1 + mark.ArrowY * mark.Box.Height;
                    patches.Add(Rectangle.FromLTRB(Math.Min(x1, x2) - 10, Math.Min(y1, y2) - 10, Math.Max(x1, x2) + 10, Math.Max(y1, y2) + 10));
                }
                return;
            }
            if (!mark.Outline && !mark.Sliding) return;
            Rectangle box = mark.Sliding ? Rectangle.Inflate(mark.Box, 4, 4) : mark.Box;
            int thick = 4;
            if (box.Width <= thick * 2 || box.Height <= thick * 2) { patches.Add(box); return; }
            patches.Add(new Rectangle(box.Left, box.Top, box.Width, thick));
            patches.Add(new Rectangle(box.Left, box.Bottom - thick, box.Width, thick));
            patches.Add(new Rectangle(box.Left, box.Top, thick, box.Height));
            patches.Add(new Rectangle(box.Right - thick, box.Top, thick, box.Height));
        }
        public static void Draw(Graphics graphics, IEnumerable<GuidanceMark> marks)
        {
            foreach (GuidanceMark mark in marks)
            {
                if(mark.Sliding)
                {
                    int grow=mark.Pulse?(((Environment.TickCount/180)%2==0)?4:8):2;
                    Rectangle box=Rectangle.Inflate(mark.Box,grow,grow);
                    using(Pen shadow=new Pen(Color.Black,mark.Pulse?7:5))graphics.DrawRectangle(shadow,box);
                    using(Pen ink=new Pen(mark.Colour,mark.Pulse?4:2))graphics.DrawRectangle(ink,box);
                    if(mark.ArrowX!=0||mark.ArrowY!=0)
                    {
                        int x1=mark.Box.Left+mark.Box.Width/2,y1=mark.Box.Top+mark.Box.Height/2;
                        int x2=x1+mark.ArrowX*mark.Box.Width,y2=y1+mark.ArrowY*mark.Box.Height;
                        using(Pen arrow=new Pen(mark.Colour,3)){arrow.EndCap=System.Drawing.Drawing2D.LineCap.ArrowAnchor;graphics.DrawLine(arrow,x1,y1,x2,y2);}
                    }
                    int size=Math.Max(12,Math.Min(22,(int)Math.Round(mark.Box.Width*0.40)));Rectangle badge=new Rectangle(mark.Box.Left+1,mark.Box.Top+1,size,size);
                    graphics.FillEllipse(Brushes.Black,badge);
                    using(Font digit=new Font("Segoe UI",size*(mark.Text.Length>2?0.43F:0.65F),FontStyle.Bold,GraphicsUnit.Pixel))TextRenderer.DrawText(graphics,mark.Text,digit,badge,mark.Colour,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
                    continue;
                }
                if (mark.Outline) using (Pen pen = new Pen(mark.Colour, 2)) graphics.DrawRectangle(pen, mark.Box);
                Rectangle label = LabelBounds(mark);
                if (mark.Corner || mark.Compact) graphics.FillRectangle(Brushes.Black, label);
                using (Font font = mark.Compact
                    ? new Font("Segoe UI", CompactFontSize(mark.Box), FontStyle.Bold, GraphicsUnit.Pixel)
                    : new Font("Segoe UI", Math.Max(9, Math.Min(17, mark.Box.Height * 0.38F)), FontStyle.Bold))
                {
                    TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
                    if (!mark.Compact)
                    {
                        Rectangle shadow = label; shadow.Offset(1, 1);
                        TextRenderer.DrawText(graphics, mark.Text, font, shadow, Color.Black, flags);
                    }
                    TextRenderer.DrawText(graphics, mark.Text, font, label, mark.Colour, flags);
                }
            }
        }
        // Pixels the slider guide can paint: both pulse rings, the outline,
        // the arrow and the badge. Capture ignores these and keeps the tile.
        public static void PaintSliderMask(Graphics graphics, GuidanceMark mark)
        {
            if(!mark.Sliding)return;
            int outer=mark.Pulse?9:7;
            int[] grows=mark.Pulse?new[]{4,8}:new[]{2};
            using(Pen ink=new Pen(Color.White,outer))
                foreach(int grow in grows)graphics.DrawRectangle(ink,Rectangle.Inflate(mark.Box,grow,grow));
            if(mark.ArrowX!=0||mark.ArrowY!=0)
            {
                int x1=mark.Box.Left+mark.Box.Width/2,y1=mark.Box.Top+mark.Box.Height/2;
                int x2=x1+mark.ArrowX*mark.Box.Width,y2=y1+mark.ArrowY*mark.Box.Height;
                using(Pen arrow=new Pen(Color.White,7)){arrow.EndCap=LineCap.ArrowAnchor;graphics.DrawLine(arrow,x1,y1,x2,y2);}
            }
            int size=Math.Max(12,Math.Min(22,(int)Math.Round(mark.Box.Width*0.40)))+2;
            graphics.FillEllipse(Brushes.White,new Rectangle(mark.Box.Left,mark.Box.Top,size,size));
        }
    }

    public sealed class GuideInk
    {
        readonly int originX,originY,width,height;
        readonly byte[] bits;
        GuideInk(int originX,int originY,int width,int height,byte[] bits)
        {
            this.originX=originX;this.originY=originY;this.width=width;this.height=height;this.bits=bits;
        }
        public bool Ignores(int x,int y)
        {
            int lx=x-originX,ly=y-originY;
            if(bits==null||lx<0||ly<0||lx>=width||ly>=height)return false;
            return bits[ly*width+lx]!=0;
        }
        public static GuideInk ForSlider(int imageWidth,int imageHeight,IList<GuidanceMark> marks,Point origin)
        {
            if(marks==null||imageWidth<1||imageHeight<1)return null;
            Rectangle union=Rectangle.Empty;bool any=false;
            var local=new List<GuidanceMark>();
            foreach(GuidanceMark mark in marks)
            {
                if(mark==null||!mark.Sliding)continue;
                GuidanceMark shifted=mark.Offset(-origin.X,-origin.Y);
                Rectangle area=Rectangle.Inflate(shifted.Box,16,16);
                if(shifted.ArrowX!=0||shifted.ArrowY!=0)
                {
                    int x1=shifted.Box.Left+shifted.Box.Width/2,y1=shifted.Box.Top+shifted.Box.Height/2;
                    int x2=x1+shifted.ArrowX*shifted.Box.Width,y2=y1+shifted.ArrowY*shifted.Box.Height;
                    area=Rectangle.Union(area,Rectangle.FromLTRB(Math.Min(x1,x2)-12,Math.Min(y1,y2)-12,Math.Max(x1,x2)+12,Math.Max(y1,y2)+12));
                }
                area=Rectangle.Intersect(Rectangle.Inflate(area,2,2),new Rectangle(0,0,imageWidth,imageHeight));
                if(area.Width<=0||area.Height<=0)continue;
                union=any?Rectangle.Union(union,area):area;any=true;local.Add(shifted);
            }
            if(!any)return null;
            byte[] bits=new byte[union.Width*union.Height];
            using(Bitmap bmp=new Bitmap(union.Width,union.Height,PixelFormat.Format32bppArgb))
            {
                using(Graphics g=Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode=SmoothingMode.None;
                    g.TranslateTransform(-union.X,-union.Y);
                    foreach(GuidanceMark mark in local)GuidanceDrawing.PaintSliderMask(g,mark);
                }
                BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                try
                {
                    byte[] raw=new byte[Math.Abs(data.Stride)*bmp.Height];
                    Marshal.Copy(data.Scan0,raw,0,raw.Length);
                    for(int y=0;y<bmp.Height;y++)for(int x=0;x<bmp.Width;x++)
                        if(raw[y*data.Stride+x*4+3]>=32)bits[y*bmp.Width+x]=1;
                }
                finally{bmp.UnlockBits(data);}
            }
            return new GuideInk(union.X,union.Y,union.Width,union.Height,bits);
        }
    }
}
