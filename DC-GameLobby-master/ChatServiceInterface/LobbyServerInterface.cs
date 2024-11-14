using DataLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ChatServiceInterface
{
    [ServiceContract]
    public interface LobbyServerInterface
    {
        [OperationContract]
        bool Connect(string username);

        [OperationContract]
        void Disconnect(string username);

        [OperationContract]
        bool CreateRoom(string name);

        [OperationContract]
        bool JoinLobbyRoom(string username, string roomId);

        [OperationContract]
        void LeaveLobbyRoom(string username, string roomId);

        [OperationContract]
        void SendMessage(Message message);

        [OperationContract]
        List<LobbyRoom> GetRooms(string username);

        [OperationContract]
        List<string> GetUsers(string roomId);

        [OperationContract]
        List<Message> GetMessages(string roomId, int msgCount, int lastMsg, string username);

        [OperationContract]
        void ShareFile(string roomId, string username, byte[] fileData, string fileName, string toUser);

    }
}
