using Avalonia.Controls;
using Ai_Organizer.ViewModels;

namespace Ai_Organizer;

public partial class MainWindow : Window
{
    // Parameterless ctor keeps Avalonia's runtime XAML loader and designer happy.
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}