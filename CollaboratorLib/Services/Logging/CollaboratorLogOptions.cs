namespace StoryCollaborator.Services.Logging;

using System;
using System.IO;

public class CollaboratorLogOptions
{
    public bool AllowSensitiveLogging { get; set; }

    public string? ElmahApiKey { get; set; }

    public string? ElmahLogId { get; set; }

    public string LogFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StoryCAD",
        "Collaborator",
        "logs");

    public NLog.LogLevel MinimumLevel { get; set; } = NLog.LogLevel.Info;
}
