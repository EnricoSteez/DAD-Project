using System;
namespace Server
{
    public class Resource
    {
        public string ObjectId { get; }
        public string Value { get; set; }
        public bool Locked { get; set; }
        public int Version { get; set; }

        public Resource(string objectId, string value)
        {
            ObjectId = objectId;
            Value = value;
            Locked = false;
            Version = 1;
        }

        public Resource(string objectId, string value, int version)
        {
            ObjectId = objectId;
            Value = value;
            Locked = false;
            Version = version;
        }
    }
}
