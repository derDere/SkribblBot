﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkribblBot {
    public partial class LoadingBar : Form {
        public LoadingBar() {
            InitializeComponent();
        }

        private void LoadingBar_Move(object sender, EventArgs e) {
            this.Text = $"Progress - {this.Left},{this.Top}";
        }
    }
}
