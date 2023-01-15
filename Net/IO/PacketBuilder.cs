using Client.MVVM.Mediator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Net.IO
{
    public class PacketBuilder
    {
        private MemoryStream ms;
        private MemoryStream screenStream;
        private object locker = new object();
        private object imageLocker = new object();

        public PacketBuilder()
        {
            ms = new MemoryStream();
            screenStream = new MemoryStream();
        }

        public void WriteOpCode(byte opCode)
        {
            lock (locker)
            {
                ms.WriteByte(opCode);
            }
        }

        public void WriteString(string msg)
        {
            lock (locker)
            {
                var msgLength = msg.Length;

                ms.Write(BitConverter.GetBytes(msgLength), 0, BitConverter.GetBytes(msgLength).Length);

                ms.Write(Encoding.ASCII.GetBytes(msg), 0, msgLength);
            }
        }

        public byte[] GetPacketBytes()
        {
            lock (locker)
            {
                var result = ms.ToArray();

                Task.Run(() =>
                {
                    Clear(ms);
                });

                return result;
            }
        }
        public byte[] GetPacketImageBytes()
        {
            var result = screenStream.ToArray();

            ClearImage(screenStream);

            return result;
        }

        public void Clear(MemoryStream source)
        {
            lock (locker)
            {
                byte[] buffer = source.GetBuffer();
                Array.Clear(buffer, 0, buffer.Length);
                source.Position = 0;
                source.SetLength(0);
            }
        }

        public void ClearImage(MemoryStream source)
        {
            lock (imageLocker)
            {
                byte[] buffer = source.GetBuffer();
                Array.Clear(buffer, 0, buffer.Length);
                source.Position = 0;
                source.SetLength(0);
            }
        }

        public void WriteAudioMessage(byte[] msg, int startingIndex, int bytesRecorded)
        {
            lock (locker)
            {
                var msgLength = msg.Length;

                ms.Write(BitConverter.GetBytes(msgLength), 0, BitConverter.GetBytes(msg.Length).Length);

                ms.Write(msg, 0, bytesRecorded);
            }
        }

        public void WriteScreenImageMessage(byte[] bitmap)
        {
            lock (imageLocker)
            {
                int imageLength = bitmap.Length;

                screenStream.Write(BitConverter.GetBytes(imageLength), 0, BitConverter.GetBytes(imageLength).Length);

                screenStream.Write(bitmap, 0, imageLength);
            }
        }
    }
}
