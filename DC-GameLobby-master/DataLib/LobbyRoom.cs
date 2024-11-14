using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataLib
{
    public class LobbyRoom
    {
        public string name { get; set; }

        public List<Message> messages { get; set; } = new List<Message>();

        public List<string> Users { get; set; } = new List<string>();

        public int msgCount { get; set; } = 0;

        public int lastMsgId { get; set; } = 0;

    }
}
