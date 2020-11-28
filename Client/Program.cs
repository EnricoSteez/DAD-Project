using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Dataflow;


namespace Client
{
    class Program
    {
        private static int inc = 1;
        private static Dictionary<string, GrpcServer> servers = new Dictionary<string, GrpcServer>();
        
        private static Dictionary<string, List<string>> partitions =  new Dictionary<string, List<string>>(); //key - partitionId object - list of servers with that partition

        private static Dictionary<string, int> lastKnownObjects = new Dictionary<string, int>(); //(ObjectIDs,LastKnownVersions)

        private static ServerStorageServices.ServerStorageServicesClient currentServer = null;
        private static string currentServerId = null;

        private static ServerStorageServices.ServerStorageServicesClient RetrieveServer(string serverId)
        {
            GrpcServer res = null;
            if (servers.TryGetValue(serverId, out res))
            {
                return res.Service;
            }
            res = new GrpcServer("http://localhost:" + (1000 + inc++));
            servers.Add(serverId, res);
            return res.Service;
        }


        private static List<string> TransformCommands(List<string> loopCommands, int reps)
        {
            
            List<string> commands = new List<string>();
            for(int i = 1; i <= reps; i++)
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
            if(commands.Count == 0)
            {
                return null;
            }
            string res = commands[0];
            commands.RemoveAt(0);
            return res;
        }


        static void Main(string[] args)
        {
            servers.Add("1", new GrpcServer("http://127.0.0.1:1001"));
            servers.Add("2", new GrpcServer("http://127.0.0.1:1002"));
            servers.Add("3", new GrpcServer("http://127.0.0.1:1003"));
            servers.Add("4", new GrpcServer("http://127.0.0.1:1004"));
            servers.Add("5", new GrpcServer("http://127.0.0.1:1005"));
            partitions.Add("p1", new List<string>{ "1", "2", "3" });
            partitions.Add("p2", new List<string> { "2", "4", "5" });
            string fileName = @"../../../test.txt";
            if (args.Length == 1)
            {
                fileName = args[0];
            }
            int counter = 0;
            string line;

            List<string> loopCommands = new List<string>();
            List<string> commands = new List<string>();

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            while ((line = GetElement(commands)) != null || (line = file.ReadLine()) != null)
            {
                string[] words = line.Split(' ', 4);
                string objectId;
                string partitionId;
                string serverId;
                int rep = 1;
                switch (words[0]) {
                    case "read":
                        if (words.Length != 4)
                        {
                            Console.WriteLine("Wrong number of args!");
                            break;
                        }
                        partitionId = words[1];
                        objectId = words[2];
                        serverId = words[3];

                        //here I could also only check != -1 and assume that I never read before being connected to anyone
                        //as it wouldn't make sense to read before inserting any objects
                        //and as soon as I insert an object I'm connected to someone ;)
                        if(serverId != "-1" || currentServer==null)
                        {
                            currentServer = RetrieveServer(serverId);
                        }

                        ReadObjectResponse readResponse = null;
                        bool retry = true;
                        int attempt = 0;
                        string firstAttempt = currentServerId;

                        int lastKnownVersion = lastKnownObjects.GetValueOrDefault(objectId,0);

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
                                readResponse = currentServer.ReadObject(readRequest);
                                retry = false;
                            }
                            catch (RpcException ex) when (ex.StatusCode == StatusCode.Internal)
                            {
                                //just in case it got modified with random return values anyways
                                readResponse = null;

                                if (attempt < partitions[partitionId].Count)
                                {
                                    string newAttemptId = partitions[partitionId][attempt];
                                    //It's geeky but we could avoid trying again the first server that we already know it's unreachable
                                    //we simply skip it when we encounter the id, only if it's not already the last of the list
                                    if (newAttemptId == firstAttempt)
                                    {
                                        //if I can skip it and still find at least another server in the list
                                        if(attempt < partitions[partitionId].Count-1)
                                        {
                                            attempt++;
                                            newAttemptId = partitions[partitionId][attempt];
                                        } else //after this (which I know is unreachable) there's nothing else to try
                                        {
                                            Console.WriteLine("No more servers available, sorry!");
                                            break;
                                        }
                                    }

                                    Console.WriteLine("Server Unreachable, trying with {0}", newAttemptId);
                                    currentServer = RetrieveServer(newAttemptId);
                                    currentServerId = newAttemptId;
                                    attempt++;
                                    retry = true;
                                }
                                else
                                {
                                    Console.WriteLine("No more servers available, sorry!");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Unexpected Error!!\n{0}", ex.Message);
                            }
                        }

                        if(readResponse != null)
                        {
                            if(readResponse.Id == "OLDER VERSION" && readResponse.Value == "OLDER VERSION" && readResponse.Version == "OLDER VERSION")
                                Console.WriteLine("The server only has an older version of this object");
                            else
                                Console.WriteLine("Object {0} read: {1}   V{2}",readResponse.Id, readResponse.Value, readResponse.Version);
                        }
                        else
                        {
                            Console.WriteLine("Error getting reply");
                        }
                           
                        break;
                    case "write":

                        if (words.Length != 4)
                        {
                            Console.WriteLine("Wrong number of args!");
                            break;
                        }

                        partitionId = words[1];
                        objectId = words[2];
                        string value = words[3];

                        if(currentServerId!= partitions[partitionId][0])
                        {
                            currentServer = RetrieveServer(partitions[partitionId][0]);
                            currentServerId = partitions[partitionId][0];
                        }

                        // the version of the updates is handled server side,
                        // no version in the request
                        WriteObjectRequest writeRequest = new WriteObjectRequest
                        {
                            PartitionId = partitionId,
                            ObjectId = objectId,
                            Value = value
                        };

                        WriteObjectResponse writeResponse = currentServer.WriteObject(writeRequest);

                        Console.WriteLine("Write object {0} result: {1}", objectId, writeResponse.WriteResult);

                        break;
                    case "listServer":
                        if(words.Length == 2 )
                        {
                            serverId = words[1];
                            try
                            {
                                ListServerResponse reply = RetrieveServer(serverId).ListServer(new ListServerRequest { });
                                Console.WriteLine("Objects:");
                                IEnumerator<string> objects = reply.StoredObjects.GetEnumerator();
                                IEnumerator<bool> isMasterReplica = reply.IsMasterReplica.GetEnumerator();
                                foreach (string s in reply.StoredObjects)
                                {
                                    isMasterReplica.MoveNext();
                                    Console.WriteLine("Object: {0}, is server {1} master replica? {2}", s, serverId,isMasterReplica.Current);
                                }

                                for(int i = 0; i < reply.StoredObjects.Count; i++)
                                {
                                    Console.WriteLine("Object: {0} v:{1}, is {2} master replica? {3}",
                                        reply.StoredObjects[i], reply.Versions[i], serverId, reply.IsMasterReplica[i]);
                                }
                            }
                            catch (RpcException exx) when (exx.StatusCode == StatusCode.Internal)
                            {
                                Console.WriteLine("server unreachable");
                            }
                            catch (Exception exx)
                            {
                                Console.WriteLine("Unexpected Error!!\n{0}", exx.Message);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "listGlobal": //TODO Implement here the connection to every server, find code commented at the bottom
                        //the response from each server (ListGlobal service) contains a list of ObjectIDs 
                        //and a list with Versions. Every position of the two lists corresponds: (Object-Version)

                        if (words.Length == 1 )
                        {
                            foreach(string serv in servers.Keys)
                            {
                                try
                                {
                                    ListGlobalResponse reply = RetrieveServer(serv).ListGlobal(new ListGlobalRequest { });
                                    Console.WriteLine("Partitions:");
                                    foreach (PartitionIdentification p in reply.Partitions)
                                    {
                                        Console.WriteLine("->{0}", p.PartitionId);
                                        Console.WriteLine("\tObjects:");
                                        foreach(string oid in p.ObjectIds)
                                        {
                                            Console.WriteLine("\t\t{0}", oid);
                                        }
                                    }
                                    break;
                                }
                                catch (RpcException exx) when (exx.StatusCode == StatusCode.Internal)
                                {
                                    Console.WriteLine("server unreachable");
                                }
                                catch (Exception exx)
                                {
                                    Console.WriteLine("Unexpected Error!!\n{0}", exx.Message);
                                }
                            }
                            
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "wait":
                        int ms;
                        if (words.Length == 2 && int.TryParse(words[1], out ms) )
                        {

                            Console.WriteLine("Wainting {0} ms", words[1]);
                            Thread.Sleep(ms);
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
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
                                Console.WriteLine("Invalid number of repetitions");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid repetition!");
                        }
                        break;
                    default:
                        Console.WriteLine("Invalid command!");
                        break;

                }
                counter++;
            }
        }
    }
}
/*
List<Task> tasks = new System.Collections.Generic.List<Task>();
int failed = 0;

foreach (ServerIdentification server in Local.SystemNodes.Values)
{
    GrpcChannel channel = GrpcChannel.ForAddress(
                "http://" + server.Ip + ":" + (1000 + int.Parse(server.Id)).ToString());


    ServerCoordinationServices.ServerCoordinationServicesClient client =
        new ServerCoordinationServices.ServerCoordinationServicesClient(channel);

    SendInfoResponse res = null;


    tasks.Add(Task.Run(() => {
        try
        {
            SendInfoResponse res = client.SendInfo(new SendInfoRequest(), deadline: DateTime.UtcNow.AddSeconds(5));
            Console.WriteLine("Conected to server {0}", server.Id);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            Interlocked.Increment(ref failed);
            Console.WriteLine("Timeout.");
        }
        catch (Exception e)
        {
            Interlocked.Increment(ref failed);
            Console.WriteLine("Error: " + e.StackTrace);
        }
    }));

    Task union = Task.WhenAll(tasks);

    union.Wait();

    if (failed > 0)
    {
        Console.WriteLine("Failed: " + failed);
    }

    foreach (PartitionID pid in res.Partitions)
    {
        bool toInsert = true;
        foreach (PartitionIdentification partitionIdentification in response.Partitions)
        {
            if (pid.PartitionId.Equals(partitionIdentification.PartitionId))
            {
                toInsert = false;
            }
        }
        if (toInsert)
        {
            PartitionIdentification p = new PartitionIdentification
            {
                PartitionId = pid.PartitionId
            };


            p.ObjectIds.Add(pid.ObjectIds);

            response.Partitions.Add(p);

        }
    }

    channel.ShutdownAsync();

}
*/