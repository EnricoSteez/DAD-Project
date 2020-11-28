using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;

namespace Server
{

    public class Server
    {
        public Dictionary<string, Partition> Storage { get; }
        public Dictionary<string, ServerIdentification> SystemNodes { get; set; }
        public string Server_id { get; }
        public  IPAddress Ip { get; }
        public int MinDelay;
        public int MaxDelay;
        private List<string> _isMasterOf;


        public Server() //dummy implementation for debugging with just 1 server at localhost
        {
            Server_id = "1";
            Ip = IPAddress.Parse("127.0.0.1");
            //empty storage at startup
            Storage = new Dictionary<string, Partition>();
            //empty dictionary at startup
            SystemNodes = new Dictionary<string, ServerIdentification>();
            _isMasterOf = new List<string>();
        }

        public Server(string server_id, string ip, int minDelay, int maxDelay)
        {
            Server_id = server_id;
            Ip = IPAddress.Parse(ip);
            Storage = new Dictionary<string, Partition>();
            SystemNodes = new Dictionary<string, ServerIdentification>();
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
                if (p.Elements.ContainsKey(newValue.ObjectId))
                {
                    Resource res = p.Elements[newValue.ObjectId];
                    Monitor.Exit(p);

                    //lock resource
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

                    Monitor.Exit(res);
                    //here I can just lock the partition because Partitions don't change
                    //no need to lock the entire storage of a server

                    Monitor.Enter(p);

                    p.Elements[newValue.ObjectId] = newValue;
                    p.Elements[newValue.ObjectId].Version++;

                    Monitor.Exit(p);
                }
                else //add new resource to the Elements of the correct Partition, with version=1
                {
                    newValue.Version = 1;
                    p.Elements.Add(newValue.ObjectId, newValue);

                    //just in case someone is passing a locked resource
                    Resource res = Storage[partitionId].Elements[newValue.ObjectId];

                    Monitor.Exit(p);

                    Monitor.Enter(res);
                    res.Locked = false;
                    Monitor.Exit(res);

                }

                return 0;
            }
            else
            {
                Monitor.Exit(Storage);
            }
            
            return -1;
        }

        
        internal string RetrieveObject(string id, string partitionId)
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
                        return res.Value;
                    else //if the resource is locked I wait for a pulse on this resource from the Unlock function 
                    {
                        Monitor.Exit(res);
                        //leave the lock and wait for someone to unflag
                        while (res.Locked)
                        {
                            Monitor.Wait(res);
                        }

                        return res.Value;
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
            return "N/A"; //no such resource on this server
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
                        Console.WriteLine("Thread {0} just got the permission for locking resource {1} in Partition {2}",
                        Thread.CurrentThread.Name, id, partitionId);

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
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            
            string id = args[0];
            string url = args[1];
            int.TryParse(args[2], out int minDelay);
            int.TryParse(args[3], out int maxDelay);

            /*
             * this is working only if PCS passes as arguments only the actually stored partitions
             * and not all the partitions of the system
             */

            List<string> partitionIds = new List<string>();
            List<string> masterIds = new List<string>();

            for (int i=4 ; i<args.Length; i+=2)
            {
                partitionIds.Add(args[i]);
                masterIds.Add(args[i + 1]);
            }
            
            Server init = new Server(id,url,minDelay,maxDelay);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            for(int i = 0; i < partitionIds.ToArray().Length ; i++)
            {
                init.AddPartition(new Partition(partitionIds[i],masterIds[i]));
            }
            

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services =
                {
                    ServerStorageServices.BindService(new ServerClientService(init)),
                    ServerCoordinationServices.BindService(new ServerServerService(init))
                },

                Ports = { new ServerPort("127.0.0.1", 1000 + int.Parse(init.Server_id), ServerCredentials.Insecure) }
            };

            try
            {
                server.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadLine();


        }
    }
}
