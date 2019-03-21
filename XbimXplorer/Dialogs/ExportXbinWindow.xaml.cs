using System;
using System.IO;
using System.Windows;
using Xbim.Ifc;

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

        private bool CancelAfterNotification(string errorZoneMessage, Exception ce, int totExports)
        {
            var tasksLeft = totExports - 1;
            var message = errorZoneMessage + "\r\n" + ce.Message + "\r\n";
            if (tasksLeft > 0)
            {
                message += "\r\n" +
                           $"Do you wish to continue exporting other {tasksLeft} formats?";
                var ret = MessageBox.Show(message, "Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                return ret != MessageBoxResult.Yes;
            }
            else
            {
                var ret = MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return ret != MessageBoxResult.Yes;
            }
        }
    }

}
