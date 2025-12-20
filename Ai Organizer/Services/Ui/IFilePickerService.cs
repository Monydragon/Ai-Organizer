using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Ui;

public interface IFilePickerService
{
    Task<IReadOnlyList<string>> PickFoldersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> PickFilesAsync(CancellationToken cancellationToken);
}


