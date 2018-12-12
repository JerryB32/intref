using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using interflow;

namespace GUI_interflow
{
    /// <summary>
    /// Main window for the GUI application
    /// </summary>
    public partial class MainWindow : Window
    {

        String APIKey;
        String LastDir;
        String CurrentDir;

        //This is the FileObject that contains the info the datagrid populates off of
        public class FileObject
        {
            public string Name { get; set; }
            public string Length { get; set; }
            public string RelativePath { get; set; }
            public string LastModified { get; set; }

            //There are two end points that serve files. Download_ListFiles and File_ListSharedFiles. you have to download these files with different endpoints as well, thus we need to
            //track what endpoint the file came from. If we got it from Download_ListFiles we will mark it as a classicFile and be downloaded with Download_File.
            //If its a file from File_ListSharedFiles it is marked false and downloaded with File_Download. We only show the shared files in the root.
            public Nullable<bool> classicFile { get; set; }
        }

        //A function to make human readable file sizes. Big thanks to Erik Schierboom and deepee1 on github for providing this solution!
        static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        //a function that takes in the response from interflow calls, and outputs a list of FileObjects. Mainly to be used to set the datagrid value
        private List<FileObject> LoadFileData(Dictionary<string, dynamic> FileInput, Dictionary<string, dynamic> DirInput, Dictionary<string, dynamic> SharedFiles = null)
        {
            List<FileObject> FileList = new List<FileObject>();

            //Add the back a level directory. Here its just going to be like every other dir, but later in the code that handles processing dir traversal we will check the name:
            FileList.Add(new FileObject()
            {
                Name = "../",
                Length = "dir",
                RelativePath = "",
                LastModified = "",
                classicFile = null
            });

            //add the normal directories
            foreach (var dir in DirInput["data"])
            {
                FileList.Add(new FileObject()
                {
                    Name = dir,
                    Length = "dir",
                    RelativePath = "",
                    LastModified = "",
                    classicFile = null
                });
            }

            //add the normal files
            foreach (var file in FileInput["data"])
            {
                FileList.Add(new FileObject()
                {
                    Name = file["Name"],
                    Length = BytesToString(Convert.ToInt64(file["Length"])),
                    RelativePath = file["RelativePath"],
                    LastModified = file["LastModified"] ?? "N/A",
                    classicFile = true
                });
            }

            //add the shared files. Here its treated like any other file (except for the classicFile = false). 
            //We check to see if SharedFiles is null, since we only add that to the call if the user is in the root directory.
            if(SharedFiles != null)
            {
                foreach (var file in SharedFiles["data"])
                {
                    FileList.Add(new FileObject()
                    {
                        Name = file["Name"],
                        Length = BytesToString(Convert.ToInt64(file["Length"])),
                        RelativePath = file["RelativePath"],
                        LastModified = file["LastModified"] ?? "Null",
                        classicFile = false
                    });
                }
            }

            return FileList;
        }


        public MainWindow()
        {
            InitializeComponent();
        }


        private async void Buttom_APIKey_Submit_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.IsIndeterminate = true;
            //validate the api key
            if ((await interflow.interflow.getValidation_async(PasswordBox_APIKey.Password))["response"] != 200)
            {
                ProgressBar.IsIndeterminate = false;
                MessageBox.Show("Your API key seems to be invalid!\nPlease enter a valid API key and try again", "ERROR!", MessageBoxButton.OK);
            }
            else
            {
                APIKey = PasswordBox_APIKey.Password;

                //Set our Dirs (needed for ../ processing)
                LastDir = "";
                CurrentDir = "";

                //make rows in the datagrid clickable
                Style rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new EventSetter(DataGridRow.MouseDoubleClickEvent, new MouseButtonEventHandler(Row_DoubleClick)));
                DataGrid_MainView.RowStyle = rowStyle;

                //initialize the datagrid
                DataGrid_MainView.ItemsSource = LoadFileData(
                        (await interflow.interflow.getInterflowFiles_async(APIKey, "")), 
                        (await interflow.interflow.getInterflowDirs_async(APIKey, "")),
                        (await interflow.interflow.getInterflowSharedFiles_async(APIKey)));
                //hide the classicFile column in the data view, its not part of the api and is just a hack to get the old and new style of file to work togeather.
                DataGrid_MainView.Columns[4].Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;
            }
        }

        //handler for when users double click on the data row
        private async void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;

            //FileRow is the FileObject a user clicked on now! this makes it super easy to process (FileRow.Name for instance gets the file name)
            FileObject FileRow = (row.DataContext as FileObject);

            if (FileRow.Length == "dir")
            {
                ProgressBar.IsIndeterminate = true;
                if (FileRow.Name != "../")
                {
                    //we just had the user double click a directory. lets "go into it" by replacing the data in the datagrid with the new directory.
                    //user can never double click a non ../ dir to take them back to the root, thus we dont need to check if we are in root to display shared files
                    DataGrid_MainView.ItemsSource = LoadFileData((await interflow.interflow.getInterflowFiles_async(APIKey, FileRow.Name)), (await interflow.interflow.getInterflowDirs_async(APIKey, FileRow.Name)));
                    //hide the classicFile column in the data view, its not part of the api and is just a hack to get the old and new style of file to work togeather.
                    DataGrid_MainView.Columns[4].Visibility = Visibility.Collapsed;

                    ProgressBar.IsIndeterminate = false;
                    LastDir = CurrentDir;
                    CurrentDir += FileRow.Name;
                }
                else if (FileRow.Name == "../")
                {
                    //someone might try to see whats before the root dir... dont let them (protip: theres nothing but errors)
                    if (CurrentDir == "")
                    {
                        return;
                    }

                    //take the view back up a directory
                    if (LastDir == "")
                    {
                        DataGrid_MainView.ItemsSource = LoadFileData(
                                (await interflow.interflow.getInterflowFiles_async(APIKey, "")),
                                (await interflow.interflow.getInterflowDirs_async(APIKey, "")),
                                (await interflow.interflow.getInterflowSharedFiles_async(APIKey)));
                    }
                    else
                    {
                        DataGrid_MainView.ItemsSource = LoadFileData((await interflow.interflow.getInterflowFiles_async(APIKey, LastDir)), (await interflow.interflow.getInterflowDirs_async(APIKey, LastDir)));
                    }
                    //hide the classicFile column in the data view, its not part of the api and is just a hack to get the old and new style of file to work togeather.
                    DataGrid_MainView.Columns[4].Visibility = Visibility.Collapsed;

                    ProgressBar.IsIndeterminate = false;
                    //set the current dir to reflect where we just took them
                    CurrentDir = LastDir;
                    //remove a directory level from LastDir assuming its not empty
                    if (LastDir != "")
                    {
                        LastDir = LastDir.TrimEnd('/');
                        LastDir = LastDir.Remove(LastDir.LastIndexOf('/'));
                    }
                }
            }
            else
            {
                //user clicked a file, lets prompt them to download it.
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = FileRow.Name; // Default file name
                string extention = System.IO.Path.GetExtension(FileRow.Name);
                dlg.DefaultExt = extention; // Default file extension
                dlg.Filter = "(" + extention + ") | *" + extention; // Filter files by extension

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    string filename = dlg.FileName;
                    ProgressBar.IsIndeterminate = true;

                    //If we are in the root, a user might have tried downloading a shared file. Check and download depending on what type of file it is
                    if (FileRow.classicFile == true)
                    {
                        await interflow.interflow.downloadInterflowFile_async(APIKey, CurrentDir, FileRow.Name, filename);
                    }
                    else
                    {
                        await interflow.interflow.downloadInterflowSharedFile_async(APIKey, FileRow.Name, filename);
                    }
                    ProgressBar.IsIndeterminate = false;
                }
            }
        }

        private async void Button_Download_Click(object sender, RoutedEventArgs e)
        {
            //check to see if items are actually sellected first:
            if (!(DataGrid_MainView.SelectedItems.Count > 0))
            {
                return;
            }

            //Prompt user for output folder
            //TODO: the windows forms dialog lacks in features. implement a 3rd party lib that allows the vista and later style file dialog.
            string OutputPath;
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputPath = dialog.SelectedPath;
                }
                else
                {
                    return;
                }
            }


            int count = DataGrid_MainView.SelectedItems.Count;
            for (int i = 0; i < DataGrid_MainView.SelectedItems.Count; i++)
            {
                FileObject FileRow = (DataGrid_MainView.SelectedItems[i] as FileObject);

                //handle directory downloads (mirroring)
                if (FileRow.Length == "dir")
                {
                    //guarenteed someone is going to try and download "../"
                    if (FileRow.Name == "../")
                    {
                        continue;
                    }

                    var result = MessageBox.Show("You have selected to download the folder: " + FileRow.Name + "\nThis will create a new directory in the output destination and download " +
                                        "all of the contents. BE AWARE that some directories contain multiple gigabytes of data and this operation may take a long time to complete " +
                                        "Do you want to continue?\n\nPressing no will skip this folder and continue with other downloads.", "Warning!", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        //do the download
                        string TmpDirOutputPath = System.IO.Path.Combine(OutputPath, FileRow.Name);
                        System.IO.Directory.CreateDirectory(TmpDirOutputPath);

                        //update the status bar to show a correct representation of progress in the directory:
                        int dirSize = (await interflow.interflow.getInterflowFileNamesOnly_async(APIKey, CurrentDir + FileRow.Name))["data"].Count;
                        int j = 0; //a counter var for our for each loop below.
                        count += dirSize;

                        foreach (dynamic file in (await interflow.interflow.getInterflowFiles_async(APIKey, CurrentDir + FileRow.Name))["data"])
                        {
                            await interflow.interflow.downloadInterflowFile_async(APIKey, (CurrentDir + FileRow.Name), file["Name"], System.IO.Path.Combine(TmpDirOutputPath, file["Name"]));

                            ProgressBar.Value = (((double)(i+j+1) / count) * 100.0);
                            j++;
                        }

                        //we are done downloading the files from the directory... reset the count and update the status bar.
                        count -= dirSize;
                        ProgressBar.Value = (((double)(i + 1) / count) * 100.0);
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    //user is downloading a file, not a dir. if we are in the root, it might be a shared file... do a check and then download based on what it is.
                    if (FileRow.classicFile == true)
                    {
                        await interflow.interflow.downloadInterflowFile_async(APIKey, CurrentDir, FileRow.Name, System.IO.Path.Combine(OutputPath, FileRow.Name));
                    }
                    else
                    {
                        await interflow.interflow.downloadInterflowSharedFile_async(APIKey, FileRow.Name, System.IO.Path.Combine(OutputPath, FileRow.Name));
                    }
                }
                ProgressBar.Value = (((double)(i + 1) / count) * 100.0);
            }
            MessageBox.Show("All files Sucessfully downloaded!", "Finished Processing Downloads.", MessageBoxButton.OK);
            GC.Collect();
            ProgressBar.Value = 0;
        }

        private async void Button_Upload_File_Click(object sender, RoutedEventArgs e)
        {

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.Multiselect = false;

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // open document
                string filename = dlg.FileName;
                if(await interflow.interflow.UploadInterflowGenericFile_async(APIKey, filename) == 202)
                {
                    MessageBox.Show("File upload sucessful", "Finished upload.", MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show("There was an error uploading the file. Please try again", "Finished upload.", MessageBoxButton.OK);
                }
            }
        }

        private async void Button_Upload_OneIndicator_Click(object sender, RoutedEventArgs e)
        {
            //make sure the user doesnt try uploading anything with no api key set
            if(APIKey == null)
            {
                return;
            }

            UploadWindow UploadPopup = new UploadWindow();
            UploadPopup.ShowDialog();

            if(UploadPopup.Submit == false)
            {
                return;
            }

            int returnCode = await interflow.interflow.UploadInterflowOneIndicator_async(APIKey, UploadPopup.OneIndicatorJSON);
            if(returnCode == 201)
            {
                MessageBox.Show("OneIndicator upload sucessful!", "Upload Sucess!", MessageBoxButton.OK);
            }
            else if(returnCode == 400)
            {
                MessageBox.Show("The supplied OneIndicator was not valid! Please verify the OneIndicator and try again.", "ERROR!", MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show("OneIndicator failed with status code: " + returnCode.ToString(), "ERROR!", MessageBoxButton.OK);
            }
        }
    }
}
