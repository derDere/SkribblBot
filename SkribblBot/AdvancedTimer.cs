using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkribblBot {
    public class AdvancedTimer : Timer {

        public override bool Enabled {
            get {
                return base.Enabled;
            }
            set {
                base.Enabled = value;
                Ichanged = true;
                if (Binding != null) {
                    Binding.Checked = base.Enabled;
                }
                Ichanged = false;
            }
        }

        private bool Ichanged = false;
        private CheckBox _Binding;
        public CheckBox Binding {
            get {
                return _Binding;
            }
            set {
                bool originalValue = base.Enabled;
                if (value != _Binding) {
                    RemoveBindingEvents();
                }
                _Binding = value;
                if (_Binding != null) {
                    _Binding.Checked = originalValue;
                    AddBindingEvents();
                }
            }
        }

        private void AddBindingEvents() {
            if (_Binding == null)
                return;
            _Binding.CheckedChanged += Binding_CheckedChanged_Handler;
        }

        private void RemoveBindingEvents() {
            if (_Binding == null)
                return;
            _Binding.CheckedChanged -= Binding_CheckedChanged_Handler;
        }

        private EventHandler Binding_CheckedChanged_Handler;
        private void Binding_CheckedChanged(object sender, EventArgs e) {
            if (!Ichanged) {
                base.Enabled = Binding.Checked;
            }
        }

        public AdvancedTimer() : base() {
            Init();
        }

        public AdvancedTimer(System.ComponentModel.IContainer container) : base (container) {
            Init();
        }

        private void Init() {
            Binding_CheckedChanged_Handler = new EventHandler(Binding_CheckedChanged);
        }
    }
}
