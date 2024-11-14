using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace DataLib
{
    [DataContract]
    public class Message
    {

        public Message(string username, string text, string room, string toUser, string filePath = null)  { 
            
            Username = username;
            Text = text;
            Room = room;
            Time = DateTime.Now;
            ToUser = toUser;
            FilePath = filePath;
        }

        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public string Text { get; set; }

        [DataMember]
        public string Room { get; set; }

        [DataMember]
        public string ToUser { get; set; }

        [DataMember]
        public DateTime Time { get; set; }

        [DataMember]
        public string FilePath { get; set; }

    }
}
