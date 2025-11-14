namespace Privatekonomi.Core.Models;

/// <summary>
/// Information about a supported bank institution
/// </summary>
public class BankInfo
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    
    public BankInfo(string name, string colorHex)
    {
        Name = name;
        ColorHex = colorHex;
    }
}

/// <summary>
/// Centralized configuration for supported banks
/// </summary>
public static class BankRegistry
{
    /// <summary>
    /// List of supported banks with their official brand colors
    /// </summary>
    public static readonly List<BankInfo> SupportedBanks = new()
    {
        new BankInfo("Handelsbanken", "#003781"),  // Handelsbanken blue
        new BankInfo("ICA-banken", "#E3000F"),     // ICA red
        new BankInfo("Nordea", "#0000A0"),         // Nordea blue
        new BankInfo("SEB", "#60CD18"),            // SEB green
        new BankInfo("Swedbank", "#FF7900"),       // Swedbank orange
        new BankInfo("Avanza", "#00C281")          // Avanza green/turquoise
    };
    
    /// <summary>
    /// Get bank info by name
    /// </summary>
    public static BankInfo? GetBankByName(string name)
    {
        return SupportedBanks.FirstOrDefault(b => 
            b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Get color for a bank
    /// </summary>
    public static string? GetBankColor(string? bankName)
    {
        if (string.IsNullOrEmpty(bankName))
            return null;
            
        return GetBankByName(bankName)?.ColorHex;
    }
}
