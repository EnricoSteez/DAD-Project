using System;
using System.Net;

namespace Server
{

    //this is the mask with all the INFORMATIONS of a Server that could be passed
    //so that we can avoid passing al the data such as Dictionaries etc
    //It's just a representation to recognize each Server's public infos
    public class ServerIdentification
    {
        public string Id;
        public string Partition;
        public IPAddress Ip;

        public ServerIdentification(string id, string partition, IPAddress ip)
        {
            Id = id;
            Partition = partition;
            Ip = ip;
        }
    }
}
