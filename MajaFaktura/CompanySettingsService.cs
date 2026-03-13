using System.IO;
using System.Text.Json;

namespace MajaFaktura;

public class CompanySettings
{
    public string CompanyName { get; set; } = "Maria Padyasek";
    public string CompanyAddress { get; set; } = "Tornfalksgatan 4B";
    public string CompanyPostalCity { get; set; } = "215-60 Malmö";
    public string CompanyPhone { get; set; } = "0704-356410";
    public string CompanyEmail { get; set; } = "majka71@hotmail.com";
    public string CompanyVatNumber { get; set; } = "SE7101024201";
    public string CompanyBankInfo { get; set; } = "Bankgiro 386-2620";
    public string CompanyBankName { get; set; } = "Handelsbanken";
}

public static class CompanySettingsService
{
    private static readonly string SettingsFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MajaFaktura");

    private static readonly string SettingsPath =
        Path.Combine(SettingsFolder, "company.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static CompanySettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<CompanySettings>(json, JsonOptions) ?? new CompanySettings();
            }
        }
        catch
        {
            // Return defaults on any error
        }

        return new CompanySettings();
    }

    public static void Save(CompanySettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail — not critical
        }
    }

    public static void ApplyTo(InvoiceData invoice, CompanySettings settings)
    {
        invoice.CompanyName = settings.CompanyName;
        invoice.CompanyAddress = settings.CompanyAddress;
        invoice.CompanyPostalCity = settings.CompanyPostalCity;
        invoice.CompanyPhone = settings.CompanyPhone;
        invoice.CompanyEmail = settings.CompanyEmail;
        invoice.CompanyVatNumber = settings.CompanyVatNumber;
        invoice.CompanyBankInfo = settings.CompanyBankInfo;
        invoice.CompanyBankName = settings.CompanyBankName;
    }

    public static CompanySettings FromInvoice(InvoiceData invoice)
    {
        return new CompanySettings
        {
            CompanyName = invoice.CompanyName,
            CompanyAddress = invoice.CompanyAddress,
            CompanyPostalCity = invoice.CompanyPostalCity,
            CompanyPhone = invoice.CompanyPhone,
            CompanyEmail = invoice.CompanyEmail,
            CompanyVatNumber = invoice.CompanyVatNumber,
            CompanyBankInfo = invoice.CompanyBankInfo,
            CompanyBankName = invoice.CompanyBankName
        };
    }
}
