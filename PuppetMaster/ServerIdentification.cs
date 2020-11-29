using System;
using System.Collections.Generic;
using System.Net;

namespace Server
{

    //this is a simple and light mask with all the INFORMATIONS of a Server that could be passed
    //so that we can avoid passing al the data such as Dictionaries etc
    //It's just a representation to recognize each Server's public infos

    public class ServerIdentification
    {
        public string Id;
        public List<string> Partitions;
        public string Ip;


        public ServerIdentification(String serverId, string Ip)
        {
            this.Id = serverId;
            this.Ip = Ip;
        }

    }
}
