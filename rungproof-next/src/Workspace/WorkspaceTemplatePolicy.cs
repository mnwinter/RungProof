using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RungProof.Next.Scenes;

namespace RungProof.Next.Workspace;

public static class WorkspaceTemplatePolicy
{
    public const string SelectionId="template:ControlInstallation";
    public static bool Supports(string sceneId)=>sceneId is "lab-2-01-workstation-call" or "lab-4-01-press-count-lamp" or "lab-5-01-delayed-lamp";
    public static string Fingerprint(SceneDefinition scene)=>Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(scene with { TemplateOverride=null }))));
    public static void ValidateShape(string sceneId,WorkspaceTemplateOverride value)
    {
        if(!Supports(sceneId)||value.Version!=1||value.TargetId!="ControlInstallation")
            throw new InvalidOperationException("This template target is not editable in the current slice.");
        if(value.Position is not { Length:3 }||value.Position.Any(p=>!double.IsFinite(p))||!double.IsFinite(value.YawDegrees))
            throw new InvalidOperationException("Template transform must contain finite values.");
        if(value.Position[1]!=0||value.Position.Any(p=>Math.Abs(p)>10)||Math.Abs(value.YawDegrees)>360)
            throw new InvalidOperationException("Operator assembly must stay grounded within the bounded training workspace (10m, yaw ±360°).");
        if(string.IsNullOrEmpty(value.SourceFingerprint)||value.SourceFingerprint.Length!=64||value.SourceFingerprint.Any(c=>!Uri.IsHexDigit(c)))
            throw new InvalidOperationException("Template source fingerprint is invalid.");
    }
    public static void Validate(SceneDefinition scene,WorkspaceTemplateOverride value)
    {
        ValidateShape(scene.Id,value);
        if(!string.Equals(Fingerprint(scene),value.SourceFingerprint,StringComparison.Ordinal))
            throw new InvalidOperationException("Template source has changed; reopen the original scene before editing this workspace.");
    }
}
