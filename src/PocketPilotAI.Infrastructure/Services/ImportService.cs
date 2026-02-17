using Microsoft.EntityFrameworkCore;
using PocketPilotAI.Core.Application.Dtos.Import;
using PocketPilotAI.Core.Application.Dtos.Transactions;
using PocketPilotAI.Core.Application.Interfaces;
using PocketPilotAI.Core.Common;
using PocketPilotAI.Core.Domain.Enums;
using PocketPilotAI.Infrastructure.Persistence;

namespace PocketPilotAI.Infrastructure.Services;

public class ImportService(AppDbContext dbContext, ITransactionService transactionService) : IImportService
{
  private static readonly Dictionary<string, string[]> KnownColumns = new(StringComparer.OrdinalIgnoreCase)
  {
    ["date"] = ["date", "bookingdate", "transactiondate"],
    ["merchant"] = ["merchant", "description", "counterparty", "payee"],
    ["amount"] = ["amount", "value", "sum"],
    ["currency"] = ["currency", "ccy"],
    ["category"] = ["category", "type", "group"]
  };

  public async Task<Result<ImportPreviewDto>> PreviewAsync(Stream csvStream, CancellationToken cancellationToken = default)
  {
    if (!csvStream.CanRead)
    {
      return Result<ImportPreviewDto>.Failure("CSV stream cannot be read.");
    }

    csvStream.Position = 0;
    using StreamReader reader = new(csvStream, leaveOpen: true);

    string? headerLine = await reader.ReadLineAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(headerLine))
    {
      return Result<ImportPreviewDto>.Failure("CSV file is empty.");
    }

    string[] headers = ParseCsvLine(headerLine);
    Dictionary<string, string> mapping = DetectMapping(headers);

    List<ImportRowPreviewDto> rows = new();
    for (int i = 0; i < 10; i++)
    {
      string? line = await reader.ReadLineAsync(cancellationToken);
      if (line is null)
      {
        break;
      }

      string[] values = ParseCsvLine(line);
      if (!TryBuildRow(values, headers, mapping, out ImportRowPreviewDto? row))
      {
        continue;
      }

      rows.Add(row!);
    }

    return Result<ImportPreviewDto>.Success(new ImportPreviewDto
    {
      Headers = headers,
      DetectedMapping = mapping,
      Rows = rows
    });
  }

  public async Task<Result<ImportResultDto>> ImportAsync(
    Guid userId,
    Guid accountId,
    Stream csvStream,
    CancellationToken cancellationToken = default)
  {
    if (!await dbContext.Accounts.AsNoTracking().AnyAsync(x => x.Id == accountId && x.UserId == userId, cancellationToken))
    {
      return Result<ImportResultDto>.Failure("Account not found.");
    }

    csvStream.Position = 0;
    using StreamReader reader = new(csvStream, leaveOpen: true);

    string? headerLine = await reader.ReadLineAsync(cancellationToken);
    if (string.IsNullOrWhiteSpace(headerLine))
    {
      return Result<ImportResultDto>.Failure("CSV file is empty.");
    }

    string[] headers = ParseCsvLine(headerLine);
    Dictionary<string, string> mapping = DetectMapping(headers);

    int total = 0;
    int imported = 0;
    int skipped = 0;
    List<string> warnings = new();

    while (true)
    {
      string? line = await reader.ReadLineAsync(cancellationToken);
      if (line is null)
      {
        break;
      }

      if (string.IsNullOrWhiteSpace(line))
      {
        continue;
      }

      total++;
      string[] values = ParseCsvLine(line);

      if (!TryBuildRow(values, headers, mapping, out ImportRowPreviewDto? row))
      {
        skipped++;
        warnings.Add($"Skipped row {total}: invalid row format.");
        continue;
      }

      TransactionType type = row!.Amount < 0 ? TransactionType.Expense : TransactionType.Income;
      decimal amount = Math.Abs(row.Amount);

      Result<TransactionDto> createResult = await transactionService.CreateAsync(
        userId,
        new CreateTransactionRequest
        {
          AccountId = accountId,
          MerchantName = NormalizeMerchant(row.Merchant),
          Amount = amount,
          Currency = row.Currency,
          DateUtc = row.DateUtc,
          Type = type,
          Notes = "Imported from CSV"
        },
        cancellationToken);

      if (createResult.IsFailure)
      {
        skipped++;
        warnings.Add($"Skipped row {total}: {createResult.Error}");
      }
      else
      {
        imported++;
      }
    }

    return Result<ImportResultDto>.Success(new ImportResultDto
    {
      TotalRows = total,
      ImportedRows = imported,
      SkippedRows = skipped,
      Warnings = warnings
    });
  }

  private static bool TryBuildRow(
    string[] values,
    IReadOnlyList<string> headers,
    IReadOnlyDictionary<string, string> mapping,
    out ImportRowPreviewDto? row)
  {
    row = null;
    if (!TryGetValue("date", values, headers, mapping, out string? dateValue) ||
      !DateTime.TryParse(dateValue, out DateTime date))
    {
      return false;
    }

    if (!TryGetValue("merchant", values, headers, mapping, out string? merchant))
    {
      return false;
    }

    if (!TryGetValue("amount", values, headers, mapping, out string? amountValue) ||
      !decimal.TryParse(amountValue, out decimal amount))
    {
      return false;
    }

    TryGetValue("currency", values, headers, mapping, out string? currency);
    TryGetValue("category", values, headers, mapping, out string? category);

    row = new ImportRowPreviewDto
    {
      DateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc),
      Merchant = merchant ?? "Unknown Merchant",
      Amount = amount,
      Currency = string.IsNullOrWhiteSpace(currency) ? "SEK" : currency.ToUpperInvariant(),
      Category = category
    };

    return true;
  }

  private static bool TryGetValue(
    string key,
    IReadOnlyList<string> values,
    IReadOnlyList<string> headers,
    IReadOnlyDictionary<string, string> mapping,
    out string? value)
  {
    value = null;
    if (!mapping.TryGetValue(key, out string? header))
    {
      return false;
    }

    int index = -1;
    for (int i = 0; i < headers.Count; i++)
    {
      if (string.Equals(headers[i], header, StringComparison.OrdinalIgnoreCase))
      {
        index = i;
        break;
      }
    }

    if (index < 0 || index >= values.Count)
    {
      return false;
    }

    value = values[index].Trim();
    return true;
  }

  private static Dictionary<string, string> DetectMapping(IReadOnlyList<string> headers)
  {
    Dictionary<string, string> mapping = new(StringComparer.OrdinalIgnoreCase);
    foreach ((string target, string[] aliases) in KnownColumns)
    {
      string? match = headers.FirstOrDefault(x => aliases.Contains(x.Trim(), StringComparer.OrdinalIgnoreCase));
      if (!string.IsNullOrWhiteSpace(match))
      {
        mapping[target] = match;
      }
    }

    return mapping;
  }

  private static string[] ParseCsvLine(string line)
  {
    List<string> result = new();
    bool inQuotes = false;
    int start = 0;

    for (int i = 0; i < line.Length; i++)
    {
      if (line[i] == '"')
      {
        inQuotes = !inQuotes;
      }
      else if (line[i] == ',' && !inQuotes)
      {
        result.Add(line[start..i].Trim().Trim('"'));
        start = i + 1;
      }
    }

    result.Add(line[start..].Trim().Trim('"'));
    return result.ToArray();
  }

  private static string NormalizeMerchant(string value)
    => string.IsNullOrWhiteSpace(value) ? "Unknown Merchant" : value.Trim();
}
