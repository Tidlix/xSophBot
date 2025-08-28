using System.Globalization;

namespace TwitchSharp.Entitys
{
    public class TwitchBadge
    {
        public string SetID { get; set; }
        public string ID { get; set; }
        public string Info { get; set; }
        public TwitchBadge(string setID, string id, string info)
        {
            SetID = setID;
            ID = id;
            Info = info;
        }
    }
}