using System.ComponentModel;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MajaFaktura;

public partial class MainWindow : Window
{
    private InvoiceData _invoice;
    private string? _currentFilePath;
    private CompanySettings _companySettings;

    private const string FileFilter = "Faktura-filer (*.faktura)|*.faktura|Alla filer (*.*)|*.*";

    private static readonly string[] CompanyPropertyNames =
    [
        nameof(InvoiceData.CompanyName),
        nameof(InvoiceData.CompanyAddress),
        nameof(InvoiceData.CompanyPostalCity),
        nameof(InvoiceData.CompanyPhone),
        nameof(InvoiceData.CompanyEmail),
        nameof(InvoiceData.CompanyVatNumber),
        nameof(InvoiceData.CompanyBankInfo),
        nameof(InvoiceData.CompanyBankName)
    ];

    public MainWindow()
    {
        InitializeComponent();
        ClampToWorkArea();

        _companySettings = CompanySettingsService.Load();
        _invoice = CreateNewInvoice();
        DataContext = _invoice;
        UpdateTitle();
    }

    private void ClampToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        if (Width > workArea.Width)
            Width = workArea.Width;
        if (Height > workArea.Height)
            Height = workArea.Height;
    }

    private InvoiceData CreateNewInvoice()
    {
        var invoice = new InvoiceData();
        CompanySettingsService.ApplyTo(invoice, _companySettings);
        invoice.Items.Add(new InvoiceItem());
        invoice.PropertyChanged += Invoice_PropertyChanged;
        return invoice;
    }

    private void Invoice_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null && CompanyPropertyNames.Contains(e.PropertyName))
        {
            _companySettings = CompanySettingsService.FromInvoice(_invoice);
            CompanySettingsService.Save(_companySettings);
        }
    }

    private void UpdateTitle()
    {
        var fileName = _currentFilePath != null
            ? System.IO.Path.GetFileName(_currentFilePath)
            : "Ny faktura";
        Title = $"{fileName} - MajaFaktura";
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Vill du skapa en ny faktura? Osparade ändringar går förlorade.",
            "Ny faktura",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        _invoice.PropertyChanged -= Invoice_PropertyChanged;
        _invoice = CreateNewInvoice();
        DataContext = _invoice;
        _currentFilePath = null;
        UpdateTitle();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = FileFilter,
            Title = "Öppna faktura"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var invoice = new InvoiceData();
            InvoiceFileService.Load(invoice, dialog.FileName);

            _invoice.PropertyChanged -= Invoice_PropertyChanged;
            _invoice = invoice;
            _invoice.PropertyChanged += Invoice_PropertyChanged;
            DataContext = _invoice;
            _currentFilePath = dialog.FileName;
            UpdateTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Kunde inte öppna filen.\n\n{ex.Message}",
                "Fel",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_currentFilePath != null)
        {
            SaveToFile(_currentFilePath);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = FileFilter,
            Title = "Spara faktura",
            FileName = string.IsNullOrWhiteSpace(_invoice.InvoiceNumber)
                ? "faktura"
                : $"Faktura_{_invoice.InvoiceNumber}"
        };

        if (dialog.ShowDialog() == true)
        {
            SaveToFile(dialog.FileName);
        }
    }

    private void SaveToFile(string path)
    {
        try
        {
            InvoiceFileService.Save(_invoice, path);
            _currentFilePath = path;
            UpdateTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Kunde inte spara filen.\n\n{ex.Message}",
                "Fel",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        _invoice.Items.Add(new InvoiceItem());
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is InvoiceItem item)
        {
            _invoice.Items.Remove(item);
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var printView = CreatePrintView();
        var dialog = new PrintDialog();

        if (dialog.ShowDialog() == true)
        {
            dialog.PrintVisual(printView, $"Faktura {_invoice.InvoiceNumber}");
        }
    }

    private void SavePdf_Click(object sender, RoutedEventArgs e)
    {
        var printView = CreatePrintView();
        var dialog = new PrintDialog();

        try
        {
            var server = new LocalPrintServer();
            var pdfQueue = server.GetPrintQueues()
                .FirstOrDefault(q => q.Name.Contains("PDF", StringComparison.OrdinalIgnoreCase));

            if (pdfQueue != null)
                dialog.PrintQueue = pdfQueue;
        }
        catch
        {
            // Fall back to default printer selection
        }

        if (dialog.ShowDialog() == true)
        {
            dialog.PrintVisual(printView, $"Faktura {_invoice.InvoiceNumber}");
        }
    }

    private InvoicePrintView CreatePrintView()
    {
        var view = new InvoicePrintView { DataContext = _invoice };

        var pageSize = new Size(796, 1123);
        view.Measure(pageSize);
        view.Arrange(new Rect(pageSize));
        view.UpdateLayout();

        return view;
    }
}
