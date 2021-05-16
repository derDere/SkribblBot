using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkribblBot {
    class Language {

        public string Name { get; set; }
        public string Url { get; set; }

        public Language(string n, string u) {
            Name = n;
            Url = u;
        }

        public string GetPath() {
            string path = new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Directory.FullName;
            path += "\\" + this.Name + ".json";
            return path;
        }

        public override string ToString() {
            return Name;
        }
    }

    public class WordInfo {

        public string Word { get; set; }
        public int count { get; set; }

        [Newtonsoft.Json.JsonProperty("lastSeenTime")]
        private long lastSeenTimeTicks;
        [Newtonsoft.Json.JsonIgnore]
        public DateTime lastSeenTime {
            get {
                return new DateTime(lastSeenTimeTicks);
            }
            set {
                lastSeenTimeTicks = value.Ticks;
            }
        }

        public override string ToString() {
            return Word;
        }
    }
}
