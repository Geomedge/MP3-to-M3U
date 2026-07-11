using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using WinRT.Interop;
using WinUIEx;

namespace M3U_to_MP3
{

    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

        }

        private int[] ButtonUnlock = {0,0,0};

        // Global variable to store the selected file and folder paths
        private string selectedFilePath = "";
        private string selectedFolderPath = "";

        // Global variable where to save the files
        private string saveFolderPath = "";

        //Global variable to store the error log path and the list of errors
        List<string> errorSource = new List<string>();
        int counter = 0;

        void checkunlock()
        {
            if (ButtonUnlock[0] == 1 && ButtonUnlock[1] == 1 && ButtonUnlock[2] == 1)
            {
                StartButton.IsEnabled = true;
            }
        }


        private async void SelectSaveFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            // Get the window handle (HWND) and initialize the picker
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                saveFolderPath = folder.Path;
                ButtonUnlock[0] = 1;
                SaveFolderTextBlock.Text = folder.Path;
                checkunlock();
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Selected Save Folder",
                    Content = folder.Path,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            else
            {
                ButtonUnlock[0] = 0;
            }
        }

        private async void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            // Get the window handle (HWND) and initialize the picker
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                selectedFolderPath = folder.Path;
                ButtonUnlock[1] = 1;
                MusicFolderTextBlock.Text = folder.Path;
                checkunlock();
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Selected Folder",
                    Content = folder.Path,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            else
            {
                ButtonUnlock[1] = 0;
            }
        }


        private async void SelectM3U_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();

            // Get the window handle (HWND) and initialize the picker
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".m3u");
            picker.FileTypeFilter.Add(".m3u8");

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                selectedFilePath = file.Path;
                ButtonUnlock[2] = 1;
                M3UFileTextBlock.Text = file.Path;
                checkunlock();
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Selected File",
                    Content = file.Path,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
            else
            {
                ButtonUnlock[2] = 0;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AppNotification notification = new AppNotificationBuilder()
            .AddText("M3U to MP3")
            .AddText("Processing" + selectedFilePath)
            .AddProgressBar(new AppNotificationProgressBar()
            {
                Title = "Progress",
                Value = 0.75,
                Status = "In progress..."
            })
            .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }


        //Button for starting the program and moving the files to the selected folder
        private async Task copyPath(string initialPath)
        {
            initialPath = selectedFolderPath + "\\" + initialPath;
            string fileName = Path.GetFileName(initialPath);
            Console.WriteLine("Copying: " + initialPath);
            await Task.Run(() =>
            {
                try
                {
                    File.Copy(initialPath, Path.Combine(saveFolderPath, fileName), true);
                }
                catch (Exception ex)
                {
                    // Log the error and add it to the errorSource list
                    Console.WriteLine("Error copying file: " + initialPath + " - " + ex.Message);
                    errorSource.Add(initialPath);
                }
            });
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            counter = 0;
            errorSource.Clear(); // Clear the list of errors
            int b = File.ReadAllLines(selectedFilePath).Length;
            using (StreamReader sr = new StreamReader(selectedFilePath))
            {
                string? line;
                line = sr.ReadLine(); // Removes the first line of the file which should be #EXTM3U
                while ((line = sr.ReadLine()) != null)
                {
                    // Removes any data but the path of the song
                    counter = counter + 1;
                    line = line.Replace("../", "");
                    line = line.Replace("3432-3330/", "");
                    line = line.Replace("890E-2AB0/", "");
                    await copyPath(line);
                    double progress = (double)counter / b * 100;
                    if (ProgressBar1?.DispatcherQueue != null)
                    {
                        ProgressBar1.DispatcherQueue.TryEnqueue(() => { ProgressBar1.Value = progress; });
                    }
                    else
                    {
                        var dq = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                        dq?.TryEnqueue(() => { ProgressBar1!.Value = progress; });
                    }
                }
            }

            double percentageCopied = (double)(counter - errorSource.Count) / counter * 100;

            // Write errors to a text file
            using (StreamWriter sw = new StreamWriter(saveFolderPath + "\\Errors.txt"))
            {
                foreach (string error in errorSource)
                {
                    sw.WriteLine("Error: " + error);
                }
            }
            ButtonUnlock = [0, 0, 0];

            AppNotification notification = new AppNotificationBuilder()
            .AddText("M3U to MP3")
            .AddText("Finished Converting")
            .AddText("You can find your files in: " + saveFolderPath)
            .AddButton(new AppNotificationButton("Perform action without launching app")
            .AddArgument("action", "BackgroundAction"))
            .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
    }
}
