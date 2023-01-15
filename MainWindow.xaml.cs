using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Client.MVVM.Mediator;
using Client.Net.IO;
using MaterialDesignThemes.Wpf;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region public props/fields
        public bool IsDarkTheme { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public int WorkerState
        {
            get
            {
                return _workerState;
            }
            set
            {
                _workerState = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("WorkerState"));
                }
            }
        }
        public string PlaybackDevice_Tooltip
        {
            get
            {
                return icon_Warning_GreenCheck_Playback ?? icon_Warning_Tooltip;
            }
            set
            {
                icon_Warning_GreenCheck_Playback = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("PlaybackDevice_Tooltip"));
                }
            }
        }
        public string RecordingDevice_Tooltip
        {
            get
            {
                return icon_Warning_GreenCheck_Recording ?? icon_Warning_Tooltip;
            }
            set
            {
                icon_Warning_GreenCheck_Recording = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("RecordingDevice_Tooltip"));
                }
            }
        }
        public string PlaybackDevice_Icon
        {
            get
            {
                return iconPathPlayback ?? icon_warning;
            }
            set
            {
                iconPathPlayback = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("PlaybackDevice_Icon"));
                }
            }
        }
        public string RecordingDevice_Icon
        {
            get
            {
                return iconPathRecording ?? icon_warning;
            }
            set
            {
                iconPathRecording = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("RecordingDevice_Icon"));
                }
            }
        }
        #endregion

        #region private props/fields
        private bool playbackIconsTooltipLoaded = false;
        private bool recordingIconsTooltipLoaded = false;

        private const int AUDIO_PORT = 1; // 6789 is the base port but the server listens for audio on 6788
        private const int SHARE_SCREEN_PORT = 2; //6789 is the base port but the server listens for audio on 6787

        private readonly PaletteHelper _palleteHelper;

        private string iconPathPlayback;
        private const string icon_warning = "./Icons/Warning.png";
        private const string icon_greencheck = "./Icons/check.png";
        private string icon_Warning_GreenCheck_Playback;
        private const string icon_Warning_Tooltip = "Device is not set. Go to options to set a device.";
        private const string icon_GreenCheck_Tooltip = "Device is correctly set.";

        private object recordingLocker = new object();

        private string icon_Warning_GreenCheck_Recording;

        private string iconPathRecording;

        private BackgroundWorker _bgWorker;

        private List<WaveInCapabilities> waveinList;

        private int _workerState;

        private bool connectionPerforming;

        private event Action OnLoginSucceeded;

        private TcpClient _client;
        private TcpClient _clientAudio;
        private TcpClient _clientShareScreen;

        private object locker = new object();

        private PacketBuilder packetBuilder;

        private MMDeviceCollection inputDevices;

        #endregion

        public MainWindow()
        {
            waveinList = new List<WaveInCapabilities>();

            _palleteHelper = new PaletteHelper();

            _client = new TcpClient();

            _clientAudio = new TcpClient();

            _clientShareScreen = new TcpClient();

            OnLoginSucceeded += ShowMainChatApp;

            _bgWorker = new BackgroundWorker();

            _bgWorker.WorkerSupportsCancellation = true;

            _bgWorker.DoWork += PerformConnectToServer;

            connectionPerforming = false;

            DataContext = this;

            InitializeComponent();

            LoadAudioDevices();
        }
        private void LoadAudioDevices()
        {
            try
            {
                MMDeviceEnumerator inputDeviceEnumerator = new MMDeviceEnumerator();

                inputDevices = inputDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

                for (int deviceID = 0; deviceID < WaveIn.DeviceCount; deviceID++)
                {
                    var deviceInformation = WaveIn.GetCapabilities(deviceID);

                    RecordingDeviceCombo.Items.Add(deviceInformation.ProductName);

                    waveinList.Add(deviceInformation);
                }

                for (int deviceID = 0; deviceID < WaveOut.DeviceCount; deviceID++)
                {
                    var deviceInformation = WaveOut.GetCapabilities(deviceID);

                    PlaybackDeviceCombo.Items.Add(deviceInformation.ProductName);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Problem loading audio devices from computer.");
                var m = e.Message;
            }
        }
        private void toggleTheme(object sender, RoutedEventArgs e)
        {
            ITheme theme = _palleteHelper.GetTheme();

            if (IsDarkTheme = theme.GetBaseTheme() == BaseTheme.Dark)
            {
                IsDarkTheme = false;
                theme.SetBaseTheme(Theme.Light);
            }

            else
            {
                IsDarkTheme = true;
                theme.SetBaseTheme(Theme.Dark);
            }

            _palleteHelper.SetTheme(theme);
        }
        private void exitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        //Perform login to server chat.
        private async void connectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RecordingDevice_Icon.Equals(icon_warning) || PlaybackDevice_Icon.Equals(icon_warning))
                MessageBox.Show("Set devices from the menu !");

            else
            {

                if (!connectionPerforming)
                {
                    connectionPerforming = true;
                    _bgWorker.RunWorkerAsync();
                }

                try
                {
                    string currentSocket = txtSocket.Text;

                    foreach (var ch in currentSocket)
                    {
                        if (!((ch >= '0' && ch <= '9') || ch.Equals('.') || ch.Equals(':')))
                        {
                            throw new ArgumentException();
                        }
                    }

                    var socketArray = currentSocket.Split(':');

                    int port = 0;

                    string ip = socketArray[0];

                    Int32.TryParse(socketArray[1], out port);

                    if (port == 0)
                    {
                        throw new ArgumentException();
                    }

                    await _client.ConnectAsync(ip, port);

                    await _clientAudio.ConnectAsync(ip, port - 1);

                    await _clientShareScreen.ConnectAsync(ip, port - 2);

                    InformationCenter.Client = _client;

                    InformationCenter.packetReader = new PacketReader(_client.GetStream());

                    InformationCenter.AudioClient = _clientAudio;

                    InformationCenter.audioPacketReader = new PacketReader(_clientAudio.GetStream());

                    InformationCenter.ShareScreenClient = _clientShareScreen;

                    InformationCenter.screenPacketReader = new PacketReader(_clientShareScreen.GetStream());

                    var connectPacket = new PacketBuilder();

                    connectPacket.WriteOpCode(0);

                    connectPacket.WriteString(txtPassword.Text);

                    _client.Client.Send(connectPacket.GetPacketBytes());

                    MessageBox.Show("Success connecting to server...");

                    OnLoginSucceeded.Invoke();
                }
                catch (ArgumentException ex)
                {
                    connectionPerforming = false;

                    MessageBox.Show("Socket is not correct ." + ex.Message);

                    WorkerState = 0;
                }
                catch (Exception ex)
                {
                    connectionPerforming = false;

                    MessageBox.Show("Error connecting to server." + ex.Message);

                    WorkerState = 0;
                }
            }
        }
        private void PerformConnectToServer(object caller, EventArgs e)
        {
            for (int i = 0; i < 101; i++)
            {
                System.Threading.Thread.Sleep(5);
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    WorkerState = i;
                }));
            }
        }
        private void ShowMainChatApp()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MainChat window = new MainChat();

                var chosenUsername = txtPassword.Text;

                InformationCenter.Username = chosenUsername;

                window.Show();

                this.Close();
            }));
        }
        private void PlaybackDevice_Chosen(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PlaybackDeviceCombo.SelectedItem != null)
            {
                playbackIconsTooltipLoaded = true;
                PlaybackDevice_Tooltip = icon_GreenCheck_Tooltip;
                PlaybackDevice_Icon = icon_greencheck;

                //Set up the device used for playing back the received audio. 
                InformationCenter.PlaybackDevice = new WaveOutEvent();

                InformationCenter.PlaybackDevice.DeviceNumber = PlaybackDeviceCombo.SelectedIndex;
            }
            else
            {
                playbackIconsTooltipLoaded = false;
                PlaybackDevice_Tooltip = icon_Warning_Tooltip;
                PlaybackDevice_Icon = icon_warning;

            }
        }
        private void RecordingDevice_Chosen(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (RecordingDeviceCombo.SelectedItem != null)
            {
                recordingIconsTooltipLoaded = true;
                RecordingDevice_Tooltip = icon_GreenCheck_Tooltip;
                RecordingDevice_Icon = icon_greencheck;

                //Set up the device used for recording audio.

                InformationCenter.RecordingDevice = new WaveIn();

                InformationCenter.RecordingDevice.WaveFormat = new WaveFormat(44100, 1);

                InformationCenter.RecordingDevice.DeviceNumber = RecordingDeviceCombo.SelectedIndex;

                foreach (var input in inputDevices)
                {
                    if (input.FriendlyName.ToString().Contains(RecordingDeviceCombo.SelectedItem.ToString()))
                    {
                        InformationCenter.RecordingDeviceMeter = input;
                        break;
                    }
                }
            }
            else
            {
                recordingIconsTooltipLoaded = false;
                RecordingDevice_Tooltip = icon_Warning_Tooltip;
                RecordingDevice_Icon = icon_warning;

            }
        }
    }
}
