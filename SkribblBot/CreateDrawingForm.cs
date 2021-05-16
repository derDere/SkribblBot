using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkribblBot {
    public partial class CreateDrawingForm : Form {

        public static string hex(Color color) {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        public static double deltaBetween(Point A, Point B) {
            int Lx = A.X;
            int Ly = A.Y;
            int Sx = B.X;
            int Sy = B.Y;

            if (Lx < Sx) {
                int tmp = Lx;
                Lx = Sx;
                Sx = tmp;
            }

            if (Ly < Sy) {
                int tmp = Ly;
                Ly = Sy;
                Sy = tmp;
            }

            int Dx = Lx - Sx;
            int Dy = Ly - Sy;

            if (Dx == 0)
                return Dy;

            if (Dy == 0)
                return Dx;

            return Math.Floor(Math.Sqrt(Math.Pow(Dx, 2) + Math.Pow(Dy, 2)));
        }

        private bool DrawMode = false;

        public class DrawAction {
            public bool pen { get; set; }
            public bool bukkit { get; set; }
            public bool mouseDown { get; set; }
            public bool mouseUp { get; set; }
            public bool move { get; set; }
            public Color? color { get; set; }
            public Point? target { get; set; }
            public char? letter { get; set; }

            public override string ToString() {
                if (pen)
                    return "Use Pen";
                if (bukkit)
                    return "Use FillTool";
                if (mouseDown)
                    return "Mouse Down";
                if (mouseUp)
                    return "Mouse Up";
                if (color.HasValue)
                    return $"Color {hex(color.Value)}";
                if (move && target.HasValue)
                    return $"Move to {target.Value.X}, {target.Value.Y}";
                if (letter.HasValue && target.HasValue)
                    return $"Letter '{letter.Value}' at {target.Value.X}, {target.Value.Y}";
                return "Nothing";
            }
        }

        public Form1 form1 { get; set; }

        public Button[] ColorButtons;

        public CreateDrawingForm() {
            InitializeComponent();

            ColorButtons = new Button[] {
                ColorBtn1,
                ColorBtn2,
                ColorBtn3,
                ColorBtn4,
                ColorBtn5,
                ColorBtn6,
                ColorBtn7,
                ColorBtn8,
                ColorBtn9,
                ColorBtn10,
                ColorBtn11,
                ColorBtn12,
                ColorBtn13,
                ColorBtn14,
                ColorBtn15,
                ColorBtn16,
                ColorBtn17,
                ColorBtn18,
                ColorBtn19,
                ColorBtn20,
                ColorBtn21,
                ColorBtn22
            };

            foreach (Button colorBtn in ColorButtons) {
                colorBtn.Click += new EventHandler(ColorBtn_Click);
            }
        }

        private void ColorBtn_Click(object sender, EventArgs e) {
            Button btn = sender as Button;
            StepsLb.Items.Add(new DrawAction() { color = btn.BackColor });
        }

        internal static string ImagesFolder() {
            string path = new System.IO.FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName;
            path += @"\images\";
            if (!System.IO.Directory.Exists(path)) {
                System.IO.Directory.CreateDirectory(path);
            }
            return path;
        }

        private void CreateDrawingForm_Load(object sender, EventArgs e) {
            OrderWordLists();
        }

        private void OrderWordLists() {
            WordsExitsLb.Items.Clear();
            WordsMissingLb.Items.Clear();

            foreach (WordInfo wordInf in form1.Words.Values) {
                string path = ImagesFolder() + "\\" + wordInf.Word + ".json";
                if (System.IO.File.Exists(path)) {
                    WordsExitsLb.Items.Add(wordInf);
                } else {
                    WordsMissingLb.Items.Add(wordInf);
                }
            }
        }

        private void PenBtn_Click(object sender, EventArgs e) {
            StepsLb.Items.Add(new DrawAction() { pen = true });
            Draw();
        }

        private void BukitBtn_Click(object sender, EventArgs e) {
            StepsLb.Items.Add(new DrawAction() { bukkit = true });
            Draw();
        }

        private void DownBtn_Click(object sender, EventArgs e) {
            StepsLb.Items.Add(new DrawAction() { mouseDown = true });
            Draw();
        }

        private void UpBtn_Click(object sender, EventArgs e) {
            StepsLb.Items.Add(new DrawAction() { mouseUp = true });
            Draw();
        }

        private void drawPicture_MouseClick(object sender, MouseEventArgs e) {
            if (DrawMode)
                return;
            StepsLb.Items.Add(new DrawAction() { move = true, target = new Point(e.X, e.Y) });
            Draw();
        }

        private bool MouseDown = false;
        private Point LastMovePoint;
        private void drawPicture_MouseDown(object sender, MouseEventArgs e) {
            if (DrawMode) {
                StepsLb.Items.Add(new DrawAction() { move = true, target = new Point(e.X, e.Y) });
                StepsLb.Items.Add(new DrawAction() { mouseDown = true });
                Draw();
                MouseDown = true;
                LastMovePoint = new Point(e.X, e.Y);
            }
        }

        private void drawPicture_MouseMove(object sender, MouseEventArgs e) {
            if (DrawMode && MouseDown) {
                double delta = deltaBetween(LastMovePoint, new Point(e.X, e.Y));
                if (delta > 10) {
                    LastMovePoint = new Point(e.X, e.Y);
                    StepsLb.Items.Add(new DrawAction() { move = true, target = new Point(e.X, e.Y) });
                    Draw();
                }
            }
        }

        private void drawPicture_MouseUp(object sender, MouseEventArgs e) {
            if (DrawMode) {
                StepsLb.Items.Add(new DrawAction() { move = true, target = new Point(e.X, e.Y) });
                StepsLb.Items.Add(new DrawAction() { mouseUp = true });
                Draw();
                MouseDown = false;
            }
        }

        private void UndoBtn_Click(object sender, EventArgs e) {
            if (StepsLb.Items.Count > 0) {
                StepsLb.Items.RemoveAt(StepsLb.Items.Count - 1);
                Draw();
            }
        }

        public void Draw() {
            Bitmap b = new Bitmap(drawPicture.Width, drawPicture.Height);
            using (Graphics g = Graphics.FromImage(b)) {
                g.Clear(Color.White);
                //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Color lastColor = Color.Black;
                Point? lastPoint = null;
                bool mouseIsDown = false;
                bool lastToolIsPen = true;
                foreach (DrawAction action in StepsLb.Items) {
                    if (action.pen)
                        lastToolIsPen = true;
                    if (action.bukkit)
                        lastToolIsPen = false;
                    if (action.mouseDown)
                        mouseIsDown = true;
                    if (action.mouseUp)
                        mouseIsDown = false;
                    if (action.color.HasValue)
                        lastColor = action.color.Value;
                    if (action.mouseDown && !lastToolIsPen && lastPoint.HasValue) {
                        g.FillEllipse(new SolidBrush(Color.FromArgb(128, lastColor)), lastPoint.Value.X - 10, lastPoint.Value.Y - 10, 20, 20);
                        g.DrawEllipse(new Pen(lastColor), lastPoint.Value.X - 10, lastPoint.Value.Y - 10, 20, 20);
                    }
                    if (action.move && lastPoint.HasValue && lastToolIsPen && mouseIsDown) {
                        if (lastColor.Equals(Color.White)) {
                            g.DrawLine(new Pen(Color.Gray, 1), lastPoint.Value, action.target.Value);
                        } else {
                            g.DrawLine(new Pen(lastColor, 4), lastPoint.Value, action.target.Value);
                        }
                    }
                    if (action.move && action.target.HasValue) {
                        lastPoint = action.target;
                    }
                }

                if (lastPoint.HasValue) {
                    Pen markPen;
                    if (mouseIsDown)
                        markPen = new Pen(Color.Red);
                    else
                        markPen = new Pen(Color.Lime);
                    if (lastToolIsPen) {
                        g.DrawLine(markPen, lastPoint.Value.X - 11, lastPoint.Value.Y, lastPoint.Value.X + 10, lastPoint.Value.Y);
                        g.DrawLine(markPen, lastPoint.Value.X, lastPoint.Value.Y - 11, lastPoint.Value.X, lastPoint.Value.Y + 10);
                    } else {
                        g.DrawLine(markPen, lastPoint.Value.X, lastPoint.Value.Y, lastPoint.Value.X + 10, lastPoint.Value.Y);
                        g.DrawLine(markPen, lastPoint.Value.X, lastPoint.Value.Y, lastPoint.Value.X + 10, lastPoint.Value.Y - 11);
                    }
                }

                colorPreviewPic.BackColor = lastColor;
            }
            drawPicture.Image = b;
        }

        private void DrawModeCB_CheckedChanged(object sender, EventArgs e) {
            DrawMode = DrawModeCB.Checked;
        }

        private void WordsMissingLb_SelectedIndexChanged(object sender, EventArgs e) {
            WordInfo wi = WordsMissingLb.SelectedItem as WordInfo;
            CurrentWordLab.Tag = wi;
            CurrentWordLab.Text = wi.Word;
        }

        private void WordsExitsLb_SelectedIndexChanged(object sender, EventArgs e) {
            if (StepsLb.Items.Count <= 0 || MessageBox.Show("Loading a existing word will delete all unsaved current progress!\nDo you wish to continue?", "WARNING", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                WordInfo wi = WordsExitsLb.SelectedItem as WordInfo;
                if (wi == null)
                    return;
                CurrentWordLab.Tag = wi;
                CurrentWordLab.Text = wi.Word;

                string path = ImagesFolder() + "\\" + wi.Word + ".json";
                string jj = System.IO.File.ReadAllText(path);

                DrawAction[] actions = Newtonsoft.Json.JsonConvert.DeserializeObject<DrawAction[]>(jj);

                StepsLb.Items.Clear();
                StepsLb.Items.AddRange(actions);
                Draw();
            }
        }

        private void SaveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            if (CurrentWordLab.Tag == null || typeof(WordInfo) != CurrentWordLab.Tag.GetType()) {
                MessageBox.Show("Please select a word!");
                return;
            }

            List<DrawAction> actions = new List<DrawAction>();
            foreach (DrawAction act in StepsLb.Items) {
                actions.Add(act);
            }

            string jj = Newtonsoft.Json.JsonConvert.SerializeObject(actions);

            WordInfo wi = CurrentWordLab.Tag as WordInfo;

            string path = ImagesFolder() + "\\" + wi.Word + ".json";

            System.IO.File.WriteAllText(path, jj);

            OrderWordLists();
        }

        private void ClearBtn_Click(object sender, EventArgs e) {
            if (MessageBox.Show("Do you really want to clear the whole image?!","WARNING",MessageBoxButtons.YesNo) == DialogResult.Yes) {
                StepsLb.Items.Clear();
                Draw();
            }
        }
    }
}
