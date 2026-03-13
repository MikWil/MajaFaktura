using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MajaFaktura;

public class InvoiceItemDto
{
    public string Service { get; set; } = string.Empty;
    public decimal PriceExclVat { get; set; }
    public string Hours { get; set; } = string.Empty;
    public decimal VatRate { get; set; } = 25;
}

public class InvoiceDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerPostalCity { get; set; } = string.Empty;
    public string CustomerPersonalNumber { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;
    public decimal LateInterestRate { get; set; }
    public decimal RutDeduction { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyPostalCity { get; set; } = string.Empty;
    public string CompanyPhone { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyVatNumber { get; set; } = string.Empty;
    public string CompanyBankInfo { get; set; } = string.Empty;
    public string CompanyBankName { get; set; } = string.Empty;

    public List<InvoiceItemDto> Items { get; set; } = [];
}

public static class InvoiceFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public static void Save(InvoiceData invoice, string filePath)
    {
        var dto = new InvoiceDto
        {
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            CustomerName = invoice.CustomerName,
            CustomerAddress = invoice.CustomerAddress,
            CustomerPostalCity = invoice.CustomerPostalCity,
            CustomerPersonalNumber = invoice.CustomerPersonalNumber,
            Reference = invoice.Reference,
            LateInterestRate = invoice.LateInterestRate,
            RutDeduction = invoice.RutDeduction,
            CompanyName = invoice.CompanyName,
            CompanyAddress = invoice.CompanyAddress,
            CompanyPostalCity = invoice.CompanyPostalCity,
            CompanyPhone = invoice.CompanyPhone,
            CompanyEmail = invoice.CompanyEmail,
            CompanyVatNumber = invoice.CompanyVatNumber,
            CompanyBankInfo = invoice.CompanyBankInfo,
            CompanyBankName = invoice.CompanyBankName,
            Items = invoice.Items.Select(i => new InvoiceItemDto
            {
                Service = i.Service,
                PriceExclVat = i.PriceExclVat,
                Hours = i.Hours,
                VatRate = i.VatRate
            }).ToList()
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static void Load(InvoiceData invoice, string filePath)
    {
        var json = File.ReadAllText(filePath);
        var dto = JsonSerializer.Deserialize<InvoiceDto>(json, JsonOptions);
        if (dto == null) return;

        invoice.InvoiceNumber = dto.InvoiceNumber;
        invoice.InvoiceDate = dto.InvoiceDate;
        invoice.DueDate = dto.DueDate;
        invoice.CustomerName = dto.CustomerName;
        invoice.CustomerAddress = dto.CustomerAddress;
        invoice.CustomerPostalCity = dto.CustomerPostalCity;
        invoice.CustomerPersonalNumber = dto.CustomerPersonalNumber;
        invoice.Reference = dto.Reference;
        invoice.LateInterestRate = dto.LateInterestRate;
        invoice.RutDeduction = dto.RutDeduction;
        invoice.CompanyName = dto.CompanyName;
        invoice.CompanyAddress = dto.CompanyAddress;
        invoice.CompanyPostalCity = dto.CompanyPostalCity;
        invoice.CompanyPhone = dto.CompanyPhone;
        invoice.CompanyEmail = dto.CompanyEmail;
        invoice.CompanyVatNumber = dto.CompanyVatNumber;
        invoice.CompanyBankInfo = dto.CompanyBankInfo;
        invoice.CompanyBankName = dto.CompanyBankName;

        invoice.Items.Clear();
        foreach (var itemDto in dto.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                Service = itemDto.Service,
                PriceExclVat = itemDto.PriceExclVat,
                Hours = itemDto.Hours,
                VatRate = itemDto.VatRate
            });
        }
    }
}
