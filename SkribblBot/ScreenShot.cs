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
    public partial class ScreenShot : Form {
        public ScreenShot() {
            InitializeComponent();
        }

        public Image GetShot() {
            Bitmap b = new Bitmap(panel1.Width, panel1.Height);
            using (Graphics g = Graphics.FromImage(b)) {
                g.CopyFromScreen(this.Location.X + Form1.XOFF + panel1.Location.X, this.Location.Y + Form1.YOFF + panel1.Location.Y, 0, 0, panel1.Size);
            }
            return b;
        }
    }
}
