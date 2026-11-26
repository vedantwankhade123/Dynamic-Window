using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DynamicWindow.App.ViewModels;
using DynamicWindow.Core.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using ClipboardEntry = DynamicWindow.Core.Models.ClipboardHistoryItem;

namespace DynamicWindow.App.Controls;

public sealed partial class ExpandedDynamicIslandControl : UserControl
{
    public ExpandedDynamicIslandControl()
    {
        InitializeComponent();
    }

    private void OnActionButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string actionId && DataContext is MainDynamicWindowViewModel vm)
        {
            vm.ExecuteActivityActionCommand.Execute(actionId);
        }
    }

    private async void OnShelfItemDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ClipboardEntry item)
        {
            if (item.Kind == ClipboardItemKind.File && item.FilePaths.Count > 0)
            {
                var deferral = args.GetDeferral();
                try
                {
                    var files = new List<IStorageItem>();
                    foreach (var p in item.FilePaths.Where(File.Exists))
                    {
                        files.Add(await StorageFile.GetFileFromPathAsync(p));
                    }
                    if (files.Count > 0) args.Data.SetStorageItems(files);
                }
                finally { deferral.Complete(); }
            }
            else if (item.Kind == ClipboardItemKind.Image && !string.IsNullOrEmpty(item.ImagePath) && File.Exists(item.ImagePath))
            {
                var deferral = args.GetDeferral();
                try
                {
                    var f = await StorageFile.GetFileFromPathAsync(item.ImagePath);
                    args.Data.SetStorageItems(new IStorageItem[] { f });
                }
                finally { deferral.Complete(); }
            }
            else
            {
                args.Data.SetText(item.Text);
            }
            args.Data.RequestedOperation = DataPackageOperation.Copy;
        }
    }

    private void OnShelfCopyClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ClipboardEntry item && DataContext is MainDynamicWindowViewModel vm)
        {
            vm.CopyClipboardItemCommand.Execute(item);
            btn.Content = "Copied ✓";
        }
    }

    private void OnShelfPinClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && DataContext is MainDynamicWindowViewModel vm)
        {
            vm.TogglePinClipboardItemCommand.Execute(id);
        }
    }

    private void OnShelfRemoveClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id && DataContext is MainDynamicWindowViewModel vm)
        {
            vm.RemoveClipboardItemCommand.Execute(id);
        }
    }
}
