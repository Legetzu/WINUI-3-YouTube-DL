using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.AccessCache;
using Windows.Storage;
using Windows.Storage.Pickers;
using WINUI3_YTDL.ViewModels;
using System.Runtime.InteropServices;
using System.Linq.Expressions;
using ABI.Microsoft.UI.Xaml;
using Windows.Devices.Printers;
using System.Windows;
using WinUIEx.Messaging;
using Windows.UI.Popups;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Windows.System;

namespace WINUI3_YTDL.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
    private async void PickFolderButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Clear previous returned file name, if it exists, between iterations of this scenario
        PickFolderOutputTextBlock.Text = "";

        // Create a folder picker
        var folderPicker = new FolderPicker();

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);

        // Initialize the folder picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        // Set options for your folder picker
        folderPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        folderPicker.FileTypeFilter.Add("*");

        // Open the picker for the user to pick a folder
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            PickFolderOutputTextBlock.Text = "Picked folder: " + folder.Name;
        }
        else
        {
            PickFolderOutputTextBlock.Text = "Operation cancelled.";
        }
    }

    private async void GetURL_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var youtube = new YoutubeClient();
        var URL = URLTextBox.Text;
        try
        {
            var video = await youtube.Videos.GetAsync(URL);
            VidInfoPanel.Visibility = Visibility.Visible;
            Info.IsOpen = false;
            DownloadPanel.Visibility = Visibility.Visible;
            VideoName.Text = video.Title;
            TitleTextBox.Text = video.Title;
            DurationTextBox.Text = video.Duration.ToString();
            AuthorTextBox.Text = video.Author.ChannelTitle;
        }
        catch
        {
            VidInfoPanel.Visibility = Visibility.Collapsed;
            Info.Message = "URL is invalid or empty.";
            Info.IsOpen = true;
        }
    }

    private async void Download_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var path = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("PickedFolderToken");
            Console.WriteLine(path.Path);
            var youtube = new YoutubeClient();
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(URLTextBox.Text);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
            progressBar.Visibility = Visibility.Visible;
            var progress = new Progress<double>(p =>
            {
                progressBar.Value = Convert.ToInt32(p * 100);
                progress_label.Text = Convert.ToInt32(p * 100) + "%";
            });
            try { 
                await youtube.Videos.Streams.DownloadAsync(streamInfo, $"{path.Path}//{VideoName.Text}.{streamInfo.Container}", progress);
            } catch
            {
                Info.Message = "Couldn't download video. Video name could contain illegal characters.";
                Info.IsOpen = true;
            }
            
        }
        catch
        {
            Info.Message = "Path/link is invalid or empty.";
            Info.IsOpen = true;
            return;
        }


    }
}
