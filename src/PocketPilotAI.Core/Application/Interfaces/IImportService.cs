using PocketPilotAI.Core.Application.Dtos.Import;
using PocketPilotAI.Core.Common;

namespace PocketPilotAI.Core.Application.Interfaces;

public interface IImportService
{
  Task<Result<ImportPreviewDto>> PreviewAsync(Stream csvStream, CancellationToken cancellationToken = default);

  Task<Result<ImportResultDto>> ImportAsync(
    Guid userId,
    Guid accountId,
    Stream csvStream,
    CancellationToken cancellationToken = default);
}
