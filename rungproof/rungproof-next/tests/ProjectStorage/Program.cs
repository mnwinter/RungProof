using RungProof.Next.VirtualController;

var root = Path.Combine(Path.GetTempPath(), "RungProofProjectStorage-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
void Validate(string text)
{
    if (text != "valid-new") throw new InvalidDataException("Invalid project.");
}
try
{
    var original = Path.Combine(root, "project.rpproj.json");
    File.WriteAllText(original, "legacy-original");
    var result = LadderProjectFileStore.SaveValidated(original, "valid-new", Validate, preserveOriginal: true);
    Check(File.ReadAllText(original) == "valid-new", "New content must replace destination.");
    Check(result.BackupPath is not null && File.ReadAllText(result.BackupPath) == "legacy-original", "Migration must preserve original bytes.");
    var firstBackup = result.BackupPath;
    File.WriteAllText(original, "second-original");
    result = LadderProjectFileStore.SaveValidated(original, "valid-new", Validate, preserveOriginal: true);
    Check(result.BackupPath != firstBackup && File.ReadAllText(firstBackup!) == "legacy-original", "Backup collision must not overwrite earlier backup.");
    Check(File.ReadAllText(result.BackupPath!) == "second-original", "Second backup must contain second source.");

    File.WriteAllText(original, "keep-original");
    try { LadderProjectFileStore.SaveValidated(original, "invalid", Validate, true); throw new Exception("Expected rejection."); }
    catch (InvalidDataException) { }
    Check(File.ReadAllText(original) == "keep-original", "Validation failure must preserve original.");
    Check(!Directory.EnumerateFiles(root, "*.tmp").Any(), "Validation must leave no temporary file.");

    File.SetAttributes(original, FileAttributes.ReadOnly);
    try { LadderProjectFileStore.SaveValidated(original, "valid-new", Validate); throw new Exception("Expected read-only rejection."); }
    catch (UnauthorizedAccessException) { }
    finally { File.SetAttributes(original, FileAttributes.Normal); }
    Check(File.ReadAllText(original) == "keep-original", "Failed replacement must preserve original.");
    Check(!Directory.EnumerateFiles(root, "*.tmp").Any(), "Failed replacement must clean temporary bytes.");

    var validationCalls = 0;
    try
    {
        LadderProjectFileStore.SaveValidated(original, "valid-new", _ =>
        {
            if (++validationCalls == 2) throw new InvalidDataException("Reject reread temporary content.");
        }, true);
        throw new Exception("Expected reread rejection.");
    }
    catch (InvalidDataException) { }
    Check(validationCalls == 2 && File.ReadAllText(original) == "keep-original", "Failure after temporary flush must preserve original.");
    Check(!Directory.EnumerateFiles(root, "*.tmp").Any(), "Interrupted validation must clean temporary bytes.");

    using (var held = new FileStream(original, FileMode.Open, FileAccess.Read, FileShare.None))
    {
        try { LadderProjectFileStore.SaveValidated(original, "valid-new", Validate, true); throw new Exception("Expected locked backup failure."); }
        catch (IOException) { }
    }
    Check(File.ReadAllText(original) == "keep-original", "Failed migration backup must preserve original bytes.");
    Check(!Directory.EnumerateFiles(root, "*.tmp").Any(), "Failed backup must clean temporary bytes.");

    var saveAs = Path.Combine(root, "new.rpproj.json");
    result = LadderProjectFileStore.SaveValidated(saveAs, "valid-new", Validate, true);
    Check(result.BackupPath is null && File.ReadAllText(saveAs) == "valid-new", "New file must save without fictional backup.");
    Check(File.ReadAllText(original) == "keep-original", "Save As must leave original intact.");
    Console.WriteLine($"PROJECT_STORAGE_TESTS_PASS checks={checks}");
}
finally
{
    // Only this test's uniquely created temporary directory is removed.
    Directory.Delete(root, recursive: true);
}
