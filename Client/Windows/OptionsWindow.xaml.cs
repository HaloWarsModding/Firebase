using HWM.Tools.Firebase.WPF.Core;
using HWM.Tools.Firebase.WPF.Core.Serialization;
using HWM.Tools.Firebase.WPF.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace HWM.Tools.Firebase.WPF.Windows
{
    public partial class OptionsWindow : Window
    {
        public MediaElement? BackgroundAnimation => ((VisualBrush)Resources["Background.Animated"]).Visual as MediaElement;

        public OptionsWindow()
        {
            InitializeComponent();

            if (BackgroundAnimation != null)
            {
                BackgroundAnimation.Source      = new Uri(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Layout", "OptionsWindow", "AnimatedBackground.wmv"));
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

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
            e.Handled = true;
        }

        private void SaveConfigOnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext != null && DataContext is OptionsViewModel vm && vm.IsDirty)
            {
                GlobalData.UserConfig.InstallationType = vm.InstallationTypes[vm.SelectedInstallation];
                GlobalData.UserConfig.TimeoutDelay     = vm.LaunchTimeoutDelay.ToString();
                GlobalData.UserConfig.UserModsFolder   = vm.UserModsFolder;
                GlobalData.UserConfig.Save();
            }
        }
    }
}
