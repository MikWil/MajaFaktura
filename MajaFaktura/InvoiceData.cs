using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MajaFaktura;

public class InvoiceItem : INotifyPropertyChanged
{
    private string _service = string.Empty;
    private decimal _priceExclVat;
    private string _hours = string.Empty;
    private decimal _vatRate = 25;

    public string Service
    {
        get => _service;
        set => SetField(ref _service, value);
    }

    public decimal PriceExclVat
    {
        get => _priceExclVat;
        set
        {
            if (SetField(ref _priceExclVat, value))
            {
                OnPropertyChanged(nameof(VatAmount));
                OnPropertyChanged(nameof(TotalInclVat));
            }
        }
    }

    public string Hours
    {
        get => _hours;
        set => SetField(ref _hours, value);
    }

    public decimal VatRate
    {
        get => _vatRate;
        set
        {
            if (SetField(ref _vatRate, value))
            {
                OnPropertyChanged(nameof(VatAmount));
                OnPropertyChanged(nameof(TotalInclVat));
            }
        }
    }

    public decimal VatAmount => Math.Round(PriceExclVat * VatRate / 100, 2);
    public decimal TotalInclVat => PriceExclVat + VatAmount;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

public class InvoiceData : INotifyPropertyChanged
{
    public InvoiceData()
    {
        Items.CollectionChanged += Items_CollectionChanged;
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (InvoiceItem item in e.NewItems)
                item.PropertyChanged += Item_PropertyChanged;

        if (e.OldItems != null)
            foreach (InvoiceItem item in e.OldItems)
                item.PropertyChanged -= Item_PropertyChanged;

        NotifyTotalsChanged();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InvoiceItem.PriceExclVat) or nameof(InvoiceItem.VatRate))
            NotifyTotalsChanged();
    }

    private void NotifyTotalsChanged()
    {
        OnPropertyChanged(nameof(SubtotalExclVat));
        OnPropertyChanged(nameof(NetAfterDeduction));
        OnPropertyChanged(nameof(VatDisplayRate));
        OnPropertyChanged(nameof(TotalVatAmount));
        OnPropertyChanged(nameof(Rounding));
        OnPropertyChanged(nameof(GrandTotal));
    }

    private string _invoiceNumber = string.Empty;
    private DateTime _invoiceDate = DateTime.Today;
    private DateTime _dueDate = DateTime.Today.AddDays(30);

    private string _customerName = string.Empty;
    private string _customerAddress = string.Empty;
    private string _customerPostalCity = string.Empty;
    private string _customerPersonalNumber = string.Empty;

    private string _reference = string.Empty;
    private decimal _lateInterestRate = 9.5m;
    private decimal _rutDeduction;

    private string _companyName = string.Empty;
    private string _companyAddress = string.Empty;
    private string _companyPostalCity = string.Empty;
    private string _companyPhone = string.Empty;
    private string _companyEmail = string.Empty;
    private string _companyVatNumber = string.Empty;
    private string _companyBankInfo = string.Empty;
    private string _companyBankName = string.Empty;

    public ObservableCollection<InvoiceItem> Items { get; } = new();

    public string InvoiceNumber
    {
        get => _invoiceNumber;
        set => SetField(ref _invoiceNumber, value);
    }

    public DateTime InvoiceDate
    {
        get => _invoiceDate;
        set => SetField(ref _invoiceDate, value);
    }

    public DateTime DueDate
    {
        get => _dueDate;
        set => SetField(ref _dueDate, value);
    }

    public string CustomerName
    {
        get => _customerName;
        set => SetField(ref _customerName, value);
    }

    public string CustomerAddress
    {
        get => _customerAddress;
        set => SetField(ref _customerAddress, value);
    }

    public string CustomerPostalCity
    {
        get => _customerPostalCity;
        set => SetField(ref _customerPostalCity, value);
    }

    public string CustomerPersonalNumber
    {
        get => _customerPersonalNumber;
        set => SetField(ref _customerPersonalNumber, value);
    }

    public string Reference
    {
        get => _reference;
        set => SetField(ref _reference, value);
    }

    public decimal LateInterestRate
    {
        get => _lateInterestRate;
        set => SetField(ref _lateInterestRate, value);
    }

    public decimal RutDeduction
    {
        get => _rutDeduction;
        set
        {
            if (SetField(ref _rutDeduction, value))
                NotifyTotalsChanged();
        }
    }

    public string CompanyName { get => _companyName; set => SetField(ref _companyName, value); }
    public string CompanyAddress { get => _companyAddress; set => SetField(ref _companyAddress, value); }
    public string CompanyPostalCity { get => _companyPostalCity; set => SetField(ref _companyPostalCity, value); }
    public string CompanyPhone { get => _companyPhone; set => SetField(ref _companyPhone, value); }
    public string CompanyEmail { get => _companyEmail; set => SetField(ref _companyEmail, value); }
    public string CompanyVatNumber { get => _companyVatNumber; set => SetField(ref _companyVatNumber, value); }
    public string CompanyBankInfo { get => _companyBankInfo; set => SetField(ref _companyBankInfo, value); }
    public string CompanyBankName { get => _companyBankName; set => SetField(ref _companyBankName, value); }

    public decimal SubtotalExclVat => Items.Sum(i => i.PriceExclVat);
    public decimal NetAfterDeduction => SubtotalExclVat - Math.Abs(RutDeduction);
    public decimal VatDisplayRate => Items.FirstOrDefault()?.VatRate ?? 25m;
    public decimal TotalVatAmount => Math.Round(NetAfterDeduction * VatDisplayRate / 100, 2);

    public decimal Rounding
    {
        get
        {
            var exact = NetAfterDeduction + TotalVatAmount;
            return Math.Round(exact, 0, MidpointRounding.AwayFromZero) - exact;
        }
    }

    public decimal GrandTotal => Math.Round(NetAfterDeduction + TotalVatAmount, 0, MidpointRounding.AwayFromZero);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
