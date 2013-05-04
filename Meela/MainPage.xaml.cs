using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Meela
{
    public sealed partial class MainPage : Meela.Common.LayoutAwarePage
    {
        public class Character
        {
            public Character() { Characters = new ObservableCollection<UrlItem>(); }
            public ObservableCollection<UrlItem> Characters { get; private set; }
        }

        public class UrlItem
        {
            public string ShortUrl { get; set; }
            public string LongUrl { get; set; }
        }

        private List<Character> Characters = new List<Character>();

        private List<string> storedDataShortList = new List<string>();
        private List<string> storedDataLongList = new List<string>();

        private Character newCharacter = new Character();
        
        private HttpClient httpClient;
        private HttpClientHandler handler;

        private DataTransferManager dataTransferManager;

        private string urlBoxLatest;
        
        public MainPage()
        {
            this.InitializeComponent();

            handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;

            httpClient = new HttpClient(handler);
            httpClient.MaxResponseContentBufferSize = 256000;
            httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            LoadUrl();

            this.dataTransferManager = DataTransferManager.GetForCurrentView();
            this.dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.ShareTextHandler);

            try
            {
                ShareOperation shareOperation = (ShareOperation)e.Parameter;

                if (shareOperation.Data.Contains(StandardDataFormats.Uri))
                {
                    Uri sharedUri = await shareOperation.Data.GetUriAsync();

                    urlBox.Text = sharedUri.ToString();
                }
                if (shareOperation.Data.Contains(StandardDataFormats.Text))
                {
                    string sharedText = await shareOperation.Data.GetTextAsync();

                    urlBox.Text = sharedText;
                }

                ShortenUrl();

                progressBar.Visibility = Visibility.Collapsed;
                pageTitle.Visibility = Visibility.Collapsed;

                mainGrid.Margin = new Thickness(60, -80, 60, 0);
            }
            catch
            {
                Window.Current.SizeChanged += OnWindowSizeChanged;

                SizeChanged();
            }
        }

        private void OnWindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            SizeChanged();
        }

        private void SizeChanged()
        {
            if (ApplicationView.Value != ApplicationViewState.Snapped)
            {
                copyBtn.Width = (resultBox.Width / 2) + 5;
                openBtn.Width = (resultBox.Width / 2) + 5;
            }
        }

        private async void ShortenUrl()
        {
            if (NetworkInformation.GetInternetConnectionProfile() != null)
            {
                progressBar.Visibility = Visibility.Visible;
                copyBtn.Visibility = Visibility.Collapsed;
                openBtn.Visibility = Visibility.Collapsed;
                resultBox.Visibility = Visibility.Collapsed;

                shortenBtn.IsEnabled = false;
                urlBox.IsEnabled = false;
                customBox.IsEnabled = false;

                HttpResponseMessage response;

                if (customBox.Text == "" || customBox.Text == "Enter the phrase you want to use in your shortened URL...")
                    response = await httpClient.GetAsync("http://mee.la/api.php?url=" + urlBox.Text);
                else
                    response = await httpClient.GetAsync("http://mee.la/api.php?url=" + urlBox.Text + "&custom=" + customBox.Text);

                string xmlString = await response.Content.ReadAsStringAsync();

                if (xmlString == "Error")
                {
                    resultBox.Visibility = Visibility.Collapsed;
                    resultBoxError.Visibility = Visibility.Visible;
                    resultBoxError.Text = "The URL is too short. Please use a full URL.";
                }
                else if (xmlString == "<br/><h2>This name is already taken please choose another one.</h2> ")
                {
                    resultBox.Visibility = Visibility.Collapsed;
                    resultBoxError.Visibility = Visibility.Visible;
                    resultBoxError.Text = "This custom URL is already taken. Please choose another one.";
                }
                else
                {
                    resultBox.Text = xmlString;

                    resultBox.Visibility = Visibility.Visible;
                    resultBoxError.Visibility = Visibility.Collapsed;
                    copyBtn.Visibility = Visibility.Visible;
                    openBtn.Visibility = Visibility.Visible;

                    StoreUrl();
                }

                progressBar.Visibility = Visibility.Collapsed;

                shortenBtn.IsEnabled = true;
                urlBox.IsEnabled = true;
                customBox.IsEnabled = true;

                urlBoxLatest = urlBox.Text;
            }
            else
            {
                resultBoxError.Text = "Your PC isn't connected to the Internet. To use Mee.la, connect to the Internet and then try again.";
            }
        }

        private async void StoreUrl()
        {
            Characters = new List<Character>();
            
            newCharacter.Characters.Add(new UrlItem()
            {
                ShortUrl = resultBox.Text,
                LongUrl = urlBox.Text
            });

            Characters.Add(newCharacter);

            cvsCharacters.Source = Characters;

            gridHistory.SelectedIndex = -1;

            string storedDataShort = resultBox.Text + ",";
            string storedDataLong = urlBox.Text + ",";

            StorageFile storedDataShortFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("storedDataShort.txt", CreationCollisionOption.OpenIfExists);

            await FileIO.AppendTextAsync(storedDataShortFile, storedDataShort);

            StorageFile storedDataLongFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("storedDataLong.txt", CreationCollisionOption.OpenIfExists);

            await FileIO.AppendTextAsync(storedDataLongFile, storedDataLong);
        }

        private async void LoadUrl()
        {
            try
            {
                Characters = new List<Character>();
                
                StorageFile storedDataShortFile = await ApplicationData.Current.LocalFolder.GetFileAsync("storedDataShort.txt");

                String storedDataShortResult = await FileIO.ReadTextAsync(storedDataShortFile);

                storedDataShortResult = storedDataShortResult.TrimEnd(',');

                storedDataShortList = storedDataShortResult.Split(',').ToList();

                StorageFile storedDataLongFile = await ApplicationData.Current.LocalFolder.GetFileAsync("storedDataLong.txt");

                String storedDataLongResult = await FileIO.ReadTextAsync(storedDataLongFile);

                storedDataLongResult = storedDataLongResult.TrimEnd(',');

                storedDataLongList = storedDataLongResult.Split(',').ToList();

                for (int i = 0; i < storedDataShortList.Count(); i++)
                {
                    newCharacter.Characters.Add(new UrlItem()
                    {
                        ShortUrl = storedDataShortList[i],
                        LongUrl = storedDataLongList[i]
                    });

                    Characters.Add(newCharacter);

                    cvsCharacters.Source = Characters;
                }

                gridHistory.SelectedIndex = -1;
            }
            catch { }
        }

        private void gridHistory_Tapped(object sender, TappedRoutedEventArgs e)
        {
            appBar.IsOpen = true;
        }

        private async void deleteHistory_Click(object sender, RoutedEventArgs e)
        {
            StorageFile storedDataShortFile = await ApplicationData.Current.LocalFolder.GetFileAsync("storedDataShort.txt");
            await storedDataShortFile.DeleteAsync();

            StorageFile storedDataLongFile = await ApplicationData.Current.LocalFolder.GetFileAsync("storedDataLong.txt");
            await storedDataLongFile.DeleteAsync();

            Characters = new List<Character>();

            newCharacter = new Character();

            cvsCharacters.Source = Characters;

            CheckHistoryCount();

            appBar.IsOpen = false;
        }

        private void copyBtnBar_Click(object sender, RoutedEventArgs e)
        {
            DataPackage copyText = new DataPackage();
            copyText.SetText(storedDataShortList[gridHistory.SelectedIndex]);

            Clipboard.SetContent(copyText);

            appBar.IsOpen = false;
        }

        private async void openBtnBar_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(storedDataShortList[gridHistory.SelectedIndex]));

            appBar.IsOpen = false;
        }

        private void appBar_Opened(object sender, object e)
        {
            CheckHistoryCount();

            if (gridHistory.SelectedIndex == -1)
            {
                copyBtnBar.Visibility = Visibility.Collapsed;
                openBtnBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                copyBtnBar.Visibility = Visibility.Visible;
                openBtnBar.Visibility = Visibility.Visible;
            }
        }

        private void appBar_Closed(object sender, object e)
        {
            gridHistory.SelectedIndex = -1;
        }

        private void CheckHistoryCount()
        {
            if (Characters.Count == 0)
                deleteHistory.IsEnabled = false;
            else
                deleteHistory.IsEnabled = true;
        }

        private void urlBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                ShortenUrl();
        }

        private void customBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                ShortenUrl();
        }

        private void shortenBtn_Click(object sender, RoutedEventArgs e)
        {
            copyConfirmText.Visibility = Visibility.Collapsed;
            
            ShortenUrl();
        }

        private void copyBtn_Click(object sender, RoutedEventArgs e)
        {
            DataPackage copyText = new DataPackage();
            copyText.SetText(resultBox.Text);

            Clipboard.SetContent(copyText);

            copyConfirmText.Visibility = Visibility.Visible;
        }

        private async void openBtn_Click(object sender, RoutedEventArgs e)
        {
            copyConfirmText.Visibility = Visibility.Collapsed;

            await Windows.System.Launcher.LaunchUriAsync(new Uri(resultBox.Text));
        }

        private void urlBox_GotFocus(object sender, RoutedEventArgs e)
        {
            copyConfirmText.Visibility = Visibility.Collapsed;
            
            if (urlBox.Text.Equals("Enter the URL you want to shorten...", StringComparison.OrdinalIgnoreCase))
                urlBox.Text = "";
        }

        private void urlBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(urlBox.Text))
                urlBox.Text = "Enter the URL you want to shorten...";
        }

        private void urlBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (urlBox.Text == "" || urlBox.Text == "Enter the URL you want to shorten...")
                shortenBtn.IsEnabled = false;
            else
                shortenBtn.IsEnabled = true;
        }

        private void customBox_GotFocus(object sender, RoutedEventArgs e)
        {
            copyConfirmText.Visibility = Visibility.Collapsed;
            
            //if (customBox.Text.Equals("Enter the phrase you want to use in your shortened URL...", StringComparison.OrdinalIgnoreCase))
            //    customBox.Text = "";
        }

        private void customBox_LostFocus(object sender, RoutedEventArgs e)
        {
            //if (string.IsNullOrEmpty(customBox.Text))
            //    customBox.Text = "Enter the phrase you want to use in your shortened URL...";
        }

        private void resultBox_GotFocus(object sender, RoutedEventArgs e)
        {
            copyConfirmText.Visibility = Visibility.Collapsed;
        }

        private void ScrollBar_PointerWheelChanged_1(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint pt = e.GetCurrentPoint(sender as FrameworkElement);
            ((ScrollBar)sender).Value -= pt.Properties.MouseWheelDelta;
        }

        private void ShareTextHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;

            if (gridHistory.SelectedIndex != -1)
            {
                request.Data.Properties.Title = storedDataShortList[gridHistory.SelectedIndex] + " - Mee.la";
                request.Data.Properties.Description = "Shortened URL for " + storedDataLongList[gridHistory.SelectedIndex];
                request.Data.SetUri(new Uri(storedDataShortList[gridHistory.SelectedIndex]));
                request.Data.SetText(storedDataShortList[gridHistory.SelectedIndex]);
            }
            else if (resultBox.Text != "")
            {
                request.Data.Properties.Title = resultBox.Text + " - Mee.la";
                request.Data.Properties.Description = "Shortened URL for " + urlBoxLatest;
                request.Data.SetUri(new Uri(resultBox.Text));
                request.Data.SetText(resultBox.Text);
            }
            else
                request.FailWithDisplayText("To share from Mee.la, shorten a URL or select one from your history, then try to share.");
        }
    }
}
