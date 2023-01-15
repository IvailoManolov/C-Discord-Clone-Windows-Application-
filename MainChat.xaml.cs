using Client.MVVM.Mediator;
using Client.MVVM.ViewModel;
using Client.Net.IO;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainChat.xaml
    /// </summary>
    public partial class MainChat : Window
    {
        private bool micMuted = true;
        private bool screenCapturing = false;
        private Storyboard micAnimationStory;
        private Storyboard cameraAnimationStory;
        private Storyboard screenAnimationStory;
        private const int LENGTH_ANIMATION_DURATION = 1000; //1.0 Second in milliseconds
        private bool screenSharing = false;

        private Task screenCaptureTask;

        private CancellationTokenSource tokenSource;

        private CancellationToken cancelToken;

        public MainChat()
        {
            InitializeComponent();

            Application.Current.MainWindow = this;

            InformationCenter.ShowGridScreen += HandleClientScreenShareAppear;

            InformationCenter.RemoveGridScreen += HandleClientScreenShareDisappear;
        }

        private void HandleClientScreenShareAppear()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ScreenShareAppearAnimation(ScreenShare);
            }));
        }

        private void HandleClientScreenShareDisappear()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ScreenShareDisAppearAnimation(ScreenShare);
            }));
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();

        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void WindowState_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                if (Application.Current.MainWindow.WindowState != WindowState.Maximized)
                {
                    Application.Current.MainWindow.WindowState = WindowState.Maximized;
                }

                else
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void exitApp(object sender, RoutedEventArgs e)
        {

            Application.Current.Shutdown();
        }

        private void HandleMicMute(object sender, RoutedEventArgs e)
        {
            if (micMuted)
                micMuted = false;

            else
                micMuted = true;

            InformationCenter.MicrophoneMute = micMuted;

            if (!micMuted)
            {
                BlinkingMic(microphone, LENGTH_ANIMATION_DURATION, 10000, true);
            }
            else
            {
                BlinkingMic(microphone, LENGTH_ANIMATION_DURATION, 10000, false);
            }
        }

        public void BlinkingMic(System.Windows.Controls.Image image, int length, double repetition, bool shouldPlay)
        {
            if (shouldPlay)
            {
                DoubleAnimation sizeAnimation = new DoubleAnimation()
                {
                    From = 1.0,
                    To = 0.2,
                    Duration = new Duration(TimeSpan.FromMilliseconds(length)),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                Storyboard storyBoard = new Storyboard();
                storyBoard.Children.Add(sizeAnimation);
                Storyboard.SetTarget(sizeAnimation, image);
                Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath("Opacity"));
                micAnimationStory = storyBoard;
                micAnimationStory.Begin();
            }

            else
                micAnimationStory.Stop();

        }

        public void CameraActiveBlinkingAnimation(System.Windows.Controls.Image image, int length, bool shouldPlay)
        {
            if (shouldPlay)
            {
                DoubleAnimation sizeAnimation = new DoubleAnimation()
                {
                    From = 1.0,
                    To = 0.2,
                    Duration = new Duration(TimeSpan.FromMilliseconds(length)),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                Storyboard storyBoard = new Storyboard();
                storyBoard.Children.Add(sizeAnimation);
                Storyboard.SetTarget(sizeAnimation, image);
                Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath("Opacity"));
                cameraAnimationStory = storyBoard;
                cameraAnimationStory.Begin();
            }

            else
                cameraAnimationStory.Stop();
        }

        private void HandleScreenCapturing(object sender, RoutedEventArgs e)
        {
            if (screenCapturing)
            {
                screenCapturing = false;
                CameraActiveBlinkingAnimation(camera, LENGTH_ANIMATION_DURATION, false);
            }

            else
            {
                CameraActiveBlinkingAnimation(camera, LENGTH_ANIMATION_DURATION, true);
                screenCapturing = true;
            }

            InformationCenter.ScreenCapturing = screenCapturing;

            if (screenSharing)
            {
                ScreenShareDisAppearAnimation(ScreenShare);

                screenSharing = false;
            }
            else
            {

                ScreenShareAppearAnimation(ScreenShare);
                screenSharing = true;
            }
        }

        private void ScreenShareAppearAnimation(DependencyObject ob)
        {
            DoubleAnimation sizeAnimation = new DoubleAnimation()
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(LENGTH_ANIMATION_DURATION)),
                AutoReverse = false,
                RepeatBehavior = new RepeatBehavior(1)
            };

            Storyboard storyBoard = new Storyboard();
            storyBoard.Children.Add(sizeAnimation);
            Storyboard.SetTarget(sizeAnimation, ob);
            Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath("Opacity"));
            screenAnimationStory = storyBoard;
            screenAnimationStory.Begin();
        }
        private void ScreenShareDisAppearAnimation(DependencyObject ob)
        {

            DoubleAnimation sizeAnimation = new DoubleAnimation()
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(LENGTH_ANIMATION_DURATION)),
                AutoReverse = false,
                RepeatBehavior = new RepeatBehavior(1)
            };

            Storyboard storyBoard = new Storyboard();
            storyBoard.Children.Add(sizeAnimation);
            Storyboard.SetTarget(sizeAnimation, ob);
            Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath("Opacity"));
            screenAnimationStory = storyBoard;
            screenAnimationStory.Begin();
        }
    }
}
