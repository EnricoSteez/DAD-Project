using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace Client
{
    class Program
    {
        private static string url;
        private static string username;
        private static int inc = 1;
        private static Dictionary<string, GrpcServer> servers = new Dictionary<string, GrpcServer>();

        public static Dictionary<string, List<string>> partitions = new Dictionary<string, List<string>>(); //key - partitionId object - list of servers with that partition

        private static Dictionary<string, int> lastKnownObjects = new Dictionary<string, int>(); //(ObjectIDs,LastKnownVersions)

        private static ServerStorageServices.ServerStorageServicesClient currentServer = null;
        private static string currentServerId = null;

        
        public static void Print(string s)
        {

            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Client " + username + ":   " + s);

        }

        private static ServerStorageServices.ServerStorageServicesClient RetrieveServer(string serverId)
        {

            if(serverId.Equals("-1"))
            {
                throw new Exception();
            }

            GrpcServer res;

            if (servers.TryGetValue(serverId, out res))
            {
                Program.Print("retrieve server will return: " + res.ToString() + " (server " + serverId + ")");
                return res.Service;
            }
            else
            {
                Program.Print("Server" + serverId + "not in list");
                return null;
            }


        }

        private static bool ElectNewMaster(string partitionId)
        {

            if(partitions[partitionId].Count < 2)
            {
                return false;
            }
            Monitor.Enter(partitions[partitionId]);
            List<string> partitionservers = partitions[partitionId].Skip(1).ToList();
            Monitor.Exit(partitions[partitionId]);
            partitionservers.Sort();
            for (int sindex = 1; sindex < partitionservers.Count; sindex++)
            {
                currentServerId = partitionservers[sindex];
                GrpcServer servvvv;
                if (servers.TryGetValue(currentServerId, out servvvv))
                {
                    GrpcChannel c = GrpcChannel.ForAddress(servvvv.Url);
                    ElectionServices.ElectionServicesClient service = new ElectionServices.ElectionServicesClient(c);
                    try
                    {
                        ChooseMasterResponse resp = service.ChooseMaster(new ChooseMasterRequest { PartitionId = partitionId });
                        Monitor.Enter(Program.partitions[partitionId]);

                        if (Program.partitions[partitionId][0] != currentServerId)
                        {
                            string oldMaster = Program.partitions[partitionId][0];
                            Program.partitions[partitionId].RemoveAt(0);
                            Program.partitions[partitionId].Insert(0, currentServerId);
                            Program.partitions[partitionId].Add(oldMaster);

                            Program.Print(Program.partitions[partitionId].ToString());
                        }
                        Monitor.Exit(Program.partitions[partitionId]);
                        Print("elected server " + currentServerId + "as the new master of the partition " + partitionId);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Program.Print("Could not elect server " + currentServerId);
                    }
                }

                
            }
            return false;

        }


        private static List<string> TransformCommands(List<string> loopCommands, int reps)
        {
            List<string> commands = new List<string>();
            for (int i = 1; i <= reps; i++)
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


        static void Main(string[] args)
        {




            Dictionary<string, string> simpleservers = new Dictionary<string, string>();
            string fileName = @"../../../test.txt";
            url = "localhost";
            username = "john";
            string partitionsFile = "partitions.binary";
            string serversFile = "servers.binary";
            if (args.Length == 5)
            {
                fileName = args[0];
                url = args[1];
                username = args[2];
                partitionsFile = args[3];
                serversFile = args[4];
            }


            Program.Print(partitionsFile + " " + serversFile);


            /*parse serialized dictionaries*/

            BinaryFormatter bf = new BinaryFormatter();

            FileStream partfsin = new FileStream(partitionsFile, FileMode.Open, FileAccess.Read, FileShare.None);
            FileStream servfsin = new FileStream(serversFile, FileMode.Open, FileAccess.Read, FileShare.None);

            partitions = (Dictionary<string, List<string>>)bf.Deserialize(partfsin);
            simpleservers = (Dictionary<string, string>)bf.Deserialize(servfsin);  //key - serverId value - server url

            foreach (string ss in simpleservers.Keys)
            {
                servers.Add(ss, new GrpcServer(simpleservers[ss]));
            }
            int counter = 0;
            string line;


            /////////////////

            List<string> loopCommands = new List<string>();
            List<string> commands = new List<string>();


            List<Task> registerRequestTasks = new List<Task>();
            bool updated = false;
            foreach (GrpcServer s in servers.Values)
            {

                registerRequestTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        Print("i will register in server " + s.Url);
                        RegisterResponse res = s.Service.Register(new RegisterRequest { Url = url }, deadline: DateTime.UtcNow.AddSeconds(8));
                        Print("register response:::  " + res.NewMasters.ToString());
                        if (!updated)
                        {
                            
                            foreach (NewMastersStructure nm in res.NewMasters)
                            {
                                string partitionId = nm.PartitionId;
                                string serverId = nm.ServerId;
                                Monitor.Enter(Program.partitions[partitionId]);
                                string oldMaster = Program.partitions[partitionId][0];
                                partitions[partitionId].RemoveAt(0);
                                partitions[partitionId].Insert(0, serverId);
                                partitions[partitionId].Add(oldMaster);
                                Monitor.Exit(Program.partitions[partitionId]);
                                Print("Updated " + partitionId + " with new master " + serverId);
                            }
                            updated = true;
                        }
                    } 
                    catch(RpcException ex) when(ex.StatusCode == StatusCode.DeadlineExceeded || ex.StatusCode == StatusCode.Unknown || ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.Internal)
                    {
                        Program.Print("Server " + s.Url + " unavailable");
                    }
                }));
            }
            Task union = Task.WhenAll(registerRequestTasks);
            union.Wait();



            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services =
                {
                    ElectionServices.BindService(new ElectionServicesClass())
                    //TODO add PuppetMasterServices.BindService()
                },

                Ports = { new ServerPort("127.0.0.1", int.Parse(url.Split(":")[2]), ServerCredentials.Insecure) }
            };

            try
            {
                server.Start();
            }
            catch (Exception e)
            {
                Program.Print(e.Message);
            }

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            while ((line = GetElement(commands)) != null || (line = file.ReadLine()) != null)
            {
                string[] words = line.Split(' ', 4);
                string objectId;
                string partitionId;
                string serverId;
                int rep = 1;
                switch (words[0])
                {
                    case "read":
                        if (words.Length != 4)
                        {
                            Program.Print("Wrong number of args!");
                            break;
                        }
                        partitionId = words[1];
                        objectId = words[2];
                        serverId = words[3];

                        //here I could also only check != -1 and assume that I never read before being connected to anyone
                        //as it wouldn't make sense to read before inserting any objects
                        //and as soon as I insert an object I'm connected to someone ;)
                        if (!serverId.Equals("-1") || currentServer == null)
                        {
                            currentServer = RetrieveServer(serverId);
                            currentServerId = serverId;
                        }

                        ReadObjectResponse readResponse = null;
                        bool retry = true;
                        int attempt = 0;
                        string firstAttempt = currentServerId;

                        int lastKnownVersion = lastKnownObjects.GetValueOrDefault(objectId, 0);

                        ReadObjectRequest readRequest = new ReadObjectRequest
                        {
                            PartitionId = partitionId,
                            ObjectId = objectId,
                            LastVersion = lastKnownVersion
                        };

                        while (retry)
                        {
                            try
                            {
                                if(currentServer == null)
                                {
                                    throw new RpcException (new Status(StatusCode.Internal, "server not found"));
                                }
                                readResponse = currentServer.ReadObject(readRequest);
                                retry = false;
                            }
                            catch (RpcException ex) when (ex.StatusCode == StatusCode.Internal || ex.StatusCode == StatusCode.Unknown || ex.StatusCode == StatusCode.Unavailable)
                            {
                                //just in case it got modified with random return values anyways
                                readResponse = null;
                                Monitor.Enter(partitions[partitionId]);
                                if (attempt < partitions[partitionId].Count)
                                {
                                    string newAttemptId = partitions[partitionId][attempt];
                                    //It's geeky but we could avoid trying again the first server that we already know it's unreachable
                                    //we simply skip it when we encounter the id, only if it's not already the last of the list
                                    if (newAttemptId == firstAttempt)
                                    {
                                        //if I can skip it and still find at least another server in the list
                                        if (attempt < partitions[partitionId].Count - 1)
                                        {
                                            attempt++;
                                            newAttemptId = partitions[partitionId][attempt];
                                        }
                                        else //after this (which I know is unreachable) there's nothing else to try
                                        {
                                            Program.Print("No more servers available, sorry!");
                                            break;
                                        }
                                    }

                                    
                                    
                                    Program.Print(String.Format("Server Unreachable, trying with {0}", newAttemptId));
                                    currentServer = RetrieveServer(newAttemptId);
                                    currentServerId = newAttemptId;
                                    attempt++;
                                    retry = true;
                                }
                                else
                                {
                                    Program.Print("No more servers available, sorry!");
                                    break;
                                }
                                Monitor.Exit(partitions[partitionId]);
                            }
                            catch (Exception ex)
                            {
                                Program.Print("Unexpected Error!!\n " + ex.Message);
                            }
                        }

                        // print results
                        if (readResponse != null)
                        {
                            if (readResponse.Id == "OLDER VERSION" && readResponse.Value == "OLDER VERSION")
                                Program.Print("The server only has an older version of this object");
                            else if (readResponse.Id == "N/A" && readResponse.Value == "N/A")
                                Program.Print("The server doesn't store the required object");
                            else
                            {
                                Program.Print(String.Format("Object {0} read: {1}   V{2}", readResponse.Id, readResponse.Value, readResponse.Version));
                                if (lastKnownObjects.ContainsKey(readResponse.Id))
                                {
                                    lastKnownObjects[readResponse.Id] = readResponse.Version;
                                }
                                else
                                {
                                    lastKnownObjects.Add(readResponse.Id, readResponse.Version);
                                }
                            }
                        }
                        else
                        {
                            Program.Print("Error getting reply");
                        }

                        break;
                    case "write":
                        if (words.Length != 4)
                        {
                            Program.Print("Wrong number of args!");
                            break;
                        }

                        partitionId = words[1];
                        objectId = words[2];
                        string value = words[3];
                        Monitor.Enter(partitions[partitionId]);
                        if (currentServerId != partitions[partitionId][0])
                        {
                            currentServer = RetrieveServer(partitions[partitionId][0]);
                            currentServerId = partitions[partitionId][0];
                        }
                        Monitor.Exit(partitions[partitionId]);



                        // the version of the updates is handled server side,
                        // no version in the request
                        WriteObjectRequest writeRequest = new WriteObjectRequest
                        {
                            PartitionId = partitionId,
                            ObjectId = objectId,
                            Value = value
                        };
                        Program.Print("will write to: " + currentServerId);

                        try
                        {
                            if(currentServer == null)
                            {
                                throw new RpcException(new Status(StatusCode.Internal, "server not found"));
                            }
                            WriteObjectResponse writeResponse = currentServer.WriteObject(writeRequest, deadline:DateTime.UtcNow.AddSeconds(8));
                            Program.Print(String.Format("Write object {0} result: {1}", objectId, writeResponse.WriteResult));

                        }
                        catch (RpcException exx) when (
                                      exx.StatusCode == StatusCode.Unknown ||
                                      exx.StatusCode == StatusCode.Unavailable || exx.StatusCode == StatusCode.DeadlineExceeded || exx.StatusCode == StatusCode.Internal)
                        {

                            

                            if (exx.StatusCode == StatusCode.Unknown || exx.StatusCode == StatusCode.Unavailable || exx.StatusCode == StatusCode.Internal)
                            {
                                Program.Print(String.Format("Server {0} Unreachable, say Goodbye before it's too late...", currentServerId));
                                //ADIOS
                                servers.Remove(currentServerId);
                            }
                            else
                            {
                                Program.Print(String.Format("Server {0} Frozen", currentServerId));
                            }
                            Program.Print("will try to elect another server");
                            if(ElectNewMaster(partitionId))
                            {
                                Monitor.Enter(partitions[partitionId]);
                                currentServer = RetrieveServer(partitions[partitionId][0]);
                                currentServerId = partitions[partitionId][0];
                                Monitor.Exit(partitions[partitionId]);
                                try
                                {
                                    Program.Print("will write to: " + currentServerId);
                                    WriteObjectResponse writeResponse = currentServer.WriteObject(writeRequest, deadline: DateTime.UtcNow.AddSeconds(8));
                                    Program.Print(String.Format("Write object {0} result: {1}", objectId, writeResponse.WriteResult));

                                }
                                catch (RpcException exxx) when (
                                      exxx.StatusCode == StatusCode.Unknown ||
                                      exxx.StatusCode == StatusCode.Unavailable || exx.StatusCode == StatusCode.DeadlineExceeded)
                                {
                                    Program.Print(String.Format("Server {0} Unreachable, say Goodbye before it's too late...", currentServerId));
                                }
                            }
                        }
                        break;
                    case "listServer":
                        if (words.Length == 2)
                        {
                            serverId = words[1];

                            if (currentServerId != serverId)
                            {
                                currentServer = RetrieveServer(serverId);
                                currentServerId = serverId;
                            }

                            try
                            {
                                if(currentServer == null)
                                {
                                    throw new RpcException(new Status(StatusCode.Internal, "server not found"));
                                }
                                ListServerResponse reply = currentServer.ListServer(new ListServerRequest { });

                                //PRINT RESULTS
                                Program.Print("Objects:");

                                foreach (ListServerResource r in reply.Objects)
                                {
                                    Program.Print(String.Format("Object: {0} V{1}, master: {2}", r.Id, r.Version, r.IsMasterReplica));
                                }

                            }
                            catch (RpcException exx) when (
                                        exx.StatusCode == StatusCode.Unknown ||
                                        exx.StatusCode == StatusCode.Unavailable || exx.StatusCode == StatusCode.Internal)
                            {
                                Program.Print(String.Format("Server {0} Unreachable, say Goodbye before it's too late...", currentServerId));
                                //farewell champ
                                servers.Remove(currentServerId);
                            }
                            catch (Exception exx)
                            {
                                Program.Print("Unexpected Error!!\n" + exx.Message);
                            }
                        }
                        else
                        {
                            Program.Print("Wrong number of args!");
                        }
                        break;
                    case "listGlobal":

                        if (words.Length == 1)
                        {
                            List<Task> tasks = new List<Task>();

                            ListGlobalResponse reply = null;

                            foreach (string serv in servers.Keys)
                            {
                                Program.Print("asking server " + serv);
                                currentServer = RetrieveServer(serv);
                                try
                                {
                                    if(currentServer == null)
                                    {
                                        throw new RpcException(new Status(StatusCode.Internal, "server not found"));
                                    }
                                    currentServerId = serv;
                                    reply = currentServer.ListGlobal(new ListGlobalRequest { });
                                    Program.Print("asked " + currentServerId);
                                    Program.Print(String.Format("Server {0} Partitions:", currentServerId));
                                    foreach (PartitionIdentification p in reply.Partitions)
                                    {
                                        Program.Print(String.Format("->{0}", p.PartitionId));
                                        Program.Print("\tObjects:");
                                        for (int i = 0; i < p.ObjectIds.Count; i++)
                                        {
                                            Program.Print(String.Format("{0}\tV{1}", p.ObjectIds[i], p.Versions[i]));
                                        }
                                    }
                                }
                                catch (RpcException exx) when (
                                    exx.StatusCode == StatusCode.Unknown ||
                                    exx.StatusCode == StatusCode.Unavailable ||
                                    exx.StatusCode == StatusCode.DeadlineExceeded || exx.StatusCode == StatusCode.Internal)
                                {
                                    Program.Print(string.Format("Server {0} Unreachable, say Goodbye before it's too late...", currentServerId));
                                    //goodbye sweetheart
                                    servers.Remove(currentServerId);
                                }
                                catch (Exception exx)
                                {
                                    Program.Print("Unexpected Error!!\n" + exx.Message);
                                }


                                
                            }
                        }
                        else
                        {
                            Program.Print("Wrong number of args!");
                        }
                        break;
                    case "wait":
                        int ms;
                        if (words.Length == 2 && int.TryParse(words[1], out ms))
                        {

                            Program.Print(String.Format("Waiting {0} ms", words[1]));
                            Thread.Sleep(ms);
                        }
                        else
                        {
                            Program.Print("Wrong number of args!");
                        }
                        break;
                    case "begin-repeat":
                        if (commands.Count == 0 && words.Length == 2 && int.TryParse(words[1], out rep))
                        {
                            if (rep > 0)
                            {
                                string lineaux = file.ReadLine();
                                while (lineaux != null && lineaux != "end-repeat")
                                {
                                    loopCommands.Add(lineaux);
                                    lineaux = file.ReadLine();
                                }
                                commands = TransformCommands(loopCommands, rep);
                            }
                            else
                            {
                                rep = 0;
                                Program.Print("Invalid number of repetitions");
                            }
                        }
                        else
                        {
                            Program.Print("Invalid repetition!");

                        }
                        break;
                    default:
                        Program.Print("Invalid command!");
                        break;

                }
                counter++;
            }
        }
    }
}