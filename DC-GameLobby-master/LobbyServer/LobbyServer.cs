using ChatServiceInterface;
using DataLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace LobbyServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal class LobbyServer : LobbyServerInterface
    {

        private static readonly Dictionary<string, LobbyRoom> _chatRooms = new Dictionary<string, LobbyRoom>();
        private static readonly List<string> _users = new List<string>();


        public bool Connect(string username)
        {
            if (username != null || username != "")
            {
                if (!_users.Contains(username))
                {
                    _users.Add(username);
                    Console.WriteLine("User connected: " + username);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public void Disconnect(string username)
        {
            if (_users.Contains(username))
            {
                _users.Remove(username);
                Console.WriteLine("User disconnected: " + username);
            }
        }

        public bool CreateRoom(string name)
        {
            if (name != null || name != "")
            {
                if (!_chatRooms.ContainsKey(name))
                {
                    _chatRooms.Add(name, new LobbyRoom { name = name });
                    Console.WriteLine("Room created: " + name);
                    return true;
                }
                else
                { 
                    return false;
                }
            }
            else { return false; }
        }
        public bool JoinLobbyRoom(string username, string roomId)
        {
            LobbyRoom room = _chatRooms[roomId];
            if (room != null)
            {
                room.Users.Add(username);
                Console.WriteLine("User joined room: " + username + " " + roomId);
                Message message = new Message("Server", username + " joined the room", roomId, "All");
                SendMessage(message);
                return true;
            }
            
            return false;
        }

        public void LeaveLobbyRoom(string username, string roomId)
        {
            LobbyRoom room = _chatRooms[roomId];
            if (room != null)
            {
                room.Users.Remove(username);
                Console.WriteLine("User left room: " + username + " " + roomId);
                Message message = new Message("Server", username + " left the room", roomId, null);
                SendMessage(message);
            }
        }

        public void ShareFile(string roomId, string username, byte[] fileData, string fileName, string toUser = null)
        {
            var room = _chatRooms[roomId];
            if (room != null)
            {
                string fileDirectory = "SharedFiles";
                if (!System.IO.Directory.Exists(fileDirectory))
                {
                    System.IO.Directory.CreateDirectory(fileDirectory);
                }

                string filePath = System.IO.Path.Combine(fileDirectory, fileName);
                System.IO.File.WriteAllBytes(filePath, fileData);

                string fileLocalPath = System.IO.Path.GetFullPath(filePath);

                Message fileMessage = new Message(username, $"File shared: ", roomId, toUser, fileLocalPath);

                SendMessage(fileMessage);
            }
        }

        public void SendMessage(Message message)
        {
            if (message.Room != null)
            {

                
                    Console.WriteLine("Message received: " + message.Text + " To: " + message.ToUser);
                    message.Id = _chatRooms[message.Room].lastMsgId + 1;
                    _chatRooms[message.Room].messages.Add(message);
                    _chatRooms[message.Room].msgCount++;
                    _chatRooms[message.Room].lastMsgId = message.Id;

                    Console.WriteLine("Last MessageID: " + message.Id);

            }
        }

        public List<LobbyRoom> GetRooms(string username)
        {
            return _chatRooms.Values.ToList();
        }

        public List<Message> GetMessages(string roomId, int messageCount, int lastMsgId, string username)
        {
            
            if ( _chatRooms[roomId].lastMsgId != lastMsgId)
            {
                List<Message> messages = new List<Message>();
                for (int i = lastMsgId; i < _chatRooms[roomId].msgCount; i++)
                {
                    Message message = _chatRooms[roomId].messages[i];
                    if(!message.Username.Equals(username) && message.ToUser != null && !message.ToUser.Equals("All") && !message.ToUser.Equals(username))
                    {
                        continue;
                    }
                    else
                    {
                        messages.Add(message);
                    }
                }

                return messages;
            }

            return null;
        }

        public List<string> GetUsers(string roomId)
        {
            LobbyRoom lobbyRoom = _chatRooms[roomId];
            if (lobbyRoom != null)
            {
                return lobbyRoom.Users;
            }
            return null;
        }
    }
}
