using System;
using System.IO;
using System.Text;

namespace RungProof.Next.VirtualController;

public sealed record LadderProjectSaveResult(string Path, string? BackupPath);

/// <summary>
/// Validates before touching the destination, then replaces it using a flushed
/// same-directory temporary file. Migration backups never overwrite older ones.
/// The caller owns format validation and updates its saved marker only on success.
/// </summary>
public static class LadderProjectFileStore
{
    public static LadderProjectSaveResult SaveValidated(string path, string content,
        Action<string> validate, bool preserveOriginal = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(validate);
        validate(content);
        var destination = System.IO.Path.GetFullPath(path);
        var directory = System.IO.Path.GetDirectoryName(destination)!;
        // Do not create a mistyped/missing directory or falsely report a save.
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        var temporary = System.IO.Path.Combine(directory,
            "." + System.IO.Path.GetFileName(destination) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        string? backup = null;
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(false, true).GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }
            validate(File.ReadAllText(temporary, Encoding.UTF8));
            if (File.Exists(destination))
            {
                if ((File.GetAttributes(destination) & FileAttributes.ReadOnly) != 0)
                    throw new UnauthorizedAccessException("Project is read-only: " + destination);
                if (preserveOriginal)
                {
                    backup = destination + ".before-migration." + Guid.NewGuid().ToString("N") + ".bak";
                    File.Copy(destination, backup, overwrite: false);
                }
                File.Replace(temporary, destination, destinationBackupFileName: null);
            }
            else
                File.Move(temporary, destination, overwrite: false);
            return new(destination, backup);
        }
        finally
        {
            // Only our uniquely named temporary file is eligible for cleanup.
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
