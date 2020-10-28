using System;
namespace Server
{
    public class Resource
    {
        public string ObjectId { get; }
        public string Value { get; }
        public bool Locked { get; set; }

        public Resource(string id, string value)
        {
            ObjectId = id;
            Value = value;
            Locked = false;
        }

    }
}
