using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    // A small notice on the game window. The text lets clicks through. Only the
    // action button takes a click, and the notice is moved off the card, the
    // puzzle, and the area being read.
    internal sealed class NoticeToast : Form
    {
        private readonly Label text = new Label();
        private readonly Button action = new Button();
        private readonly Timer timer = new Timer();
        private Action click;
        private Size noticeSize = new Size(180, 34);
        internal static bool TrayHidden;

        internal NoticeToast()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = KavrosTheme.Panel;
            ForeColor = KavrosTheme.Text;
            Font = NoticeFont();
            text.Dock = DockStyle.Fill;
            text.TextAlign = ContentAlignment.MiddleLeft;
            text.Padding = new Padding(8, 0, 4, 0);
            action.Dock = DockStyle.Right;
            action.Width = 84;
            action.FlatStyle = FlatStyle.Flat;
            action.Visible = false;
            action.BackColor = KavrosTheme.Gold;
            action.ForeColor = KavrosTheme.GoldInk;
            action.Click += delegate { if (click != null) click(); Hide(); };
            Controls.Add(text);
            Controls.Add(action);
            timer.Tick += delegate { timer.Stop(); Hide(); };
            FormClosed += delegate { timer.Dispose(); };
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= 0x08000000 | 0x80;
                return parameters;
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x21) { message.Result = new IntPtr(3); return; }
            if (message.Msg == 0x84)
            {
                int packed = message.LParam.ToInt32();
                Point screen = new Point((short)(packed & 0xFFFF), (short)((packed >> 16) & 0xFFFF));
                if (action.Visible && action.Bounds.Contains(PointToClient(screen))) { base.WndProc(ref message); return; }
                message.Result = new IntPtr(-1);
                return;
            }
            base.WndProc(ref message);
        }

        internal void Present(string message, bool error, string actionText, Action onClick, Rectangle game, IList<Rectangle> blocked, bool sticky)
        {
            click = onClick;
            text.Text = (TrayHidden && message != null ? message.Replace(" Check the tray.", "") : message) ?? "";
            action.Text = actionText ?? "";
            action.Visible = !string.IsNullOrEmpty(actionText);
            BackColor = error ? KavrosTheme.Danger : KavrosTheme.Panel;
            text.ForeColor = error ? KavrosTheme.GoldInk : KavrosTheme.Text;
            text.BackColor = BackColor;
            int measured = TextRenderer.MeasureText(text.Text, Font).Width + 20;
            int width = Math.Max(140, Math.Min(300, measured + (action.Visible ? action.Width : 0)));
            noticeSize = new Size(width, 34);
            ClientSize = noticeSize;
            MoveClear(game, blocked);
            if (!Visible) Show();
            timer.Stop();
            if (!sticky) { timer.Interval = 4000; timer.Start(); }
        }

        internal void MoveClear(Rectangle game, IList<Rectangle> blocked)
        {
            Rectangle place = NoticePlacement.Place(game, noticeSize, blocked);
            if (!IsHandleCreated) CreateControl();
            Native.PlaceNoZOrder(Handle, place);
        }

        internal static void ShowAndWait(string message, Rectangle area)
        {
            using (NoticeToast toast = new NoticeToast())
            {
                toast.Present(message, false, null, null, area, null, false);
                Application.Run(toast);
            }
        }

        private static Font NoticeFont()
        {
            try { return KavrosTheme.Ui(8.5F, false); }
            catch (Exception) { return new Font(FontFamily.GenericSansSerif, 8.5F); }
        }
    }

    internal static class NoticePlacement
    {
        internal static Rectangle Place(Rectangle game, Size size, IList<Rectangle> blocked)
        {
            if (size.Width < 1) size.Width = 1;
            if (size.Height < 1) size.Height = 1;
            if (game.Width < size.Width + 16 || game.Height < size.Height + 16)
            {
                Rectangle screen = game.Width > 0 ? game : Native.VirtualScreen();
                return new Rectangle(screen.Right - size.Width - 8, screen.Top + 8, size.Width, size.Height);
            }
            var spots = new List<Rectangle>();
            spots.Add(Corner(game, size, true, true));
            spots.Add(Corner(game, size, false, true));
            spots.Add(Corner(game, size, true, false));
            spots.Add(Corner(game, size, false, false));
            for (int x = game.Right - size.Width - 8; x >= game.Left + 8; x -= 24)
            {
                spots.Add(new Rectangle(x, game.Top + 8, size.Width, size.Height));
                spots.Add(new Rectangle(x, game.Bottom - size.Height - 8, size.Width, size.Height));
            }
            for (int y = game.Top + 8; y <= game.Bottom - size.Height - 8; y += 24)
            {
                spots.Add(new Rectangle(game.Left + 8, y, size.Width, size.Height));
                spots.Add(new Rectangle(game.Right - size.Width - 8, y, size.Width, size.Height));
            }
            foreach (Rectangle spot in spots)
                if (game.Contains(spot) && !Hits(spot, blocked)) return spot;
            Rectangle outside = new Rectangle(game.Right + 8, game.Top + 8, size.Width, size.Height);
            if (!Hits(outside, blocked)) return outside;
            return new Rectangle(game.Left + 8, Math.Max(0, game.Top - size.Height - 8), size.Width, size.Height);
        }

        private static Rectangle Corner(Rectangle game, Size size, bool right, bool top)
        {
            int x = right ? game.Right - size.Width - 8 : game.Left + 8;
            int y = top ? game.Top + 8 : game.Bottom - size.Height - 8;
            return new Rectangle(x, y, size.Width, size.Height);
        }

        private static bool Hits(Rectangle spot, IList<Rectangle> blocked)
        {
            if (blocked == null) return false;
            Rectangle pad = Rectangle.Inflate(spot, 4, 4);
            foreach (Rectangle block in blocked)
                if (block.Width > 0 && block.Height > 0 && pad.IntersectsWith(block)) return true;
            return false;
        }
    }
}
