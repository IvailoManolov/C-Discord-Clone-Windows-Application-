using Client.Net.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class Program
    {
        private const int PORT = 6789;
        static TcpListener listener;
        static TcpListener listenerAudio;
        static TcpListener screenShare;

        private static object locker = new object();

        static List<Client> users;

        static List<AudioClient> audioUsers;

        static List<ScreenShareClient> screenSharingUsers;


        public static async Task Main(string[] args)
        {
            users = new List<Client>();

            audioUsers = new List<AudioClient>();

            screenSharingUsers = new List<ScreenShareClient>();

            //Listening for broadcast message , UI changes.
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), PORT);

            //Listening for broadcast audio messages.
            listenerAudio = new TcpListener(IPAddress.Parse("127.0.0.1"), PORT - 1);

            //Listening for broadcast screen sharing
            screenShare = new TcpListener(IPAddress.Parse("127.0.0.1"), PORT - 2);

            listener.Start();
            listenerAudio.Start();
            screenShare.Start();

            try
            {
                _ = Task.Run(() =>
                  {
                      _ = VerbalCommunicationSocket();
                  });

                _ = Task.Run(() =>
               {
                   _ = ScreenCommunicationManager();
               });

                while (true)
                {
                    var entry = listener.AcceptTcpClientAsync();

                    var client = new Client(await entry);

                    var clientEndpoint = client.ClientSocket.Client.RemoteEndPoint.ToString();

                    client.dictHolder[client.ClientSocket] = client.ClientID;

                    if (client != null && !users.Contains(client))
                        users.Add(client);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async Task VerbalCommunicationSocket()
        {
            while (true)
            {
                var audioEntry = listenerAudio.AcceptTcpClientAsync();

                var audioClient = new AudioClient(await audioEntry);

                if (audioClient != null && !audioUsers.Contains(audioClient))
                    audioUsers.Add(audioClient);
            }
        }
        private static async Task ScreenCommunicationManager()
        {
            while (true)
            {
                var screenEntry = screenShare.AcceptTcpClientAsync();

                var screenSharerClient = new ScreenShareClient(await screenEntry);

                if (screenSharerClient != null && !screenSharingUsers.Contains(screenSharerClient))
                {
                    lock (screenSharingUsers)
                    {
                        screenSharingUsers.Add(screenSharerClient);
                    }
                }
            }
        }

        public static void BroadcastConnection()
        {
            foreach (var user in users)
            {
                foreach (var usr in users)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(2);
                    broadcastPacket.WriteString(usr.Name);
                    broadcastPacket.WriteString(usr.ClientID.ToString());
                    user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                }
            }
        }

        public static void BroadcastMutedState(string currentColor, Guid client)
        {
            foreach (var user in users)
            {
                var broadcastPacket = new PacketBuilder();
                broadcastPacket.WriteOpCode(3);
                broadcastPacket.WriteString(currentColor);
                broadcastPacket.WriteString(client.ToString());
                user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            }
        }

        public static void BroadcastDisconnectedUser(string uid)
        {
            var disconnectedUser =
                users.Where(x => x.ClientID.ToString() == uid).FirstOrDefault();

            Console.WriteLine($"[{DateTime.Now}][User Disconnect] User {disconnectedUser.Name} has disconnected");

            users.Remove(disconnectedUser);

            foreach (var user in users)
            {
                var broadCastPacket = new PacketBuilder();

                broadCastPacket.WriteOpCode(10);
                broadCastPacket.WriteString(uid);
                user.ClientSocket.Client.Send(broadCastPacket.GetPacketBytes());
            }
        }

        public static void BroadcastAudio(byte[] audioBuffer)
        {
            foreach (var audioUser in audioUsers)
            {
                var broadcastAudioPacket = new PacketBuilder();
                broadcastAudioPacket.WriteAudioMessage(audioBuffer, 0, audioBuffer.Length);
                audioUser.AudioClientSocket.Client.Send(broadcastAudioPacket.GetPacketBytes());
            }
        }

        public static void BroadcastMessage(Guid senderID, string messageToSend, string userName)
        {
            foreach (var user in users)
            {
                if (user.ClientID != senderID)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(30);
                    broadcastPacket.WriteString(userName);
                    broadcastPacket.WriteString(messageToSend);
                    user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                }
            }
        }
        public static void BroadcastScreenStatusMessage(Guid senderID, string messageToSend)
        {
            foreach (var user in users)
            {
                if (user.ClientID != senderID)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(50);
                    broadcastPacket.WriteString(messageToSend);
                    user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                }
            }
        }
        public static void BroadcastScreenImage(byte[] buffer)
        {
            lock (screenSharingUsers)
            {
                foreach (var user in screenSharingUsers)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteScreenImageMessage(buffer);
                    user.ScreenClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                }
            }
        }
    }
}
