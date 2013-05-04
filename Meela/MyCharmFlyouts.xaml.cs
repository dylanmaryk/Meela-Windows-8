using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Meela
{
    public sealed partial class MyCharmFlyouts : UserControl
    {
        public MyCharmFlyouts()
        {
            this.InitializeComponent();

            SettingsPane.GetForCurrentView().CommandsRequested += CommandsRequested;
        }

        private void CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            args.Request.ApplicationCommands.Add(new SettingsCommand("s", "About", (p) =>
            {
                aboutSettings.IsOpen = true;
            }));
        }

        private async void mailHyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:info@codecanopy.com"));
        }

        private async void websiteHyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("http://www.codecanopy.com"));
        }
    }
}
