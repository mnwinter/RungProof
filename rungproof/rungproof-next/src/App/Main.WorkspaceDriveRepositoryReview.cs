using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.Workspace;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifyWorkspaceDriveRepositoryOnly;
    // Native repository test: exercise the production save/load path, not a
    // test serializer that bypasses its version and endpoint validation.
    private void VerifyWorkspaceDriveRepository()
    {
        var source = SceneCatalogLoader.LoadScene(_sceneCatalog!.Scenes.Single(
            scene => scene.Id == WorkspaceDriveConnectionPolicy.SourceId));
        const string instance = "placement-001-learner-belt";
        var relation = WorkspaceDriveConnectionPolicy.CreateRelation(source, instance);
        var document = new WorkspaceDocument(9, source.Id,
            [new(instance, WorkspaceDriveConnectionPolicy.BeltAsset,
                [0, 0, 3], [0, 0, 0], [1, 1, 1])],
            SignalLinks: [new("added_run", instance, "run_command", relation.RunPoint),
                new("added_running", instance, "running", relation.RunningPoint),
                new("added_speed", instance, "actual_speed", relation.ActualSpeedPoint)],
            ConnectorLinks: [], Groups: [], DriveConnection: relation);
        var directory = ProjectSettings.GlobalizePath(
            $"res://.tools/workspace-drive-repository-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "drive.workspace.json");
        WorkspaceRepository.Save(document, path);
        var loaded = WorkspaceRepository.Load(_candidateCatalog!, _sceneCatalog, path);
        if (JsonSerializer.Serialize(document) != JsonSerializer.Serialize(loaded))
            throw new InvalidOperationException("Drive repository changed the exact saved document.");
        var effective = WorkspaceDriveConnectionPolicy.AugmentSource(source, loaded, _candidateCatalog!);
        if (effective.Simulation.GetProperty("points").GetArrayLength()
            != source.Simulation.GetProperty("points").GetArrayLength() + 3)
            throw new InvalidOperationException("Drive repository lost its derived I/O declarations.");
        GD.Print("WORKSPACE_DRIVE_REPOSITORY_CHECK version9_exact_save_load_derived_endpoint_validation=True");

        var detached = document with { SignalLinks = document.SignalLinks!
            .Where(link => link.SignalId != "actual_speed").ToArray() };
        WorkspaceRepository.Save(detached, path);
        loaded = WorkspaceRepository.Load(_candidateCatalog!, _sceneCatalog, path);
        if (WorkspaceDriveConnectionPolicy.Eligibility(loaded).CanRun)
            throw new InvalidOperationException("Detached feedback was silently repaired or made runnable.");
        GD.Print("WORKSPACE_DRIVE_REPOSITORY_CHECK detached_feedback_readable_not_runnable=True");

        Reject(document with { Version = 8 });
        Reject(document with { DriveConnection = null });
        var stale = document with { DriveConnection = relation with { SourceFingerprint = new string('0', 64) } };
        try
        {
            WorkspaceRepository.ValidateDocument(stale, _candidateCatalog!, _sceneCatalog);
            throw new InvalidOperationException("Drive repository accepted a stale source identity.");
        }
        catch (InvalidOperationException error) when (error.Message.Contains("fingerprint", StringComparison.Ordinal)) { }
        GD.Print("WORKSPACE_DRIVE_REPOSITORY_CHECK version_relation_and_stale_source_rejected=True");

        var draft = new LadderEditorDocument();
        draft.ResetProject("learner-drive-repository", "Drive_Main", TimeSpan.FromMilliseconds(40));
        draft.SourceSceneId = source.Id;
        draft.AddTag("saved_enable", PlcVariableRole.Memory, initialValue: true);
        draft.AddTag("added_run", PlcVariableRole.Output, relation.RunPoint);
        draft.WatchVariables.Add("saved_enable");
        draft.AddRung("Learner authored enable", "added_run");
        draft.AddContact(0, 0, "saved_enable", false);
        SaveJoint("valid");
        draft.AddRung("Readable unresolved output", "missing_current_output");
        SaveJoint("invalid");

        // A migrated inner ladder must preserve the original outer bundle bytes.
        // Build a self-contained v1 fixture so the exported verifier does not
        // depend on development-only test resources being packaged.
        var legacyNode = System.Text.Json.Nodes.JsonNode.Parse(LadderEditorProjectJson.Save(draft))!;
        legacyNode["schemaVersion"] = 1;
        var legacyRungs = legacyNode["blocks"]![0]!["rungs"]!.AsArray();
        for (var index = 0; index < legacyRungs.Count; index++)
        {
            var legacyRung = legacyRungs[index]!.AsObject();
            legacyRung.Remove("root");
            legacyRung["branches"] = System.Text.Json.Nodes.JsonNode.Parse(index == 0
                ? "[{\"id\":\"migration-branch-1\",\"contacts\":[{\"id\":\"migration-contact-1\",\"variable\":\"saved_enable\",\"normallyClosed\":false,\"compareOperator\":\"Equal\",\"edgeMode\":\"None\"}]}]"
                : "[{\"id\":\"migration-branch-2\",\"contacts\":[]}]");
        }
        var legacyJson = legacyNode.ToJsonString();
        var migrationPath = Path.Combine(directory, "migration.integrated.json");
        var legacyBundle = new WorkspaceProjectBundle(1, WorkspaceProjectBundleRepository.Kind, document, legacyJson);
        WorkspaceProjectBundleRepository.Save(legacyBundle, migrationPath, _candidateCatalog!, _sceneCatalog);
        var legacyBytes = File.ReadAllBytes(migrationPath);
        var migratedDraft = LadderEditorProjectJson.Load(legacyJson).Document!;
        WorkspaceProjectBundleRepository.Save(legacyBundle with { LadderProjectJson = LadderEditorProjectJson.Save(migratedDraft) }, migrationPath, _candidateCatalog!, _sceneCatalog);
        var backups = Directory.GetFiles(directory, "migration.integrated.json.before-migration.*.bak");
        if(backups.Length != 1 || !File.ReadAllBytes(backups[0]).SequenceEqual(legacyBytes))
            throw new InvalidOperationException("Integrated migration did not preserve original bundle bytes.");
        var future = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(migrationPath))!;
        future["version"] = 999;
        File.WriteAllText(migrationPath, future.ToJsonString());
        var futureBytes = File.ReadAllBytes(migrationPath);
        var futureRejected = false;
        try { WorkspaceProjectBundleRepository.Save(legacyBundle, migrationPath, _candidateCatalog!, _sceneCatalog); }
        catch (InvalidOperationException) { futureRejected = true; }
        if(!futureRejected || !File.ReadAllBytes(migrationPath).SequenceEqual(futureBytes))
            throw new InvalidOperationException("Future bundle was overwritten.");
        GD.Print("WORKSPACE_DRIVE_REPOSITORY_CHECK migration_backup_future_preservation=True");

        // The runtime now supports v9. Reopen through the real joint path and
        // prove exact relation/draft preservation with execution stopped.
        MarkWorkspaceSaved();
        var validPath = Path.Combine(directory, "valid.integrated.json");
        var expected = WorkspaceProjectBundleRepository.LoadIfBundle(validPath, _candidateCatalog!, _sceneCatalog!)!;
        LoadWorkspaceFromPath(validPath);
        if (_currentSceneId != source.Id || _workspaceDriveRelation != relation
            || _simulatorShell!.CaptureIntegratedLadderDraft() != expected.LadderProjectJson
            || JsonSerializer.Serialize(CreateWorkspaceDocument()) != JsonSerializer.Serialize(document)
            || _virtualController is not null && _virtualController.Snapshot.State != VirtualControllerState.Stopped)
            throw new InvalidOperationException("Drive runtime joint open changed saved work or started execution.");
        GD.Print("WORKSPACE_DRIVE_REPOSITORY_CHECK actual_runtime_joint_open_preserves_relation_draft_stopped=True");
        void SaveJoint(string mode)
        {
            var json = LadderEditorProjectJson.Save(draft);
            var jointPath = Path.Combine(directory, mode + ".integrated.json");
            WorkspaceProjectBundleRepository.Save(new(1, WorkspaceProjectBundleRepository.Kind,
                document, json), jointPath, _candidateCatalog!, _sceneCatalog);
            var joint = WorkspaceProjectBundleRepository.LoadIfBundle(jointPath, _candidateCatalog!, _sceneCatalog)
                ?? throw new InvalidOperationException("Saved drive project was not recognized as integrated.");
            if (joint.LadderProjectJson != json
                || JsonSerializer.Serialize(joint.Workspace) != JsonSerializer.Serialize(document))
                throw new InvalidOperationException("Joint repository replaced the learner draft or drive relation.");
            GD.Print($"WORKSPACE_DRIVE_REPOSITORY_CHECK {mode}_joint_exact_draft_layout_watch_cadence_preserved=True");
        }

        void Reject(WorkspaceDocument invalid)
        {
            var rejected = false;
            try { WorkspaceRepository.Save(invalid, path); }
            catch (InvalidOperationException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException("Invalid drive relation/version was saved.");
        }
        // Keep the unique evidence directory; no caller's project is overwritten.
    }
}


