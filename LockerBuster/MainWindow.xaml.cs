using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace LockerBuster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string TargetFolder { get; set; }
        private string[] TargetFiles { get; set; }
        private string DestinationFolder { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private string[] GetSelectedFiles()
        {
            List<string> files = new List<string>();

            if (!string.IsNullOrEmpty(TargetFolder))
            {
                files.AddRange(Directory.EnumerateFiles(TargetFolder, "*.*", SearchOption.AllDirectories)
                    .Where(x => x.EndsWith(".pl2") || x.EndsWith(".vl2")));
            }
            else if (TargetFiles.Length > 0)
            {
                files.AddRange(TargetFiles.Where(x => x.EndsWith(".pl2") || x.EndsWith(".vl2")));
            }

            return files.ToArray();
        }

        private void RefreshFileList()
        {
            FileList.Items.Clear();
            foreach (var file in GetSelectedFiles()) FileList.Items.Add(file);
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
                TargetFolder = dialog.FileName;
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
            foreach (var file in GetSelectedFiles())
            {
                Decrypt(file, DestinationFolder);
                FileList.Items.Remove(file);
            }
        }

        private void Decrypt(string filePath, string destination)
        {
            var encryptedBytes = File.ReadAllBytes(filePath);
            var byteLength = Math.Min(encryptedBytes.Length, 2048);
            byte[] encrypted = new byte[byteLength];

            Array.Copy(encryptedBytes, 0, encrypted, 0, byteLength);
            byte[] decrypted = XOROperations.Decrypt(encrypted);

            Array.Copy(decrypted, 0, encryptedBytes, 0, byteLength);

            File.WriteAllBytes($"{destination}/{Path.GetFileNameWithoutExtension(filePath)}", encryptedBytes);
        }
    }
}
