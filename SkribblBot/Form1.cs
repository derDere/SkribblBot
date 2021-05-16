using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IronOcr;

namespace SkribblBot {

    public partial class Form1 : Form {

        public static Random RND = new Random();

        public Dictionary<string, WordInfo> Words = new Dictionary<string, WordInfo>();
        public string LastOCR = "";
        public List<string> guesses = new List<string>();
        public List<string> blacklist = new List<string>();
        public List<string> closeCalls = new List<string>();

        public void BlackListAdd(string word) {
            if (!blacklist.Contains(word)) {
                blacklist.Add(word);
                loadWin.BlistLenLab.Text = "BL: " + blacklist.Count.ToString();
                GuessWords();
            }
        }

        public const int XOFF = 8;
        public const int YOFF = 31;
        public const int TEXT_LINES_OFF = 40;
        public static readonly Point TextBoxPos = new Point(870, 760);

        private int StartCount = 0;
        private LoadingBar loadWin = new LoadingBar();
        private ScreenShot screenWin = new ScreenShot();
        private Rectangle LastWinPos;

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
            LastWinPos = new Rectangle(this.Location, this.Size);

            comboBox1.Items.Add(new Language("German", "https://skribbliohints.github.io/German.json"));
            comboBox1.Items.Add(new Language("English", "https://skribbliohints.github.io/words.json"));
            comboBox1.Items.Add(new Language("Spanish", "https://skribbliohints.github.io/Spanish.json"));
            comboBox1.SelectedIndex = 1;

            LoadWords();
        }

        private void DrawGen1(int width, int height, Bitmap image) {
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
        }

        private void button1_Click(object sender, EventArgs e) {
            // Reating Image and Colors
            int step = trackBar1.Value;
            int width = previewPic.Width / step;
            int height = previewPic.Height / step;
            Bitmap originalImg = new Bitmap(previewPic.Width, previewPic.Height);
            previewPic.DrawToBitmap(originalImg, new Rectangle(0, 0, previewPic.Width, previewPic.Height));
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
            previewPic.Image = image;

            DrawGen1(width, height, image);

            // Start Drawing
            StartDrawingActions();
            this.WindowState = FormWindowState.Minimized;
        }

        private void StartDrawingActions() {
            StartCount = cmds.Count;
            loadWin.progressBar1.Maximum = StartCount;
            loadWin.pictureBox1.Image = previewPic.Image;
            drawTicker.Start();
        }

        private Point PicPos(int x, int y, int? step = null) {
            if (step == null) {
                step = trackBar1.Value;
            }
            return new Point(this.Location.X + previewPic.Location.X + XOFF + (x * step.Value), this.Location.Y + previewPic.Location.Y + YOFF + (y * step.Value));
        }

        private Point ToolPos(Panel pan) {
            return new Point(this.Location.X + pan.Location.X + XOFF, this.Location.Y + pan.Location.Y + YOFF);
        }

        private void drawTicker_Tick(object sender, EventArgs e) {
            if (cmds.Count > 0) {
                Cmd cmd = cmds[0];
                cmds.RemoveAt(0);
                if (cmd.down) {
                    CursorControl.MouseDown();
                    drawTicker.Interval = 1;
                } else if (cmd.up) {
                    CursorControl.MouseUp();
                    drawTicker.Interval = 1;
                } else if (cmd.target.HasValue) {
                    CursorControl.MoveTo(cmd.target.Value);
                    drawTicker.Interval = 20;
                }
                loadWin.progressBar1.Value = StartCount - cmds.Count;
                loadWin.statusLab.Text = $"{cmds.Count}/{StartCount} {Math.Round((double)cmds.Count / StartCount * 100)}% Cmds left";
            } else {
                drawTicker.Stop();
            }
            if (ModifierKeys.HasFlag(Keys.Control)) {
                drawTicker.Stop();
                GuessTicker.Stop();
                TextTicker.Stop();
                cmds.Clear();
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e) {
            label1.Text = trackBar1.Value.ToString() + "px";
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            previewPic.ImageLocation = textBox1.Text;
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

            drawTicker.Binding = loadWin.DrawTickerCB;
            chatReadTicker.Binding = loadWin.ChatTickerCB;
            TextTicker.Binding = loadWin.TextTickerCB;
            GuessTicker.Binding = loadWin.GuessTickerCB;
            ChooseTicker.Binding = loadWin.ChooseTickerCB;

            loadWin.startBtn.Click += (s, e2) => {
                chatReadTicker.Enabled = true;
                TextTicker.Enabled = true;
                GuessTicker.Enabled = true;
            };
        }

        private void textBox1_Click(object sender, EventArgs e) {
            textBox1.SelectAll();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            previewPic.Image = screenWin.GetShot();
        }

        private void Form1_Move(object sender, EventArgs e) {
            this.Text = $"Skribbl.io Bot - {this.Left},{this.Top} H{this.Height}";
            if (this.WindowState != FormWindowState.Minimized) {
                LastWinPos = new Rectangle(this.Location, this.Size);
            }
        }

        private void ReloadWordsLab_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Language lang = comboBox1.SelectedItem as Language;

            string jj = "";

            try {
                System.Net.ServicePointManager.Expect100Continue = true;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                System.Net.ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
                var client = new System.Net.WebClient();
                client.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.15) Gecko/20110303 Firefox/3.6.15";
                jj = client.DownloadString(lang.Url);
                ReloadWordsLab.LinkVisited = false;
            } catch (Exception ex) {
                ReloadWordsLab.LinkVisited = true;
            }

            if (!string.IsNullOrEmpty(jj)) {
                System.IO.File.WriteAllText(lang.GetPath(), jj);
                LoadWords();
            }
        }

        private void LoadWords() {
            Language lang = comboBox1.SelectedItem as Language;
            string path = lang.GetPath();
            if (System.IO.File.Exists(path)) {
                string jj = System.IO.File.ReadAllText(path);
                Dictionary<string, WordInfo> data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, WordInfo>>(jj);
                foreach (string key in data.Keys) {
                    data[key].Word = key;
                }
                Words = data;
                WordStatusLab.Text = $"Loaded {Words.Count} words";
            } else {
                WordStatusLab.Text = "No words loaded! Try reloading words";
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            LoadWords();
        }

        private static SolidBrush ReadBrush = new SolidBrush(Color.FromArgb(255, 51, 51, 51));
        public static SolidBrush ReadEraser = new SolidBrush(Color.FromArgb(255, 238, 238, 238));

        private static OCRArgsResult ReadWord(OCRArgsResult args) {
            Bitmap b = new Bitmap(args.PanelBounds.Width, args.PanelBounds.Height);
            using (Graphics g = Graphics.FromImage(b)) {
                g.CopyFromScreen(args.LastWinPos.Location.X + Form1.XOFF + args.PanelBounds.Location.X, args.LastWinPos.Location.Y + Form1.YOFF + args.PanelBounds.Location.Y, 0, 0, args.PanelBounds.Size);
                int lastVal = 255;
                List<int> xVals = new List<int>();
                bool deleted = false;
                for (int x = 0; x < b.Width; x++) {
                    Color c = b.GetPixel(x, TEXT_LINES_OFF);
                    if ((c.R < 128) && (lastVal > 128)) {
                        deleted = false;
                        xVals.Add(x);
                    }
                    Color c2 = b.GetPixel(x, TEXT_LINES_OFF - 5);
                    if (c.R < 128 && c2.R < 128 && !deleted) {
                        deleted = true;
                        xVals.RemoveAt(xVals.Count - 1);
                    }
                    lastVal = c.R;
                }
                //g.FillRectangle(Brushes.Red, 0, TEXT_LINES_OFF, b.Width, 1);
                //g.FillRectangle(new SolidBrush(Color.FromArgb(255, 238, 238, 238)), 0, TEXT_LINES_OFF - 1, b.Width, 3);
                int i = 0;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                foreach (int x in xVals) {
                    //g.FillRectangle(Brushes.Red, x, TEXT_LINES_OFF, 5, 5);
                    g.FillRectangle(ReadEraser, x - 2, TEXT_LINES_OFF - 2, 19, 4);
                    g.DrawString("2", args.ReadFont, ReadBrush, x - 4, TEXT_LINES_OFF - 29);
                }
            }
            args.image = b;

            IronTesseract Ocr = new IronTesseract();
            using (var Input = new OcrInput(b)) {
                // Input.Deskew();  // use if image not straight
                // Input.DeNoise(); // use if image contains digital noise
                OcrResult Result = Ocr.Read(Input);

                args.Result = Result.Text.Replace("2", "_");
            }

            return args;
        }

        private class OCRArgsResult {
            //Args
            public Rectangle LastWinPos;
            public Rectangle PanelBounds;
            public Font ReadFont;
            //Result
            public string Result;
            public Bitmap image;
        }

        private void TextTicker_Tick(object sender, EventArgs e) {
            if (!wordOcrWorker.IsBusy) {
                wordOcrWorker.RunWorkerAsync(new OCRArgsResult() {
                    LastWinPos = LastWinPos,
                    PanelBounds = TextPanel.Bounds,
                    ReadFont = ReadFontLab.Font
                });
            }
        }

        private void Form1_Resize(object sender, EventArgs e) {
            if (this.WindowState != FormWindowState.Minimized) {
                LastWinPos = new Rectangle(this.Location, this.Size);
            }
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e) {
            e.Result = ReadWord(e.Argument as OCRArgsResult);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) {
            OCRArgsResult result = e.Result as OCRArgsResult;
            loadWin.OcrImg.Image = result.image;
            OcrLab.Text = result.Result;
            loadWin.OcrWordLab.Text = result.Result;
            LastOCR = result.Result;
            if (LastOCR.Contains("_")) {
                GuessWords();
            } else {
                TextTicker.Stop();
                StartToDrawWord(LastOCR);
            }
        }

        private static readonly Dictionary<string, string> ORCHelper = new Dictionary<string, string>() {
            {"o", "0"},
            {"i", "1"},
            {"l", "1"},
            {"t", "1"},
            {"g", "9"},
            {"$", "s"},
            {"f", "1"},
        };

        private bool Filter(WordInfo wordinfo) {
            if (wordinfo.Word.Length != LastOCR.Length)
                return false;

            if (blacklist.Contains(wordinfo.Word))
                return false;

            string word = wordinfo.Word.ToLower();
            string match = LastOCR.ToLower();

            if (match.Contains(" ")) {
                if (!word.Contains(" ")) {
                    return false;
                }
            }

            foreach (string key in ORCHelper.Keys) {
                word = word.Replace(key, ORCHelper[key]);
                match = match.Replace(key, ORCHelper[key]);
            }

            for (int i = 0; i < match.Length; i++) {
                if (match[i] != '_') {
                    if (match[i] != word[i]) {
                        return false;
                    }
                }
            }

            return true;
        }

        private int Order(WordInfo wordinfo) {
            int sum = 0;
            foreach (string t in closeCalls) {
                string s = wordinfo.Word;

                if (string.IsNullOrEmpty(s)) {
                    if (string.IsNullOrEmpty(t))
                        return 0;
                    return t.Length;
                }

                if (string.IsNullOrEmpty(t)) {
                    return s.Length;
                }

                int n = s.Length;
                int m = t.Length;
                int[,] d = new int[n + 1, m + 1];

                // initialize the top and right of the table to 0, 1, 2, ...
                for (int i = 0; i <= n; d[i, 0] = i++)
                    ;
                for (int j = 1; j <= m; d[0, j] = j++)
                    ;

                for (int i = 1; i <= n; i++) {
                    for (int j = 1; j <= m; j++) {
                        int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                        int min1 = d[i - 1, j] + 1;
                        int min2 = d[i, j - 1] + 1;
                        int min3 = d[i - 1, j - 1] + cost;
                        d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                    }
                }
                sum += d[n, m];
            }
            return sum;
        }

        private void GuessWords() {
            guesses.Clear();
            guesses.AddRange((from wordinfo in Words.Values where Filter(wordinfo) orderby Order(wordinfo) select wordinfo.Word).ToArray());
            loadWin.ListLenLab.Text = "List: " + guesses.Count.ToString();
            loadWin.listBox1.Items.Clear();
            loadWin.listBox1.Items.AddRange(guesses.ToArray());
        }

        private void QuessTicker_Tick(object sender, EventArgs e) {
            if (guesses.Count > 0) {
                CursorControl.ClickOnPoint(this.Left + TextBoxPos.X, this.Top + TextBoxPos.Y);
                string word;
                if (closeCalls.Count > 0) {
                    word = guesses[0];
                } else {
                    word = guesses[RND.Next(1000000, 9999999) % guesses.Count];
                }
                BlackListAdd(word);
                guesses.Remove(word);
                SendKeys.Send(word + "{ENTER}");
            }
        }

        private void chatOcrWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e) {
            Rectangle bounds = (Rectangle)e.Argument;
            Bitmap b = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(b)) {
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
            }
            OCRArgsResult r = new OCRArgsResult();
            r.image = b;

            IronTesseract Ocr = new IronTesseract();
            using (var Input = new OcrInput(b)) {
                OcrResult Result = Ocr.Read(Input);
                r.Result = Result.Text;
            }

            e.Result = r;
        }

        public static readonly System.Text.RegularExpressions.Regex NormalMessagePattern = new System.Text.RegularExpressions.Regex(@"(.+?:\s)(.+?)$", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        public static readonly System.Text.RegularExpressions.Regex CloseWordPattern = new System.Text.RegularExpressions.Regex(@"['‘´`]?(.+?)'\sis close!", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private void chatOcrWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e) {
            OCRArgsResult r = e.Result as OCRArgsResult;
            loadWin.chatImg.Image = r.image;
            loadWin.chatOcrResult.Text = r.Result;
            if (r.Result == $"{nickTxb.Text} guessed the word!") {
                GuessTicker.Stop();
                blacklist.Clear();
                TextTicker.Stop();
                ChooseTicker.Stop();
            } else if (r.Result.StartsWith("The word was")) {
                GuessTicker.Stop();
                blacklist.Clear();
                TextTicker.Start();
                drawTicker.Stop();
                ChooseTicker.Start();
                isDrawing = false;
            } else if (r.Result == $"{nickTxb.Text} is drawing now!") {
                blacklist.Clear();
                GuessTicker.Stop();
                TextTicker.Start();
                ChooseTicker.Stop();
            } else if (r.Result.EndsWith("is drawing now!")) {
                blacklist.Clear();
                GuessTicker.Start();
                TextTicker.Start();
                drawTicker.Stop();
                ChooseTicker.Stop();
                isDrawing = false;
            } else if (r.Result.EndsWith(" is close!")) {
                var m = CloseWordPattern.Match(r.Result);
                string word = m.Groups[1].Value;
                closeCalls.Add(word);
                loadWin.callsLab.Text = "Close: " + closeCalls.Count.ToString();
                GuessWords();
            } else {
                if (NormalMessagePattern.IsMatch(r.Result)) {
                    if (!r.Result.StartsWith(nickTxb.Text)) {
                        var m = NormalMessagePattern.Match(r.Result);
                        string word = m.Groups[2].Value;
                        loadWin.chatOcrResult.Text = word;
                        BlackListAdd(word);
                        GuessWords();
                    }
                }
                loadWin.chatOcrResult.ForeColor = Color.Black;
            }
        }

        private Rectangle GetChatBounds() {
            return new Rectangle(this.Left + TextBoxPos.X - 15, this.Top + TextBoxPos.Y - 42, 315, 26);
        }

        private void chatReadTicker_Tick(object sender, EventArgs e) {
            if (!chatOcrWorker.IsBusy) {
                chatOcrWorker.RunWorkerAsync(GetChatBounds());
            }
            if (ModifierKeys.HasFlag(Keys.Control)) {
                GuessTicker.Stop();
                TextTicker.Stop();
                drawTicker.Stop();
                chatReadTicker.Stop();
                ChooseTicker.Stop();
            }
        }

        private void button23_Click(object sender, EventArgs e) {
            loadWin.Hide();
            screenWin.Hide();
            this.Hide();
            CreateDrawingForm df = new CreateDrawingForm();
            df.form1 = this;
            df.ShowDialog();
            loadWin.Show();
            screenWin.Show();
            this.Show();
        }

        public void DrawGen2(string word) {
            string path = CreateDrawingForm.ImagesFolder() + "\\" + word + ".json";

            cmds.Clear();

            if (System.IO.File.Exists(path)) {
                string jj = System.IO.File.ReadAllText(path);
                CreateDrawingForm.DrawAction[] actions = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateDrawingForm.DrawAction[]>(jj);

                cmds.AddRange(NewColor(button22.BackColor));
                cmds.AddRange(MouseGoToCmd(ToolPos(thinPenPan)));
                cmds.AddRange(MouseDownCmd());
                cmds.AddRange(MouseUpCmd());
                cmds.AddRange(MouseGoToCmd(ToolPos(PenPanel)));
                cmds.AddRange(MouseDownCmd());
                cmds.AddRange(MouseUpCmd());

                foreach (CreateDrawingForm.DrawAction act in actions) {
                    if (act.pen) {
                        cmds.AddRange(MouseGoToCmd(ToolPos(PenPanel)));
                        cmds.AddRange(MouseDownCmd());
                        cmds.AddRange(MouseUpCmd());
                    }
                    if (act.bukkit) {
                        cmds.AddRange(MouseGoToCmd(ToolPos(BukitPannel)));
                        cmds.AddRange(MouseDownCmd());
                        cmds.AddRange(MouseUpCmd());
                    }
                    if (act.mouseDown) {
                        cmds.AddRange(MouseDownCmd());
                    }
                    if (act.mouseUp) {
                        cmds.AddRange(MouseUpCmd());
                    }
                    if (act.move && act.target.HasValue) {
                        cmds.AddRange(MouseGoToCmd(PicPos(act.target.Value.X, act.target.Value.Y, 1)));
                    }
                    if (act.color.HasValue) {
                        cmds.AddRange(NewColor(act.color.Value));
                    }
                    /*if (act.letter.HasValue) { }*/
                }

            } else {
                cmds.AddRange(NewColor(button1.BackColor));
                cmds.AddRange(MouseGoToCmd(ToolPos(BukitPannel)));
                cmds.AddRange(MouseDownCmd());
                cmds.AddRange(MouseUpCmd());
                cmds.AddRange(MouseGoToCmd(PicPos(0, 0, 1)));
                cmds.AddRange(MouseDownCmd());
                cmds.AddRange(MouseUpCmd());
                cmds.AddRange(MouseGoToCmd(ToolPos(PenPanel)));
                cmds.AddRange(MouseDownCmd());
                cmds.AddRange(MouseUpCmd());
            }
        }

        private bool isDrawing = false;
        public void StartToDrawWord(string word) {
            if (isDrawing)
                return;
            isDrawing = true;
            DrawGen2(word);
            StartDrawingActions();
            loadWin.startBtn.Text = word;
        }

        private void ChooseTicker_Tick(object sender, EventArgs e) {
            CursorControl.ClickOnPoint(
                this.Location.X + XOFF + previewPic.Location.X + (previewPic.Width / 2), 
                this.Location.Y + YOFF + previewPic.Location.Y + (previewPic.Height / 2)
            );
        }
    }

}
