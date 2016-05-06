using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;

namespace ATPP08 {
    public partial class Form1 : Form {
        string[] s= null;
        Regex reg = new Regex(@"[^ ]+", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
        string tofind = null;

        public Form1() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                s = File.ReadAllLines(openFileDialog1.FileName);
                long sum = 0;
                foreach (string i in s) {
                    sum += i.Length;
                }
                label3.Text = sum.ToString();
            }
        }

        List<FindResult> all_word_entries(string target, Action<FindResult> new_found_word_callback, CancellationToken ct) {
            List<FindResult> res = new List<FindResult>();
            for(int i = 0; i < s.Length; i++) {
                string ssss = (string)s[i].Clone();
                if (backgroundWorker1.CancellationPending || ct.IsCancellationRequested)
                    return null;
                while (true) {
                    if (ssss.Contains(target)) {
                        int position = ssss.IndexOf(target);
                        FindResult newfound = new FindResult() {
                            original_str = s[i],
                            pos = position,
                            paragraph_num = i
                        };
                        res.Add(newfound);
                        new_found_word_callback(newfound);
                        ssss = ssss.Substring(position + target.Length);
                        position += target.Length;
                    }
                    else {
                        break;
                    }
                }
                Thread.Sleep(2);
            }
            return res;
        }

        string find_longest_word(Action<string> change_callback, CancellationToken ct) {
            string max = "";
            foreach(string item in s) {
                if (backgroundWorker1.CancellationPending || ct.IsCancellationRequested)
                    return null;
                int pos = 0;
                while(true) {
                    var tail = item.Substring(pos);
                    Match m = reg.Match(tail);
                    if (!m.Success)
                        break;
                    var found = m.Value;
                    pos += found.Length;
                    if (found.Length > max.Length) {
                        max = found;
                        change_callback(max);
                    }
                }
                Thread.Sleep(2);
            }
            return max;
        }

        delegate void find_result_callback(FindResult fr);
        delegate void find_longest_callback(string s);

        void worker_starter(object sender, DoWorkEventArgs e) {
            CancellationTokenSource cts = new CancellationTokenSource();
            int seconds = int.Parse(textBox3.Text);
            cts.CancelAfter(1000 * seconds);
            find_result_callback frc = add_new_match;
            find_longest_callback flc = show_new_longest;
            tofind = textBox1.Text;
            Task t1 = new Task(() =>
                all_word_entries(tofind, (FindResult fr) => {
                    listBox1.Invoke(frc, fr);
                }, cts.Token
            ), cts.Token);
            Task t2 = new Task(() => {
                find_longest_word((string s) => {
                    textBox2.Invoke(flc, s);
                }, cts.Token);
            }, cts.Token);
            t1.Start();
            t2.Start();
        }

        void add_new_match(FindResult fr) {
            int start_match = fr.pos - 30;
            int end_match = fr.pos + 40;
            if (start_match < 0)
                start_match = 0;
            if (end_match > fr.original_str.Length) {
                end_match = fr.original_str.Length;
            }
            listBox1.Items.Add(String.Format(">[{0}line {1} pos]...{2}...", fr.paragraph_num, fr.pos,
                fr.original_str.Substring(start_match, end_match - start_match)));
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        void show_new_longest(string s) {
            textBox2.Text = s;
        }

        class FindResult {
            public string original_str;
            public int pos;
            public int paragraph_num;
        }

        private void button3_Click(object sender, EventArgs e) {
            backgroundWorker1.RunWorkerAsync();
        }

        private void Form1_Load(object sender, EventArgs e) {
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += worker_starter;
        }

        private void button2_Click(object sender, EventArgs e) {
            backgroundWorker1.CancelAsync();
        }
    }
}
