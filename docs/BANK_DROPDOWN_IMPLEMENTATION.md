# Bank Dropdown Implementation

## Översikt

Detta dokument beskriver implementationen av bank-dropdown för kontoredigering i Privatekonomi.

## Förändring

Tidigare användes ett fritextfält för "Bank/Institution" där användare kunde skriva in vilken bank som helst. Nu har detta ersatts med en dropdown-lista med förvalde banker, var och en med sin officiella varumärkesfärg.

## Banker och Färgkoder

Följande banker finns tillgängliga i dropdown-listan med sina officiella varumärkesfärger:

| Bank | Färgkod (hex) | Färg |
|------|---------------|------|
| Handelsbanken | #003781 | Mörkblå |
| ICA-banken | #E3000F | Röd |
| Nordea | #0000A0 | Blå |
| SEB | #60CD18 | Grön |
| Swedbank | #FF7900 | Orange |
| Avanza | #00C281 | Turkos/Grön |

## Teknisk Implementation

### Ny fil: `BankInfo.cs`

Skapad i `src/Privatekonomi.Core/Models/BankInfo.cs`

Innehåller två klasser:
- `BankInfo`: Representerar en bank med namn och färgkod
- `BankRegistry`: Statisk klass med lista över alla stödda banker och hjälpmetoder

```csharp
public static class BankRegistry
{
    public static readonly List<BankInfo> SupportedBanks = new() { ... };
    public static BankInfo? GetBankByName(string name);
    public static string? GetBankColor(string? bankName);
}
```

### Uppdaterade filer

1. **EditAccountDialog.razor**
   - Bytte textfält mot MudSelect dropdown
   - Lagt till färgswatch bredvid banknamn i dropdown
   - Auto-fyller kontots färg när en bank väljs

2. **AddAccountDialog.razor**
   - Samma ändringar som EditAccountDialog
   - Auto-fyller kontots färg när en bank väljs

### Användargränssnitt

Dropdown-listan visar:
- En färgad ruta (16x16px) med bankens färg
- Bankens namn

När användaren väljer en bank:
- Fältet "Institution" sätts till banknamnet
- Fältet "Färg (hex)" fylls automatiskt med bankens färgkod

## Tester

Skapad fil: `tests/Privatekonomi.Core.Tests/BankRegistryTests.cs`

Tester täcker:
- Alla sex banker finns i listan
- Alla banker har definierade färgkoder
- Korrekt färgkod för varje bank
- Case-insensitive sökning
- Hantering av ogiltiga banknamn
- GetBankColor-metoden

## Användning

### För användare

1. Öppna kontoredigering på `/settings/accounts`
2. Klicka "Redigera" eller "Lägg till konto"
3. I fältet "Bank/Institution", klicka för att öppna dropdown
4. Välj din bank från listan
5. Färgfältet fylls automatiskt med bankens färg

### För utvecklare

För att lägga till en ny bank:

```csharp
// I BankInfo.cs, lägg till i SupportedBanks-listan:
new BankInfo("BankNamn", "#HEXFÄRG")
```

## Framtida förbättringar

Möjliga förbättringar:
- Lägg till fler banker
- Visa banklogotyp istället för färgswatch
- Möjlighet att lägga till egna banker
- Bankkonfiguration från databas istället för hårdkodat

## Relaterade filer

- `src/Privatekonomi.Core/Models/BankInfo.cs` - Bankregister och bankinformation
- `src/Privatekonomi.Web/Components/Dialogs/EditAccountDialog.razor` - Dialog för kontoredigering
- `src/Privatekonomi.Web/Components/Dialogs/AddAccountDialog.razor` - Dialog för att lägga till konto
- `tests/Privatekonomi.Core.Tests/BankRegistryTests.cs` - Enhetstester
