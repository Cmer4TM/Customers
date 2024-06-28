using Customers.Infrastructure;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using OfficeOpenXml;
using PropertyChanged;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Models;

namespace Customers.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    internal class MainWindowViewModel
    {
        private static readonly string DefaultExcelMessage = "Select .xlsx file to continue";

        public WebView2 Browser { get; set; }
        public Customer Customer { get; set; } = new();
        public string Path { get; set; }
        public string Sheet { get; set; }
        public ICommand AddCustomerCommand { get; }
        public ICommand SelectExcelDocumentCommand { get; }

        public MainWindowViewModel(WebView2 browser)
        {
            Browser = browser;

            AddCustomerCommand = new RelayCommand(AddCustomer, CanAddCustomer);
            SelectExcelDocumentCommand = new RelayCommand(SelectExcelDocument);

            using FileStream fs = new("settings.txt", FileMode.OpenOrCreate);
            using StreamReader sr = new(fs);

            Sheet = sr.ReadLine() ?? string.Empty;
            Path = sr.ReadLine() ?? DefaultExcelMessage;
        }
        public async void AddCustomer()
        {
            await using FileStream fs = new("message.txt", FileMode.OpenOrCreate);
            using StreamReader sr = new(fs);
            using ExcelPackage package = new(Path);

            string msg = (await sr.ReadToEndAsync())
                .Replace(" ", "%20")
                .Replace("\n", "%0A");

            ExcelPackage.LicenseContext = LicenseContext.Commercial;

            ExcelWorksheet worksheet = package.Workbook.Worksheets[Sheet];

            if (worksheet is null)
            {
                MessageBox.Show("Selected sheet is incorrect", "Error");
                return;
            }

            ExcelAddressBase address = worksheet.Dimension;
            int startRow = address is null ? 1 : address.End.Row + 1;

            worksheet.Cells[startRow, 1].Value = Customer.Name;
            worksheet.Cells[startRow, 2].Value = Customer.City;
            worksheet.Cells[startRow, 4].Value = Customer.Phone;
            worksheet.Cells[startRow, 5].Value = Customer.Product;

            try
            {
                await package.SaveAsync();
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show($"Сan't access file {Path}\nBecause it's alredy in use. Close the file before continue", "Error");
                return;
            }

            Browser.CoreWebView2.Navigate($"https://web.whatsapp.com/send?phone=+353{string.Concat(Customer.Phone.Where(char.IsDigit))}&text={msg}");
            
            Customer = new();

            CoreWebView2ExecuteScriptResult result;
            do
            {
                result = await Browser.CoreWebView2.ExecuteScriptWithResultAsync("document.querySelector('[class=\"_ahwq x6s0dn4 xzp58vz xz6pen6 x1hij43t x13yjgf2 xy9n6vp x78zum5 x1q0g3np x1wl59ut x1n6pog2 x6ikm8r x10wlt62 xnhwuio x1sqbtui xdj266r x11i5rnm xat24cr x1mh8g0r x1x1rfll xh8yej3 xaayvut x1sapvj0\"]') !== null");
            }
            while (result.ResultAsJson == "false");

            await Browser.CoreWebView2.ExecuteScriptAsync("document.querySelector('[data-icon=\"send\"]').parentElement.click()");
        }

        public bool CanAddCustomer()
            => string.IsNullOrWhiteSpace(Customer.Name) == false
            && string.IsNullOrWhiteSpace(Customer.Phone) == false
            && string.IsNullOrWhiteSpace(Sheet) == false
            && Path != DefaultExcelMessage;
        public async void SelectExcelDocument()
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                Path = dialog.FileName;
                await TextChanged();
            }
        }
        public async Task TextChanged()
        {
            await using FileStream fs = new("settings.txt", FileMode.Create);
            await using StreamWriter sw = new(fs);

            await sw.WriteAsync(Sheet + "\n" + Path);
        }
    }
}
