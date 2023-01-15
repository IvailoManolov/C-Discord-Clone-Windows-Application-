using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Client.Net.IO
{
    public class PacketReader : BinaryReader
    {
        public NetworkStream ns;

        public PacketReader(NetworkStream ns) : base(ns)
        {
            this.ns = ns;
        }
        public string ReadMessage()
        {
            byte[] msgBuffer;

            var length = ReadInt32();

            msgBuffer = new byte[length];

            ns.Read(msgBuffer, 0, length);

            var msg = Encoding.ASCII.GetString(msgBuffer);

            return msg;
        }
        public byte[] ReadAudioMessage()
        {

            byte[] msgBuffer;

            var length = ReadInt32();

            msgBuffer = new byte[length];

            var a = ns.Read(msgBuffer, 0, length);

            return msgBuffer;
        }

        public byte[] ReadScreenPicture()
        {
            byte[] msgBuffer;

            var length = ReadInt32();

            msgBuffer = new byte[Math.Abs(length)];

            var dataRead = ns.Read(msgBuffer, 0, Math.Abs(length));

            byte[] copiedBuffer = new byte[dataRead];

            Array.Copy(msgBuffer, 0, copiedBuffer, 0, dataRead);

            return copiedBuffer;
        }
    }
}
