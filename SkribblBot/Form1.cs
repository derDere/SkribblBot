using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SkribblBot {

    public partial class Form1 : Form {

        public const int XOFF = 2;
        public const int YOFF = 31;

        private int StartCount = 0;
        private LoadingBar loadWin = new LoadingBar();
        private ScreenShot screenWin = new ScreenShot();

        private List<Cmd> cmds = new List<Cmd>();
        private Dictionary<uint, Button> ColorButtons = new Dictionary<uint, Button>();

        private static uint ColorVal(Color c) {
            uint r = 0;
            r |= ((uint)c.A << (8 * 3));
            r |= ((uint)c.R << (8 * 2));
            r |= ((uint)c.G << (8 * 1));
            r |= ((uint)c.B << (8 * 0));
            return r;
        }

        private static Color IntValue(uint i) {
            uint a = (i >> (8 * 3)) & 255;
            uint r = (i >> (8 * 2)) & 255;
            uint g = (i >> (8 * 1)) & 255;
            uint b = (i >> (8 * 0)) & 255;
            return Color.FromArgb((int)a, (int)r, (int)g, (int)b);
        }

        public struct Cmd {
            public Point? target;
            public bool down;
            public bool up;
        }

        public static Cmd[] MouseGoToCmd(Point target) {
            return new Cmd[] { new Cmd() {
                target = new Point(target.X, target.Y),
                down = false,
                up = false
            } };
        }

        public static Cmd[] MouseDownCmd() {
            return new Cmd[] { new Cmd() {
                target = null,
                down = true,
                up = false
            } };
        }

        public static Cmd[] MouseUpCmd() {
            return new Cmd[] { new Cmd() {
                target = null,
                down = false,
                up = true
            } };
        }

        public Cmd[] NewColor(Color c) {
            Button b = ColorButtons[ColorVal(c)];
            return new Cmd[] {
                MouseGoToCmd(new Point(this.Location.X + b.Location.X + 12 + XOFF, this.Location.Y + b.Location.Y + 12 + YOFF))[0],
                MouseDownCmd()[0],
                MouseUpCmd()[0]
            };
        }

        public Form1() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            // Reating Image and Colors
            int step = trackBar1.Value;
            int width = pictureBox1.Width / step;
            int height = pictureBox1.Height / step;
            Bitmap originalImg = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.DrawToBitmap(originalImg, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
            Bitmap image = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(image)) {
                g.DrawImage(originalImg, 0, 0, width, height);
            }
            for (int x = 0; x < image.Width; x++) {
                for (int y = 0; y < image.Height; y++) {
                    Color pixel = image.GetPixel(x, y);
                    Color newPix = GetDrawableColor(pixel);
                    image.SetPixel(x, y, newPix);
                }
            }
            pictureBox1.Image = image;

            // Start Creating Print
            cmds.AddRange(NewColor(button21.BackColor));
            cmds.AddRange(MouseGoToCmd(ToolPos(BukitPannel)));
            cmds.AddRange(MouseDownCmd());
            cmds.AddRange(MouseUpCmd());
            cmds.AddRange(MouseGoToCmd(PicPos(0, 0)));
            cmds.AddRange(MouseDownCmd());
            cmds.AddRange(MouseUpCmd());
            cmds.AddRange(MouseGoToCmd(ToolPos(PenPanel)));
            cmds.AddRange(MouseDownCmd());
            cmds.AddRange(MouseUpCmd());
            Color lastC = Color.Magenta;
            for (int y = 0; y < image.Height; y++) {
                cmds.AddRange(MouseGoToCmd(PicPos(-1, y)));
                cmds.AddRange(MouseDownCmd());
                for (int x = 0; x < image.Width; x++) {
                    Color c = image.GetPixel(x, y);
                    if (lastC != c) {
                        cmds.AddRange(MouseGoToCmd(PicPos(x - 1, y)));
                        cmds.AddRange(MouseUpCmd());
                        cmds.AddRange(NewColor(c));
                        cmds.AddRange(MouseGoToCmd(PicPos(x, y)));
                        cmds.AddRange(MouseDownCmd());
                    }
                    lastC = c;
                }
                cmds.AddRange(MouseGoToCmd(PicPos(width, y)));
                cmds.AddRange(MouseUpCmd());
                lastC = Color.Magenta;
            }
            cmds.AddRange(NewColor(Color.Black));
            cmds.AddRange(MouseGoToCmd(PicPos(-1, -1)));
            cmds.AddRange(MouseDownCmd());
            cmds.AddRange(MouseGoToCmd(PicPos(width, -1)));
            cmds.AddRange(MouseGoToCmd(PicPos(width, height)));
            cmds.AddRange(MouseGoToCmd(PicPos(-1, height)));
            cmds.AddRange(MouseGoToCmd(PicPos(-1, -1)));
            cmds.AddRange(MouseUpCmd());
            cmds.AddRange(NewColor(Color.White));
            cmds.AddRange(MouseGoToCmd(ToolPos(BukitPannel)));
            cmds.AddRange(MouseDownCmd());
            cmds.AddRange(MouseUpCmd());
            cmds.AddRange(MouseGoToCmd(PicPos(-2, -2)));
            cmds.AddRange(MouseDownCmd());
            cmds.AddRange(MouseUpCmd());

            // Start Drawing
            StartCount = cmds.Count;
            loadWin.progressBar1.Maximum = StartCount;
            loadWin.pictureBox1.Image = pictureBox1.Image;
            this.WindowState = FormWindowState.Minimized;
            ticker.Start();
        }

        private Point PicPos(int x, int y) {
            int step = trackBar1.Value;
            return new Point(this.Location.X + pictureBox1.Location.X + XOFF + (x * step), this.Location.Y + pictureBox1.Location.Y + YOFF + (y * step));
        }

        private Point ToolPos(Panel pan) {
            return new Point(this.Location.X + pan.Location.X + XOFF, this.Location.Y + pan.Location.Y + YOFF);
        }

        private void ticker_Tick(object sender, EventArgs e) {
            if (cmds.Count > 0) {
                Cmd cmd = cmds[0];
                cmds.RemoveAt(0);
                if (cmd.down) {
                    CursorControl.MouseDown();
                    ticker.Interval = 1;
                } else if (cmd.up) {
                    CursorControl.MouseUp();
                    ticker.Interval = 1;
                } else if (cmd.target.HasValue) {
                    CursorControl.MoveTo(cmd.target.Value);
                    ticker.Interval = 20;
                }
                loadWin.progressBar1.Value = StartCount - cmds.Count;
                loadWin.label1.Text = $"{cmds.Count}/{StartCount} {Math.Round((double)cmds.Count / StartCount * 100)}% Cmds left";
            } else {
                ticker.Stop();
            }
            if (ModifierKeys.HasFlag(Keys.Control)) {
                ticker.Stop();
                cmds.Clear();
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e) {
            label1.Text = trackBar1.Value.ToString() + "px";
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            pictureBox1.ImageLocation = textBox1.Text;
        }

        private int ColorDelta(Color A, Color B) {
            int rD = Math.Abs(A.R - B.R);
            int gD = Math.Abs(A.G - B.G);
            int bD = Math.Abs(A.B - B.B);
            return rD + gD + bD;
        }

        private Color GetDrawableColor(Color original) {
            int nearest = int.MaxValue;
            Color c = Color.Black;
            foreach (uint i in ColorButtons.Keys) {
                Color col = IntValue(i);
                int d = ColorDelta(original, col);
                if (d < nearest) {
                    nearest = d;
                    c = col;
                }
            }
            return c;
        }

        private void Form1_Load(object sender, EventArgs e) {
            textBox1_TextChanged(null, null);

            foreach (Control con in this.Controls) {
                if (con.GetType() == typeof(Button)) {
                    if (con != startBtn) {
                        Button btn = (Button)con;
                        ColorButtons.Add(ColorVal(btn.BackColor), btn);
                    }
                }
            }

            loadWin.Show();
            screenWin.Show();
        }

        private void textBox1_Click(object sender, EventArgs e) {
            textBox1.SelectAll();
        }

        private Point? isPanMoving = null;
        private int startMoveX = 0;
        private void MovePan_MouseDown(object sender, MouseEventArgs e) {
            isPanMoving = MousePosition;
            startMoveX = MovePan.Location.X;
        }
        private void MovePan_MouseMove(object sender, MouseEventArgs e) {
            if (isPanMoving.HasValue) {
                int xD = isPanMoving.Value.X - MousePosition.X;
                int newX = startMoveX - xD;
                MovePan.Left = newX;
                int newW = MovePan.Location.X - pictureBox1.Location.X;
                pictureBox1.Width = newW;
            }
        }
        private void MovePan_MouseUp(object sender, MouseEventArgs e) {
            isPanMoving = null;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            pictureBox1.Image = screenWin.GetShot();
        }
    }

}
