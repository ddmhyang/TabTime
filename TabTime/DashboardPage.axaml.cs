using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TabTime;

public partial class DashboardPage : Window
{
    public DashboardPage()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}