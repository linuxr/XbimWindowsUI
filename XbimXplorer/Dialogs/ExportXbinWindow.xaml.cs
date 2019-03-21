﻿using System;
using System.IO;
using System.Windows;
using Xbim.Ifc;
using XbinConverter;
using XbinConverter.Export;

namespace XbimXplorer.Dialogs
{
    /// <summary>
    /// ExportXbinWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExportXbinWindow
    {
        public ExportXbinWindow()
        {
            InitializeComponent();
        }
        
        public ExportXbinWindow(XplorerMainWindow callingWindow) : this()
        {
            _mainWindow = callingWindow;
            TxtFolderName.Text = Path.Combine(
                new FileInfo(_mainWindow.GetOpenedModelFileName()).DirectoryName, 
                "Export" 
            );
        }
        private XplorerMainWindow _mainWindow;

        private void DoExport(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(TxtFolderName.Text))
            {
                try
                {
                    Directory.CreateDirectory(TxtFolderName.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error creating directory. Select a different location.");
                    return;
                }
            }
            
            // file preparation
            //
            var ifcPath = _mainWindow.GetOpenedModelFileName();
            var xbinFilePath = GetExportName("xbin");
            try
            {
                var converter = new ConverterGLB();
                converter.Convert(ifcPath, xbinFilePath);
            }
            catch (Exception ce)
            {
                if (CancelAfterNotification("Error exporting xbin file.", ce))
                {
                    return;
                }
            }
            
            Close();
        }

        private string GetExportName(string extension, int progressive = 0)
        {
            var basefile = new FileInfo(_mainWindow.GetOpenedModelFileName());
            var wexbimFileName = Path.Combine(TxtFolderName.Text, basefile.Name);
            if (progressive != 0)
                extension = progressive + "." + extension;
            wexbimFileName = Path.ChangeExtension(wexbimFileName, extension);
            return wexbimFileName;
        }

        private bool CancelAfterNotification(string errorZoneMessage, Exception ce)
        {
            var message = errorZoneMessage + "\r\n" + ce.Message + "\r\n";
          
            var ret = MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return ret != MessageBoxResult.Yes;
        }

        private void SelectDirectory(object sender, RoutedEventArgs e)
        {
            using(var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    TxtFolderName.Text = fbd.SelectedPath;
                }
            }
        }
    }

}
