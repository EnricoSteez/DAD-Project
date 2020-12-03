using System;
using System.Collections.Generic;

namespace Server
{
    public class Partition
    {
        public string Id { get; set; }
        public Dictionary<string, Resource> Elements { get; }
        public string MasterId { get; set; }

        public Partition(string id, string masterId)
        {
            Id = id;
            Elements = new Dictionary<string, Resource>();
            MasterId = masterId;
        }


    }
}
