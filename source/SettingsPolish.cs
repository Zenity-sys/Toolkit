using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    internal static partial class KavrosTheme
    {
        public static void StyleCombo(ComboBox combo)
        {
            combo.BackColor = Input; combo.ForeColor = Text; combo.FlatStyle = FlatStyle.Flat;
        }

        public static void AddMainCards(PuzzleSuiteForm form) { form.ArrangePolish(); }
    }

    internal sealed class InterfaceToggle : CheckBox
    {
        public InterfaceToggle()
        {
            AutoSize = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            Appearance = Appearance.Button;
            ForeColor = KavrosTheme.Text;
            BackColor = KavrosTheme.Panel;
            Cursor = Cursors.Hand;
        }
        protected override void OnClick(EventArgs e)
        {
            if (Checked) return;
            base.OnClick(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Color fill = Checked ? KavrosTheme.Gold : KavrosTheme.Input;
            Color edge = Checked ? KavrosTheme.GoldEdge : KavrosTheme.ButtonLine;
            Color ink = Checked ? KavrosTheme.GoldInk : KavrosTheme.Text;
            Color surface = Parent == null ? BackColor : Parent.BackColor;
            e.Graphics.Clear(surface);
            KavrosTheme.FillRounded(e.Graphics, new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3)), 3, fill, edge);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ink, fill, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    public sealed partial class PuzzleSuiteForm
    {
        // Cards drawn behind the existing controls. Coordinates match the
        // control bounds set in the constructor; nothing is re-parented.
        internal void ArrangePolish()
        {
            MainSurface body = Controls.OfType<MainSurface>().Single();
            ((PreviewBox)preview).IsOffline = delegate { return offline; };
            body.Cards.Add(new MainSurface.Card { Label = "CAPTURE", Bounds = new Rectangle(16, 112, 484, 96) });
            body.Cards.Add(new MainSurface.Card { Label = "DETECTION OPTIONS", Bounds = new Rectangle(16, 216, 484, 80) });
            body.Cards.Add(new MainSurface.Card { Label = "SOLUTION", Bounds = new Rectangle(16, 346, 484, 503) });
            Control interfaceLabel = body.Controls.Find("InterfaceLabel", true)[0];
            Control legacyToggle = body.Controls.Find("LegacyInterface", true)[0];
            // Heading band is the label offset. The card stops at the toggles,
            // so Resolution and Scaling stay on the page background.
            int cardTop = interfaceLabel.Top - 32;
            int cardBottom = legacyToggle.Bottom + 16;
            body.Cards.Add(new MainSurface.Card { Label = "CLIENT", Bounds = new Rectangle(16, cardTop, 484, cardBottom - cardTop) });
            foreach (CheckBox c in body.Controls.OfType<CheckBox>())
                if (c != floatingEnabled && c != checkUpdates && c != blurPreview) c.BackColor = KavrosTheme.Panel;
            instructions.BackColor = KavrosTheme.Panel;
        }
    }
}
