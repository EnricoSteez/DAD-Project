using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Grpc.Core;
using Server.protos;

namespace Server
{

    public class Server
    {
        private Dictionary<string, Resource> Storage { get; }
        public Dictionary<string, ServerIdentification> SystemNodes { get; set; }
        public string Server_id { get; }
        public string Partition_id { get; }
        public bool IsMasterReplica { get; }
        public  IPAddress Ip { get; }
        public int MinDelay;
        public int MaxDelay;


        public Server() //dummy implementation for debugging with just 1 server at localhost
        {
            Server_id = "1";
            Partition_id = "A";
            IsMasterReplica = true;
            Ip = IPAddress.Parse("127.0.0.1");
            //empty storage at startup
            Storage = new Dictionary<string, Resource>();
            //empty dictionary at startup
            SystemNodes = new Dictionary<string, ServerIdentification>();
        }

        public Server(string server_id, string partition_id, bool isMasterReplica, string ip, int minDelay, int maxDelay)
        {
            Server_id = server_id;
            Partition_id = partition_id;
            IsMasterReplica = isMasterReplica;
            Ip = IPAddress.Parse(ip);
            Storage = new Dictionary<string, Resource>();
            SystemNodes = new Dictionary<string, ServerIdentification>();
            MinDelay = minDelay;
            MaxDelay = maxDelay;
        }

        internal int AddObject(string id, Resource value)
        {
            Monitor.Enter(Storage);
            if (Storage.ContainsKey(id)) //update resource
            {
                Resource res = Storage[id];
                Monitor.Exit(Storage);

                Monitor.Enter(res);

                if (res.Locked) //wait
                {
                    Console.WriteLine("Object {0} is locked, waiting for unlock ...", res.ObjectId);

                    Monitor.Exit(res);
                    //leave the lock and wait for someone to unflag
                    while (res.Locked)
                    {
                        Monitor.Wait(res);
                    }
                }

                Monitor.Enter(Storage);

                Storage[res.ObjectId] = value;

                Monitor.Exit(Storage);
            }
            else //add new resource to the dictionary
            {
                Storage.Add(id, value);

                //just in case someone is passing a locked resource
                Resource res = Storage[id];

                Monitor.Exit(Storage);

                Monitor.Enter(res);
                res.Locked = false;
                Monitor.Exit(res);
            }

            return 0;
        }

        
        internal string RetrieveObject(string id)
        {
            Monitor.Enter(Storage);
            if (Storage.ContainsKey(id))
            {
                Resource res = Storage[id];
                Monitor.Exit(Storage);

                Monitor.Enter(res);

                if (!res.Locked)
                    return res.Value;
                else //if the resource is locked I wait for a pulse on this resource from the Unlock function 
                {
                    Monitor.Exit(res);
                    //leave the lock and wait for someone to unflag
                    while (res.Locked)
                    {
                        Monitor.Wait(res);
                    }

                    Monitor.Enter(res);

                    return res.Value;
                }
            }
            return "N/A"; //no such resource on this server
        }

        internal bool LockObject(string id)
        {
            Monitor.Enter(Storage);
            if (Storage.ContainsKey(id))
            {
                Resource res = Storage[id];
                Monitor.Exit(Storage);

                Monitor.Enter(res);
                if (!res.Locked)
                {
                    Console.WriteLine("Thread {0} just got the permission for locking resource {1}",
                    Thread.CurrentThread.Name, id);

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
                Monitor.Exit(Storage);
            }

            return false;
        }

        internal bool UnlockObject(string id)
        {
            Monitor.Enter(Storage);
            if (Storage.ContainsKey(id))
            {
                Resource res = Storage[id];
                Monitor.Exit(Storage);

                Monitor.Enter(res);
                if(res.Locked)
                    Storage[id].Locked = false;

                Monitor.Exit(res);

                //awake the processes sleeping on this object
                Monitor.Pulse(res);
                
                return true;
            } else
            {
                Monitor.Enter(Storage);
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
        internal bool UpdateSpecialPermission(Resource resource)
        {
            Monitor.Enter(Storage[resource.ObjectId]);
            Storage[resource.ObjectId] = resource;
            Storage[resource.ObjectId].Locked = false;
            Monitor.Exit(Storage[resource.ObjectId]);

            return true;
        }

    }

    

    public class Program
    { 

        public static void Main(string[] args)
        {
            Server local = new Server("1", "A", true, "127.0.0.1",0,0);
            Server anotherOne = new Server("2", "A", false, "127.0.0.1",0,0);

            List<Server> servers = new List<Server>
            {
                { local },
                { anotherOne }
            };

            //knowledge of all other nodes. This should be initialized by the Puppet Master in the future
            foreach(Server s in servers)
            {
                foreach (Server s2 in servers)
                {
                    //serverPool contains an entry for every other node in the system
                    //with its complete identity: ServerIdentification
                    if(s2.Server_id != s.Server_id)
                    {
                        s.SystemNodes.Add(s2.Server_id, new ServerIdentification(s2.Server_id, s2.Partition_id, s2.Ip));
                    }
                    
                }
            }

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services =
            {
                ServerStorageServices.BindService(new ServerClientService(local)),
                ServerCoordinationServices.BindService(new ServerServerService(local))
            },

                Ports = { new ServerPort("127.0.0.1", 1000 + int.Parse(local.Partition_id), ServerCredentials.Insecure) }
            };

            server.Start();

            Console.WriteLine("Hello World!");
        }
    }
}
