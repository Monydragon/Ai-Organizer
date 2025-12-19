using Avalonia.Controls;
using Ai_Organizer.ViewModels;

namespace Ai_Organizer;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}