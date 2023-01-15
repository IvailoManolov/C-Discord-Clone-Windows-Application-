
using Client.Core;
using Client.MVVM.Mediator;
using Client.MVVM.Model;
using Client.Net.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client.MVVM.ViewModel
{
    class MainChatViewModel : INotifyPropertyChanged
    {
        #region Flags
        const int FLAG_READY_RECEIVE = 1;
        const int FLAG_USER_CONNECTED = 2;
        const int FLAG_USER_MUTED = 3;
        const int FLAG_USER_DISCONNECTED = 10;
        const int FLAG_USER_MESSAGE = 101;
        const int FLAG_USER_MESSAGE_RECEIVE = 30;
        const int FLAG_USER_STARTED_SHARING = 50;
        #endregion

        #region private props/data

        private Task screenCaptureTask;

        private PacketReader packetReader;

        private CancellationTokenSource tokenSource;

        private CancellationToken cancelToken;

        private string mutedMicrophoneImage = "./Icons/MicrophoneRed.png";
        private string unmutedMicrophoneImage = "./Icons/green.png";
        private string microphoneImageSource = "./Icons/MicrophoneRed.png";
        private string userMuted = "Red";
        private string userUnMuted = "Green";
        private string userMutedColor = "Red";
        private string _username;
        private bool screenImageSync = false;

        #endregion

        #region public props/data
        public ObservableCollection<MessageModel> Messages { get; set; }

        public bool IsShareButtonEnabled
        {
            get
            {
                return disableCameraButton;
            }
            set
            {
                disableCameraButton = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsShareButtonEnabled"));
                }

            }
        }

        public event Action connectedEvent;
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<UserModel> Users { get; set; }

        private string message;

        //Commands
        public RelayCommand SendCommand { get; set; }

        public string Message
        {
            get { return message; }

            set
            {
                message = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Message"));
                }
            }
        }
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Username"));
                }
            }
        }
        public string MicrophoneMuted
        {
            get
            {
                return microphoneImageSource;
            }
            set
            {
                microphoneImageSource = value;

                if (microphoneImageSource.Equals(mutedMicrophoneImage))
                {
                    Task.Run(() =>
                    {
                        InformationCenter.RecordingDevice.StopRecording();
                    });

                    userMutedColor = userMuted;
                }

                else
                {
                    Task.Run(() =>
                    {
                        InformationCenter.RecordingDevice.StartRecording();
                    });

                    userMutedColor = userUnMuted;
                }

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("MicrophoneMuted"));
                }
            }
        }

        private BitmapImage currentImage;
        private BitmapImage previousImage;
        public BitmapImage ScreenImage
        {
            get
            {
                return currentImage;
            }
            set
            {
                currentImage = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ScreenImage"));
                }
            }
        }


        private string screenCapturingIcon = "./Icons/CaptureScreen.png";
        private string screenCaptureActiveIcon = "./Icons/CapturingScreen.png";
        private string screenCaptureInActiveIcon = "./Icons/CaptureScreen.png";
        public string ScreenCaptured
        {
            get
            {
                return screenCapturingIcon;
            }
            set
            {
                screenCapturingIcon = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ScreenCaptured"));
                }
            }
        }

        private bool disableCameraButton = true;

        #endregion

        #region Constructor
        public MainChatViewModel()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            InformationCenter.UsernameChosen += UsernameCompleted;

            InformationCenter.MicMuted += OnMicMute;
            InformationCenter.MicMuted += SendMuted_State;

            InformationCenter.ScreenCaptured += OnScreenCapturing;

            SendCommand = new RelayCommand(x =>
            {
                Messages.Add(new MessageModel
                {
                    Username = Username,
                    UsernameColor = "#409aff",
                    ImageSource = "https://i.imgur.com/yMWvLXd.png",
                    Time = DateTime.Now,
                    Message = Message,
                    OwnMessage = true
                });

                var currentMessage = Message;

                SendMessageToServer(currentMessage);

                Message = "";
            });

            connectedEvent += UserConnected;

            var user = new UserModel()
            {
                Username = "pedala",
                ImageSource = null,
                UID = "219379jfa-a-fj3",
                UserMuted = "Red"
            };

            packetReader = InformationCenter.packetReader;

            var audioPacketBuilder = new PacketBuilder();

            InformationCenter.audioPacketBuilder = audioPacketBuilder;

            Messages = new ObservableCollection<MessageModel>();

            Users = new ObservableCollection<UserModel>();

            Users.Add(user);

            Messages.Add(new MessageModel
            {
                Username = "Allison",
                UsernameColor = "#409aff",
                ImageSource = "https://i.imgur.com/yMWvLXd.png",
                Message = "Test",
                Time = DateTime.Now,
                IsNativeOrigin = false,
                FirstMessage = true
            });

            Messages.Add(new MessageModel
            {
                Username = "Allison",
                UsernameColor = "#409aff",
                ImageSource = "https://i.imgur.com/yMWvLXd.png",
                Message = "Test",
                Time = DateTime.Now,
                IsNativeOrigin = false,
                FirstMessage = true
            });

            Messages.Add(new MessageModel
            {
                Username = "PEDAL",
                UsernameColor = "#409aff",
                ImageSource = "https://i.imgur.com/yMWvLXd.png",
                Message = "Test",
                Time = DateTime.Now,
                IsNativeOrigin = false,
                FirstMessage = true,
                OwnMessage = true
            });

            ReadPackets();

            SetupAudio();

            ReadAudioPackets();

            ReadScreenPackets();
        }

        private async void OnScreenCapturing()
        {
            //Send to the server that You will begin sharing.

            if (tokenSource == null)
            {
                tokenSource = new CancellationTokenSource();

                cancelToken = tokenSource.Token;
            }

            var isScreenCapturing = InformationCenter.ScreenCapturing;

            if (isScreenCapturing)
            {
                SendSharingScreenNotificationToServer("TRUE");

                Thread.Sleep(25);

                ScreenCaptured = screenCaptureActiveIcon;

                // Start sending to server.
                screenCaptureTask = Task.Run(() => CaptureScreenState(cancelToken));
            }

            else
            {
                ScreenCaptured = screenCaptureInActiveIcon;
                SendSharingScreenNotificationToServer("FALSE");

                try
                {
                    tokenSource.Cancel();
                    tokenSource = null;
                }

                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                finally
                {
                    await screenCaptureTask;
                }
            }
        }

        private void SendMessageToServer(string messageToSend)
        {
            var messagePacket = InformationCenter.packetBuilder;
            var client = InformationCenter.Client;

            //Send the message to the server.
            messagePacket.WriteOpCode(FLAG_USER_MESSAGE);

            messagePacket.WriteString(messageToSend);

            client.Client.Send(messagePacket.GetPacketBytes());
        }

        private void SendSharingScreenNotificationToServer(string messageToSend)
        {
            var messagePacket = InformationCenter.packetBuilder;
            var client = InformationCenter.Client;

            //Send the message to the server.
            messagePacket.WriteOpCode(FLAG_USER_STARTED_SHARING);

            messagePacket.WriteString(messageToSend);

            client.Client.Send(messagePacket.GetPacketBytes());
        }
        private void SetupAudio()
        {
            InformationCenter.bufferProvider =
                    new BufferedWaveProvider(InformationCenter.RecordingDevice.WaveFormat);

            InformationCenter.RecordingDevice.DataAvailable += Wave_dataAvailable;

            InformationCenter.PlaybackDevice
                .Init(InformationCenter.bufferProvider);

            InformationCenter.PlaybackDevice.DesiredLatency = 100;

            InformationCenter.PlaybackDevice.Play();
        }
        private void Wave_dataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                if (InformationCenter.RecordingDeviceMeter.AudioMeterInformation.MasterPeakValue * 100 > 1)
                {
                    var audioClient = InformationCenter.AudioClient;

                    InformationCenter.audioPacketBuilder.WriteAudioMessage(e.Buffer, 0, e.BytesRecorded);

                    audioClient.Client.Send(InformationCenter.audioPacketBuilder.GetPacketBytes());
                }
            }
            catch (Exception er)
            {
                MessageBox.Show("Failed extracting data from microphone : " + er.Message);
            }
        }
        #endregion

        #region private functions
        private void UsernameCompleted()
        {
            Username = InformationCenter.Username;
        }
        private void OnMicMute()
        {
            var isMicMuted = InformationCenter.MicrophoneMute;


            if (!isMicMuted)
            {
                MicrophoneMuted = unmutedMicrophoneImage;
            }

            else
            {
                MicrophoneMuted = mutedMicrophoneImage;
            }
        }
        #endregion

        private void CaptureScreenState(CancellationToken token)
        {
            try
            {
                CancellationToken ct = token;

                while (!ct.IsCancellationRequested)
                {
                    Bitmap bitmap_Screen = new Bitmap(1920, 1080);

                    Graphics g = Graphics.FromImage(bitmap_Screen);

                    g.CopyFromScreen(0, 0, 0, 0, bitmap_Screen.Size);

                    //Send to server.
                    SendBitmapToServer(bitmap_Screen);

                    Thread.Sleep(50);
                }
            }
            catch (Exception e)
            {

            }
        }

        private void SendBitmapToServer(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                SendScreenState(memory);
            }
        }
        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            BitmapImage bitmapimage = null;
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        #region NetworkCalls
        // ReadPackets is the function that is running on a different thread
        /*
         * This Communicates with the server and is the thread responsible for the events that will later update the ui.
         * 
         */
        private void ReadPackets()
        {
            Task.Run(() =>
            {
                var connectPacket = new PacketBuilder();

                InformationCenter.packetBuilder = connectPacket;

                var client = InformationCenter.Client;

                while (true)
                {
                    connectPacket.WriteOpCode(FLAG_READY_RECEIVE);

                    connectPacket.WriteString("");

                    client.Client.Send(connectPacket.GetPacketBytes());

                    var opCode = packetReader.ReadByte();

                    switch (opCode)
                    {
                        case FLAG_USER_CONNECTED:
                            connectedEvent?.Invoke();
                            break;

                        case FLAG_USER_MESSAGE_RECEIVE:
                            UpdateMessages();
                            break;

                        case FLAG_USER_MUTED:
                            UpdateMutedUsers();
                            break;

                        case FLAG_USER_DISCONNECTED:
                            UserDisconnected();
                            break;

                        case FLAG_USER_STARTED_SHARING:
                            UpdateScreenShare();
                            break;
                    }
                }
            });
        }
        private void UpdateScreenShare()
        {
            //Read the packet.
            var screenShareAllowed = packetReader.ReadMessage();

            if (screenShareAllowed.Equals("TRUE"))
            {
                IsShareButtonEnabled = false;

                // Show Grid for screen
                InformationCenter.ShowGrid(true);
            }

            else
            {
                InformationCenter.ShowGrid(false);

                IsShareButtonEnabled = true;
            }
        }
        private void ReadScreenPackets()
        {
            Task.Run(() =>
            {
                var screenReader = InformationCenter.screenPacketReader;

                var client = InformationCenter.ShareScreenClient;

                while (true)
                {
                    try
                    {
                        //Screen image received.
                        var screenPictureData = screenReader.ReadScreenPicture();

                        Bitmap bitmap;
                        using (var ms = new MemoryStream(screenPictureData))
                        {
                            if(ms.CanSeek)
                            {
                            bitmap = new Bitmap(ms);

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    //change the frame on the screen.

                                    ScreenImage = BitmapToImageSource(bitmap);
                                    previousImage = ScreenImage;
                                }
                                catch(Exception e)
                                {
                                    ScreenImage = previousImage;
                                    Debug.WriteLine("Catched more data to handle");
                                }
                            });

                            }
                        }

                        Thread.Sleep(25);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Screen Message dropped " + ex.Message);
                    }
                }
            });
        }
        private void ReadAudioPackets()
        {
            Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                var audioClient = InformationCenter.AudioClient;

                var audioPacketReader = InformationCenter.audioPacketReader;

                while (true)
                {
                    try
                    {
                        //Audio message received so just play it.
                        var audioMessage = audioPacketReader.ReadAudioMessage();

                        if (InformationCenter.bufferProvider.BufferedDuration <= TimeSpan.FromMilliseconds(100))
                            InformationCenter.bufferProvider.AddSamples(audioMessage, 0, audioMessage.Length);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Problem Reading Audio packets : " + e.Message);
                    }
                }
            });
        }
        private void UpdateMutedUsers()
        {
            var colorToUpdate = packetReader.ReadMessage();

            var UID = packetReader.ReadMessage();

            foreach (var u in Users)
            {
                if (u.UID == UID)
                {
                    u.UserMuted = colorToUpdate;
                }
            }
        }
        private void SendMuted_State()
        {
            var connectPacket = InformationCenter.packetBuilder;
            var client = InformationCenter.Client;

            //Send that I am muted.
            connectPacket.WriteOpCode(FLAG_USER_MUTED);

            connectPacket.WriteString(userMutedColor);

            client.Client.Send(connectPacket.GetPacketBytes());
        }
        private void UserConnected()
        {
            var user = new UserModel()
            {
                Username = packetReader.ReadMessage(),
                ImageSource = null,
                UID = packetReader.ReadMessage(),
                UserMuted = "Red"
            };


            if (!Users.Any(x => x.UID == user.UID))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Add(user);
                });
            }
        }
        private void UserDisconnected()
        {
            var disconnectedUserId = packetReader.ReadMessage();

            if (Users.Any(x => x.UID == disconnectedUserId))
            {
                var userToRemove = Users.Where(x => x.UID == disconnectedUserId).FirstOrDefault();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Remove(userToRemove);
                });
            }
        }
        private void UpdateMessages()
        {
            var userName = packetReader.ReadMessage();
            var message = packetReader.ReadMessage();

            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Messages.Add(new MessageModel
                {
                    Username = userName,
                    UsernameColor = "#409aff",
                    ImageSource = "https://i.imgur.com/yMWvLXd.png",
                    Message = message,
                    Time = DateTime.Now
                });
            }));
        }
        private void SendScreenState(MemoryStream testmap)
        {
            var screenPacketBuilder = new PacketBuilder();

            InformationCenter.screenPacketBuilder = screenPacketBuilder;

            var bitmap = testmap.ToArray();

            InformationCenter.screenPacketBuilder.WriteScreenImageMessage(bitmap);

            InformationCenter.ShareScreenClient.Client.Send(InformationCenter.screenPacketBuilder.GetPacketImageBytes());
        }
        #endregion
    }
}
