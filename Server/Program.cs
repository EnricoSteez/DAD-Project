using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;

namespace Server
{

    public class Server
    {
        private static readonly Dictionary<string, ServerCoordinationServices.ServerCoordinationServicesClient> connections =
            new Dictionary<string, ServerCoordinationServices.ServerCoordinationServicesClient>();
        public Dictionary<string, Partition> Storage { get; }
        public Dictionary<string, ServerIdentification> SystemNodes { get; set; }

        public List<string> clients { get; set; }
        public string Server_id { get; }
        public  IPAddress Ip { get; }
        public int MinDelay;
        public int MaxDelay;
        private List<string> _isMasterOf;

        public static void Print(string id, string s)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("Server " + id + ":   " + s);
        }
        public ServerCoordinationServices.ServerCoordinationServicesClient RetrieveServer(string serverId)
        {
            if (!connections.ContainsKey(serverId))
            {
                connections.Add(serverId, new ServerCoordinationServices.ServerCoordinationServicesClient(
                    GrpcChannel.ForAddress(SystemNodes[serverId].Ip)));
            }

            return connections[serverId];
        }


        public Server() //dummy implementation for debugging
        {
            Server_id = "1";
            Ip = IPAddress.Parse("127.0.0.1");
            //empty storage at startup
            Storage = new Dictionary<string, Partition>();
            //empty dictionary at startup
            SystemNodes = new Dictionary<string, ServerIdentification>();
            clients = new List<string>();
            _isMasterOf = new List<string>();
        }

        public Server(string server_id, string ip, int minDelay, int maxDelay)
        {
            Server_id = server_id;
            if(ip.Equals("localhost"))
            {
                ip = "127.0.0.1";
            }
            Ip = IPAddress.Parse(ip);
            Storage = new Dictionary<string, Partition>();
            SystemNodes = new Dictionary<string, ServerIdentification>();
            clients = new List<string>();
            MinDelay = minDelay;
            MaxDelay = maxDelay;
            _isMasterOf = new List<string>();
        }

        internal int AddObject(Resource newValue, string partitionId)
        {
            //lock storage
            Monitor.Enter(Storage);
            if (Storage.ContainsKey(partitionId)) //update resource
            {
                Partition p = Storage[partitionId];
                Monitor.Exit(Storage);

                //lock partition
                Monitor.Enter(p);
                int newVersion;
                if (p.Elements.ContainsKey(newValue.ObjectId))
                {
                    Resource res = p.Elements[newValue.ObjectId];
                    Monitor.Exit(p);

                    //lock resource
                    Monitor.Enter(res);

                    if(newValue.Version == -1) //local addObject with already present resource -> increment version
                    {
                        newVersion = res.Version + 1;
                        res.Version++;
                    } else //update from the master -> version != -1 -> copy version
                    {
                        //if(newValue.Version > res.Version) always true because it's comimg from the master
                        
                        newVersion = newValue.Version;
                        res = newValue;
                    }
                    
                    Monitor.Exit(res);
                }
                else //add new resource to the Elements of the correct Partition
                {
                    if (newValue.Version == -1) //first propagation by the master -> version = 1
                    {
                        newVersion = 1;
                        newValue.Version = 1;
                    } else //in case I missed the first propagation, should never happen
                    {
                        newVersion = newValue.Version;
                    }

                    p.Elements.Add(newValue.ObjectId, newValue);

                    Monitor.Exit(p);

                }

                return newVersion;
            }
            else
            {
                Monitor.Exit(Storage);
            }
            
            return -1;
        }

        internal Resource RetrieveObject(string id, string partitionId, int lastVersion)
        {
            Monitor.Enter(Storage);
            if (Storage.ContainsKey(partitionId))
            {
                Partition p = Storage[partitionId];
                Monitor.Exit(Storage);

                Monitor.Enter(p);
                if (p.Elements.ContainsKey(id))
                {
                    Resource res = p.Elements[id];
                    Monitor.Exit(p);

                    Monitor.Enter(res);
                    
                    if (res.Locked) //if the resource is locked I wait for a pulse on this resource from the Unlock function 
                    {
                        Monitor.Exit(res);
                        //leave the lock and wait for someone to unflag
                        while (res.Locked)
                        {
                            Monitor.Wait(res);
                        }

                    }

                    //everything ok: resource present and more recent version
                    if(res.Version > lastVersion)
                    {
                        return res;
                    }
                    else //resource present but older version: not updated from the master yet
                    {
                        return new Resource("OLDER VERSION", "OLDER VERSION") { Version = 0 };

                    }
                }
                else //the item requested from partition partitionId is not in that partition
                {
                    Monitor.Exit(p);
                }
            }
            else //the partition requested is not in this server's storage (this server is not a replica for this partition)
            {
                Monitor.Exit(Storage);
            }
            return new Resource("N/A", "N/A") { Version = 0 }; //no such resource on this server
        }

        internal bool LockObject(string id, string partitionId)
        {
            Monitor.Enter(Storage);
            if (Storage.ContainsKey(partitionId))
            {
                Partition p = Storage[partitionId];
                Monitor.Exit(Storage);


                Monitor.Enter(p);
                if (p.Elements.ContainsKey(id))
                {
                    Resource res = p.Elements[id];
                    Monitor.Exit(p);

                    Monitor.Enter(res);

                    if (!res.Locked)
                    {
                        Server.Print(this.Server_id, String.Format("Thread {0} just got the permission for locking resource {1} in Partition {2}",
                        Thread.CurrentThread.Name, id, partitionId));

                        res.Locked = true;
                        Monitor.Exit(res);
                    }
                    else//resource is already locked by someone else
                        //so I wait for him to release the lock and then I lock it myself
                    {
                        Monitor.Exit(res);
                        //leave the lock and wait for someone to unflag
                        while (res.Locked)
                        {
                            Monitor.Wait(res);
                        }

                        //re-obtain the lock and flag myself

                        Monitor.Enter(res);
                        res.Locked = true;
                        Monitor.Exit(res);

                    }

                    return true;
                }
                else
                {
                    Monitor.Exit(p);
                }

            }
            else
            {
                Monitor.Exit(Storage);
            }

            return false;
        }

        internal bool UnlockObject(string id, string partitionId)
        {
            Monitor.Enter(Storage);
            if (Storage.ContainsKey(partitionId))
            {
                Partition p = Storage[partitionId];
                Monitor.Exit(Storage);

                Monitor.Enter(p);
                if (p.Elements.ContainsKey(id))
                {
                    Resource res = p.Elements[id];
                    Monitor.Exit(p);

                    Monitor.Enter(res);
                    if (res.Locked)
                        res.Locked = false;

                    Monitor.Exit(res);

                    //awake the processes sleeping on this resource
                    Monitor.Pulse(res);

                    return true;
                }
                else
                {
                    Monitor.Exit(p);
                }
                
            } else
            {
                Monitor.Exit(Storage);
            }

            return false;
        }

        /*
         * this special function is used in the server side only protocol for updating a value in the replicas
         * When the master asks its replicas to lock a file, he then sends them the updated resource.
         * Being the resource locked on the replicas due to the previous step, they can't update it locally 
         * and this causes a deadlock.
         * With this function, the replicas can update that value even if it's locked 
         * because it's updated by the same server that requested the lock for this specific purpose
         * (use carefully)
         */
        internal bool UpdateSpecialPermission(Resource resource, string partitionId)
        {
            Monitor.Enter(Storage[partitionId]);
            Storage[partitionId].Elements[resource.ObjectId] = resource;
            Monitor.Exit(Storage[partitionId]);

            return true;
        }


        public bool UpdateMaster(string partitionId, string newMaster)
        {
            Monitor.Enter(Storage[partitionId]);
            Storage[partitionId].MasterId = newMaster;
            Monitor.Exit(Storage[partitionId]);

            return true;
        }


        public void AddPartition(Partition p)
        {
            Monitor.Enter(Storage);

            Storage.Add(p.Id, p);

            Monitor.Exit(Storage);

            if (p.MasterId == Server_id)
            {
                _isMasterOf.Add(p.Id);
            }
            
        }

        public void AddClient(string url)
        {
            Monitor.Enter(clients);
            clients.Add(url);
            Monitor.Exit(clients);

            
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {


            Dictionary<string, List<string>> partitions = new Dictionary<string, List<string>>(); //key - partitionId object - list of servers with that partition

            //args: id url minDelay maxDelay            

            /*
                * this is working only if PCS passes as arguments only the actually stored partitions
                * and not all the partitions of the system
                */
            if (args.Length != 6)
            {
                Server.Print("unknown", "Wrong number of args");
                return;
            }

            string id = args[0];
            string url = args[1];
            int.TryParse(args[2], out int minDelay);
            int.TryParse(args[3], out int maxDelay);
            string serversFile = args[4];
            string partitionsFile = args[5];

            /*parse serialized dictionaries*/

            BinaryFormatter bf = new BinaryFormatter();

            FileStream partfsin = new FileStream(partitionsFile, FileMode.Open, FileAccess.Read, FileShare.None);
            FileStream servfsin = new FileStream(serversFile, FileMode.Open, FileAccess.Read, FileShare.None);

            partitions = (Dictionary<string, List<string>>)bf.Deserialize(partfsin);


            /////////////////

            url = url.Split("//")[1];
            Server init = new Server(id,url.Split(":")[0],minDelay,maxDelay);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            foreach(string partId in partitions.Keys)
            {
                init.AddPartition(new Partition(partId,partitions[partId][0]));
            }
            Server.Print(id, serversFile);
            init.SystemNodes = (Dictionary<string, ServerIdentification>)bf.Deserialize(servfsin);  //key - serverId value - server url

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services =
                {
                    ServerStorageServices.BindService(new ServerClientService(init)),
                    ServerCoordinationServices.BindService(new ServerServerService(init)),
                    ElectionServices.BindService(new ElectionServicesClass(init))
                    //TODO add PuppetMasterServices.BindService()
                },

                Ports = { new ServerPort("127.0.0.1",  int.Parse(url.Split(":")[1]), ServerCredentials.Insecure) }
            };

            try
            {
                server.Start();
            }
            catch (Exception e)
            {
                Server.Print(id, e.Message);
            }
            Console.ReadLine();


        }
    }
}
