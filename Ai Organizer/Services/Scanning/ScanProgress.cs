namespace Ai_Organizer.Services.Scanning;

public sealed record ScanProgress(
    string CurrentRoot,
    int DirectoriesVisited,
    int FilesVisited,
    int FilesMatched);


