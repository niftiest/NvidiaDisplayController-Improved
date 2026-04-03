using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NvidiaDisplayController.Interface.HotkeyCapture;

public partial class HotkeyCaptureView : UserControl
{
    public HotkeyCaptureView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Focus();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is HotkeyCaptureViewModel viewModel)
        {
            viewModel.OnPreviewKeyDown(e.Key, Keyboard.Modifiers);
            e.Handled = true;
        }
    }
}
