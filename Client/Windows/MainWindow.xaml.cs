using HWM.Tools.Firebase.WPF.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HWM.Tools.Firebase.WPF.Windows
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }

        public MediaElement? BackgroundAnimation => ((VisualBrush)Resources["Background.Animated"]).Visual as MediaElement;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            // Minimize if launched from shortcut
            if (GlobalData.WasLaunchedFromShortcut)
                WindowState = WindowState.Minimized;
            else if (BackgroundAnimation != null)
            {
                BackgroundAnimation.Source      = new Uri(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Layout", "MainWindow", "AnimatedBackground.wmv"));
                BackgroundAnimation.Loaded     += BackgroundAnimation_Loaded;
                BackgroundAnimation.MediaEnded += BackgroundAnimation_MediaEnded;
            }
        }

        #region Background Animation Handlers

        private void BackgroundAnimation_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (BackgroundAnimation != null)
                BackgroundAnimation.Position = TimeSpan.FromSeconds(0);
        }

        private void BackgroundAnimation_Loaded(object sender, RoutedEventArgs e)
            => BackgroundAnimation?.Play();

        private void Window_LostFocus(object sender, RoutedEventArgs e)
            => BackgroundAnimation?.Pause();

        private void Window_GotFocus(object sender, RoutedEventArgs e)
            => BackgroundAnimation?.Play();

        #endregion

        #region Window Event Handlers

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void ExitButton_Click(object sender, RoutedEventArgs e)
            => Close();

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                DragMove();
        }

        #endregion
    }
}
