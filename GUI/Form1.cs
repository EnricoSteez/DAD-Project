using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace GUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Add_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {

                string Cmnd = textBox1.Text + "\r\n";
                richTextBox1.AppendText(Cmnd);

                textBox1.Text = String.Empty;
            }
        }


       
        private async void Openfile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Text File|*.txt", Multiselect = false })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    using (StreamReader rd = new StreamReader(ofd.FileName))
                    {
                        richTextBox1.AppendText(await rd.ReadToEndAsync());
                    }
                }


            }
        }
        int numlines = 0;
        private void Rtb_TextChanged(object sender, EventArgs e)
        {
            if (newline_index == numlines && newline_index != 0)
            {
                var start1 = richTextBox1.GetFirstCharIndexFromLine(newline_index);  // Get the 1st char index of the appended text
                var length1 = richTextBox1.Lines[newline_index].Length;
                richTextBox1.Select(start1, length1);
                richTextBox1.SelectionBackColor = Color.FromArgb(94, 148, 255);
            }
            
            numlines = richTextBox1.Lines.Count()-1;
            //System.Diagnostics.Debug.WriteLine("nmblines =" + numlines);

            if (numlines == 1)
            {
                var start1 = richTextBox1.GetFirstCharIndexFromLine(0);  // Get the 1st char index of the appended text
                var length1 = richTextBox1.Lines[0].Length;
                richTextBox1.Select(start1, length1);
                richTextBox1.SelectionBackColor = Color.FromArgb(94, 148, 255);
            }

        }

        int line_index = 0;
        int newline_index = 0;
        private void Runline_Click(object sender, EventArgs e)
        {
            line_index = newline_index;
            if (line_index + 1 <= numlines)
            {
                System.Diagnostics.Debug.WriteLine("nmblines =" + numlines + "  " + "line =" + line_index);
                //System.Diagnostics.Debug.WriteLine(line);
                string command = richTextBox1.Lines[line_index];
                //System.Diagnostics.Debug.WriteLine(command);

                // Unhighlight the executed command and make bold 
                var start1 = richTextBox1.GetFirstCharIndexFromLine(line_index);  // Get the 1st char index of the appended text
                var length1 = command.Length;
                richTextBox1.Select(start1, length1);

                Font font = new Font(richTextBox1.Font.FontFamily, richTextBox1.Font.Size, FontStyle.Bold);
                richTextBox1.SelectionFont = font; // Bold
                richTextBox1.SelectionBackColor = Color.Transparent; // Unhighlight


                newline_index = line_index + 1;
                // SEND COMMAND TO PUPPETMASTER


                // Highlight the next command
                if (newline_index < numlines) // so if we not finished all the lines
                {
                    var start = richTextBox1.GetFirstCharIndexFromLine(newline_index);  // Get the 1st char index of the appended text
                    var length = richTextBox1.Lines[newline_index].Length;
                    richTextBox1.Select(start, length);
                    richTextBox1.SelectionBackColor = Color.FromArgb(94, 148, 255);
                }
            }
        }


        private void none(object sender, EventArgs e)
        {

        }
    }
}
