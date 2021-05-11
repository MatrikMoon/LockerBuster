using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace LockerBuster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] _targetFiles;
        private string[] TargetFiles {
            get => _targetFiles;
            set {
                //Add folders and files that end in the pl2 or vl2, or are directories
                var filesAndFolders = new List<string>();
                filesAndFolders.AddRange(
                    value.Where(x => 
                            x.EndsWith(".pl2") ||
                            x.EndsWith(".vl2") ||
                            File.GetAttributes(x).HasFlag(FileAttributes.Directory)
                        )
                    );
                _targetFiles = filesAndFolders.ToArray();
            }
        }

        private string DestinationFolder { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void RefreshFileList()
        {
            FileList.Items.Clear();
            foreach (var file in TargetFiles ?? new string[] { }) FileList.Items.Add(file);
        }

        private int GetTotalFileCount() {
            var counter = 0;
            foreach (var item in TargetFiles)
            {
                if (File.GetAttributes(item).HasFlag(FileAttributes.Directory))
                {
                    counter += Directory.EnumerateFiles(item, "*.*", SearchOption.AllDirectories).Where(x => x.EndsWith(".pl2") || x.EndsWith(".vl2")).Count();
                }
                else
                {
                    counter++;
                }
            }
            return counter;
        }

        private void TargetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderSelectDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Title = "Select a folder containing the encrypted files"
            };
            if (dialog.Show())
            {
                TargetFiles = new[] { dialog.FileName };
                RefreshFileList();
            }
        }

        private void TargetFilesButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TargetFiles = fileDialog.FileNames;
                RefreshFileList();
            }
        }

        private void DestinationFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderSelectDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Title = "Select a folder to place the decrypted files"
            };
            if (dialog.Show())
            {
                DestinationFolder = dialog.FileName;
                DestinationLabel.Content = DestinationFolder;
                RefreshFileList();
            }
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            DestinationFolderButton.IsEnabled = false;
            TargetFilesButton.IsEnabled = false;
            DecryptionProgressBar.IsEnabled = true;

            DecryptionProgressBar.Value = 0;
            DecryptionProgressBar.Maximum = GetTotalFileCount();

            Task.Run(() =>
            {
                foreach (var file in TargetFiles)
                {
                    if (File.GetAttributes(file).HasFlag(FileAttributes.Directory))
                    {
                        DirectoryDecrypt(file, DestinationFolder);
                    }
                    else
                    {
                        FileDecrypt(file, DestinationFolder);
                        Dispatcher.Invoke(() => DecryptionProgressBar.Value++);
                    }
                    Dispatcher.Invoke(() => FileList.Items.Remove(file));
                }
            });

            DestinationFolderButton.IsEnabled = true;
            TargetFilesButton.IsEnabled = true;
            DecryptionProgressBar.IsEnabled = false;
        }

        private void FileDecrypt(string filePath, string destination)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var byteLength = Math.Min(fileBytes.Length, 2048);
            byte[] encrypted = new byte[byteLength];

            Array.Copy(fileBytes, 0, encrypted, 0, byteLength);
            byte[] decrypted = XOROperations.Decrypt(encrypted);

            Array.Copy(decrypted, 0, fileBytes, 0, byteLength);

            File.WriteAllBytes($"{destination}/{Path.GetFileNameWithoutExtension(filePath)}", fileBytes);
        }

        private void DirectoryDecrypt(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Decrypt files to new destination, but only .pl2 and .vl2
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.FullName.EndsWith(".pl2") || file.FullName.EndsWith(".vl2"))
                {
                    FileDecrypt(file.FullName, destDirName);
                    Dispatcher.Invoke(() => DecryptionProgressBar.Value++);
                }
            }

            // Copy subdirectories
            foreach (DirectoryInfo subdir in dirs)
            {
                DirectoryDecrypt(subdir.FullName, Path.Combine(destDirName, subdir.Name));
            }
        }
    }
}
