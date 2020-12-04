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
using Grpc.Core;
using Server.protos;
using System.Collections;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Grpc.Net.Client;

namespace GUI
{
    public partial class Form1 : Form
    {

        List<Server.ServerIdentification> servers = new List<Server.ServerIdentification>();
        Dictionary<string, List<string>> partitions = new Dictionary<String, List<string>>();

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
                System.Diagnostics.Debug.WriteLine("\"" + textBox1.Text + "\" added to waiting list");
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
                        System.Diagnostics.Debug.WriteLine("Script file added to command waiting list");

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
            
            if (numlines != richTextBox1.Lines.Count() - 1)
            {
                System.Diagnostics.Debug.WriteLine("Number of lines = " + (richTextBox1.Lines.Count() - 1));
            }

            numlines = richTextBox1.Lines.Count()-1;
            

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
                executeline(command);

                System.Diagnostics.Debug.WriteLine("Executed: \"" + command + "\". " + "Line " + (line_index + 1) + " of " + numlines);

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

        private void Runall_Click(object sender, EventArgs e)
        {
            while (line_index + 1 <= numlines)
            {
                string command = richTextBox1.Lines[line_index];

                // Unhighlight the executed command and make bold 
                var start1 = richTextBox1.GetFirstCharIndexFromLine(line_index);  // Get the 1st char index of the appended text
                var length1 = command.Length;
                richTextBox1.Select(start1, length1);

                Font font = new Font(richTextBox1.Font.FontFamily, richTextBox1.Font.Size, FontStyle.Bold);
                richTextBox1.SelectionFont = font; // Bold
                richTextBox1.SelectionBackColor = Color.Transparent; // Unhighlight

                line_index += 1;
                
                executeline(command);
                System.Diagnostics.Debug.WriteLine("Executed: \"" + command + "\". " + "Line " + (line_index + 1) + " of " + numlines);
            }
            newline_index = line_index;
        }



            private void executeline(string line)
        {
            string[] words = line.Split(' '); // depends how many words to split

            string serverId;
            string URL;
            string partitionName;
            string username;
            string scriptFile;
            int r;

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            switch (words[0].ToLower())
            {
                case "replicationfactor":
                    //    if (words.Length == 2 && int.TryParse(words[1], out r))
                    //    {
                    //        ///???
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine("Wrong number of arguments!");
                    //    }
                    break;
                // Script starts with Server setup (Server and Partition commands)
                // create server process
                case "server":
                    int minDelay;
                    int maxDelay;
                    //TODO: send information about which partitions to store (this information must be created according to the Partition command)
                    if (words.Length == 5)
                    {
                        serverId = words[1];
                        URL = words[2];

                        int.TryParse(words[3], out minDelay);
                        int.TryParse(words[4], out maxDelay);

                        string address = string.Join(':', URL.Split(':').SkipLast(1).ToArray());
                        GrpcChannel channel = GrpcChannel.ForAddress(address + ':' + 10000);

                        PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);

                        ServerRequestObject request = new ServerRequestObject
                        {
                            ServerId = serverId,
                            Url = URL,
                            MaxDelay = maxDelay,
                            MinDelay = minDelay
                        };

                        foreach (string partition in partitions.Keys)
                        {
                            if (partitions[partition].Contains(serverId))
                            {

                                PartitionDetails p = new PartitionDetails();
                                p.Id = partition;
                                p.MasterId = partitions[partition][0];
                                for (int k = 1; k < partitions[partition].Count; k++)
                                {
                                    p.Replicas.Add(partitions[partition][k]);
                                }
                                request.Partitions.Add(p);
                            }
                        }
                        Console.WriteLine("Going to send server request");
                        ServerResponseObject result = node.ServerRequest(request);

                        Console.WriteLine(result);

                        if (result.Success)
                        {
                            servers.Add(new Server.ServerIdentification(serverId, URL));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of arguments!");
                    }
                    break;
                // configure system to store partition on given servers
                case "partition":
                    if (int.TryParse(words[1], out r) && words.Length == r + 3)
                    {
                        partitionName = words[2];
                        // do all r serverIds

                        List<string> serverlist = new List<string>();
                        for (int i = 0; i < r; i += 1)
                        {
                            serverlist.Add(words[i + 3]);
                        }
                        partitions.Add(partitionName, serverlist);
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of arguments!");
                    }
                    break;
                // create client process
                case "client":
                    //TODO: PASS EVERY PARTITION (ID, MASTERID) IN THE DICTIONARY TO THE EVERYPARTITION PARAMETER IN THE REQUEST
                    if (words.Length == 4)
                    {
                        username = words[1];
                        URL = words[2];
                        scriptFile = words[3];

                        string address = string.Join(':', words[2].Split(':').SkipLast(1).ToArray());
                        GrpcChannel channel = GrpcChannel.ForAddress(address + ':' + 10000);

                        PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);

                        ClientRequestObject request = new ClientRequestObject
                        {
                            ClientUrl = URL,
                            Username = username,
                            Scriptfile = scriptFile

                        };
                        foreach (string partition in partitions.Keys)
                        {
                            PartitionDetails p = new PartitionDetails();
                            p.Id = partition;
                            p.MasterId = partitions[partition][0];
                            for (int pi = 1; pi < partitions[partition].Count; pi++)
                            {
                                p.Replicas.Add(partitions[partition][pi]);
                            }
                            // all servers later
                            request.Everypartition.Add(p);
                        }
                        foreach (Server.ServerIdentification s in servers)
                        {
                            ServerDetails sd = new ServerDetails { Id = s.Id, Url = s.Ip };
                            request.EveryServer.Add(sd);
                        }

                        ClientResponseObject result = node.ClientRequest(request);

                        Console.WriteLine(result);
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of arguments!");
                    }
                    break;
                // all nodes print current status
                case "status":
                    //TODO: loop through all the servers and send status request
                    if (words.Length == 1)
                    {
                        StatusRequestObject request = new StatusRequestObject { };

                        // iterate and send to server itself
                        foreach (Server.ServerIdentification serv in servers)
                        {
                            //string sid = serv.Id;
                            string sip = serv.Ip;
                            string address = String.Join(':', sip.Split(':').SkipLast(1).ToArray());

                            GrpcChannel channel = GrpcChannel.ForAddress(address + ':' + 10000);
                            PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);
                            StatusResponseObject result = node.StatusRequest(request);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of arguments!");
                    }
                    break;

                // DEBUGGING COMMANDS
                // force process to crash
                case "crash":
                    if (words.Length == 2)
                    {
                        serverId = words[1];
                        CrashRequestObject request = new CrashRequestObject { ServerId = serverId };
                        bool found = false;
                        // send crash request to corresponding server address + port 10000
                        foreach (Server.ServerIdentification serv in servers)
                        {
                            if (serv.Id.Equals(serverId))
                            {
                                found = true;
                                string urll = serv.Ip;

                                // extracting address from url
                                string address = String.Join(':', urll.Split(':').SkipLast(1).ToArray());

                                // send crash request
                                GrpcChannel channel = GrpcChannel.ForAddress(address + ':' + 10000);
                                PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);
                                CrashResponseObject result = node.CrashRequest(request);
                                break;
                            }

                        }
                        if (!found)
                            Console.WriteLine("Server not found");
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of arguments!");
                    }
                    break;
                // simulate delay in process (stops processing messages until unfreeze received)
                case "freeze":
                    if (words.Length == 2)
                    {
                        serverId = words[1];
                        FreezeRequestObject request = new FreezeRequestObject
                        { };
                        // iterate over all the servers and send the freeze req
                        foreach (Server.ServerIdentification serv in servers)
                        {
                            if (serv.Id.Contains(serverId))
                            {
                                string urll = serv.Ip;

                                // extracting address from url
                                string address = String.Join(':', urll.Split(':').SkipLast(1).ToArray());

                                // send freeze request
                                GrpcChannel channel = GrpcChannel.ForAddress(address + ':' + 10000);
                                PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);
                                FreezeResponseObject result = node.FreezeRequest(request);
                            }
                            else
                                Console.WriteLine("Server not found");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of arguments!");
                    }
                    break;
                // process back to normal operation
                case "unfreeze":
                    if (words.Length == 2)
                    {
                        serverId = words[1];
                        UnfreezeRequestObject request = new UnfreezeRequestObject { };

                        // iterate over all the servers and send the unfreeze req
                        foreach (Server.ServerIdentification serv in servers)
                        {
                            if (serv.Id.Contains(serverId))
                            {
                                string urll = serv.Ip;

                                // extracting address from url
                                string address = String.Join(':', urll.Split(':').SkipLast(1).ToArray());

                                // send unfreeze request
                                GrpcChannel channel = GrpcChannel.ForAddress(address + ':' + 10000);
                                PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);
                                UnfreezeResponseObject result = node.UnfreezeRequest(request);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of arguments!");
                    }
                    break;
                case "wait":
                    int ms;
                    if (words.Length == 2)
                    {
                        if (int.TryParse(words[1], out ms))
                        {
                            Console.WriteLine("Waiting {0} ms", words[1]);
                            Thread.Sleep(ms);
                        }
                        else
                        {
                            Console.WriteLine("Invalid time arg!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of arguments!");
                    }

                    break;

                default:
                    Console.WriteLine("Invalid command!");
                    break;
            }
        //throw new NotImplementedException();
        }
 


        private void none(object sender, EventArgs e)
        {

        }

        private static PuppetMasterServices.PuppetMasterServicesClient PuppetMasterServicesClient()
        {

            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + 10000);
            PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);

            return node;
        }



        private static List<string> TransformCommands(List<string> loopCommands, int reps)
        {
            List<string> commands = new List<string>();
            for (int i = reps; i > 0; i--)
            {
                foreach (string s in loopCommands)
                {
                    commands.Add(Regex.Replace(s, @"\$i", i.ToString(), RegexOptions.None));
                }
            }
            return commands;

        }

        private static string GetElement(List<string> commands)
        {
            if (commands.Count == 0)
            {
                return null;
            }
            string res = commands[0];
            commands.RemoveAt(0);
            return res;
        }
    }
}
