using Client.MVVM.Model;
using Client.Net.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client.MVVM.Mediator
{
    public static class InformationCenter
    {
        private static string _username;
        public static string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
                if (UsernameChosen != null)
                {
                    UsernameChosen.Invoke();
                }
            }
        }
        public static event Action UsernameChosen;
        public static event Action MicMuted;
        public static event Action ScreenCaptured;
        public static event Action ShowGridScreen;
        public static event Action RemoveGridScreen;
        private static bool micMuted;
        private static bool screenRecording;
        public static bool MicrophoneMute
        {
            get { return micMuted; }
            set 
            {
                micMuted = value;

                if (MicMuted != null)
                    MicMuted.Invoke();
            }
        }
        public static TcpClient Client { get; internal set; }
        public static TcpClient AudioClient { get; internal set; }
        public static TcpClient ShareScreenClient { get; internal set; }
        public static PacketBuilder packetBuilder { get; set; }
        public static PacketReader packetReader { get; set; }
        public static PacketReader audioPacketReader { get; set; }
        public static PacketReader screenPacketReader { get; set; }
        public static PacketBuilder audioPacketBuilder { get; set; }
        public static PacketBuilder screenPacketBuilder { get; set; }
        public static WaveIn RecordingDevice { get; set; }
        public static MMDevice RecordingDeviceMeter { get; set; }
        public static WaveOutEvent PlaybackDevice { get; set; }
        public static BufferedWaveProvider bufferProvider { get; set; }
        public static RawSourceWaveStream audioStream { get; set; }
        public static bool ScreenCapturing
        {
            get 
            {
                return screenRecording;
            }
            set
            {
                screenRecording = value;

                if (ScreenCaptured != null)
                    ScreenCaptured.Invoke();
            }
        }
        public static void ShowGrid(bool showingGrid)
        {
            if (showingGrid)
                ShowGridScreen?.Invoke();

            else
                RemoveGridScreen?.Invoke();
        }
    }
}
