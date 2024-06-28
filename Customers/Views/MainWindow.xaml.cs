using Customers.ViewModels;
using System.Windows;

namespace Customers
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel(browser);
        }

        private async void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            => await ((MainWindowViewModel)DataContext).TextChanged();
    }
}