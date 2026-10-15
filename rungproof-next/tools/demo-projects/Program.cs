using System;
using System.IO;
using System.Text;
using RungProof.Next.VirtualController;

// Run from rungproof-next. Default is read-only; --write explicitly regenerates
// the five fixed paths from the same documents used by the native Demo menu.
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 1 || args.Length == 1 && args[0] is not ("--check" or "--write"))
        {
            Console.Error.WriteLine("Usage: dotnet run --project tools/demo-projects/RungProof.DemoProjects.csproj -- [--check|--write]");
            return 2;
        }
        var root = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(root, "RungProof.Next.csproj")))
        {
            Console.Error.WriteLine("Run from the rungproof-next project root.");
            return 2;
        }
        var write = args.Length == 1 && args[0] == "--write";
        var failed = false;
        foreach (var (sceneId, relativePath) in AuthoredDemoLadderPrograms.ProjectFiles)
        {
            if (!AuthoredDemoLadderPrograms.TryCreate(sceneId, out var document)
                || !LadderCompiler.Compile(document.BuildProgram()).IsValid)
                throw new InvalidOperationException($"Authored demo is invalid: {sceneId}");
            var expected = LadderEditorProjectJson.Save(document);
            var path = Path.Combine(root, relativePath);
            if (write) File.WriteAllText(path, expected + "\n", new UTF8Encoding(false));
            var loaded = File.Exists(path) ? LadderEditorProjectJson.Load(File.ReadAllText(path)) : null;
            var matches = loaded?.IsReadable == true && loaded.Document is not null
                && LadderEditorProjectJson.Save(loaded.Document) == expected;
            Console.WriteLine($"DEMO_PROJECT {(matches ? "PASS" : "FAIL")} {relativePath} scene={sceneId}");
            failed |= !matches;
        }
        if (failed) Console.Error.WriteLine("Saved demo projects differ from the Demo menu. Regenerate with --write.");
        return failed ? 1 : 0;
    }
}
