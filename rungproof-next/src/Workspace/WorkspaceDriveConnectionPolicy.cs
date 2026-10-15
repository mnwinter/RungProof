using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RungProof.Next.Catalog;
using RungProof.Next.Scenes;

namespace RungProof.Next.Workspace;

public static class WorkspaceDriveConnectionPolicy
{
    public const string SourceId="scene-1-conveyor-stop";
    public const string BeltAsset="material-handling.belt-conveyor.600x6000.v1";
    public static string NormalizeInstanceId(string instanceId)
    {
        if(string.IsNullOrWhiteSpace(instanceId))throw new InvalidOperationException("Drive instance ID is blank.");
        var value=new string(instanceId.Select(c=>c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_'?c:'_').ToArray());
        return value[0] is >= '0' and <= '9'?"belt_"+value:value;
    }
    public static string Fingerprint(SceneDefinition source)
    {
        // Existing scenes may omit Training/Verification (Undefined JsonElement).
        // Include their absence explicitly, rather than trying to serialize Undefined.
        JsonElement? Defined(JsonElement element)=>element.ValueKind==JsonValueKind.Undefined?null:element;
        var content=JsonSerializer.Serialize(new{source.Version,source.Id,source.Name,source.Description,source.Camera,source.Equipment,
            Simulation=Defined(source.Simulation),Training=Defined(source.Training),Verification=Defined(source.Verification),source.Migration,source.PlcTestProfile,source.TemplateOverride});
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
    public static WorkspaceDriveConnection CreateRelation(SceneDefinition source,string instanceId)
    {
        if(source.Id!=SourceId)throw new InvalidOperationException("Drive connection requires its existing source scene.");
        var prefix=NormalizeInstanceId(instanceId);
        return new(1,instanceId,Fingerprint(source),prefix+"_run",prefix+"_running",prefix+"_actual_speed_mps");
    }
    public static void ValidateShape(WorkspaceDocument document)
    {
        var relation=document.DriveConnection;
        if(relation is null)
        {if(document.Version==9)throw new InvalidOperationException("Workspace version 9 requires a drive connection.");return;}
        if(document.Version!=9||document.SourceSceneId!=SourceId||relation.Version!=1
            ||document.TemplateOverride is not null||document.FiniteBeltPreset is not null||document.FiniteRollerPreset is not null)
            throw new InvalidOperationException("Unsupported drive connection version/source or conflicting preset relation.");
        var prefix=NormalizeInstanceId(relation.InstanceId);
        if(relation.RunPoint!=prefix+"_run"||relation.RunningPoint!=prefix+"_running"||relation.ActualSpeedPoint!=prefix+"_actual_speed_mps")
            throw new InvalidOperationException("Drive connection point names must match its normalized instance ID.");
        if(relation.SourceFingerprint is not {Length:64}||relation.SourceFingerprint.Any(c=>!Uri.IsHexDigit(c)))
            throw new InvalidOperationException("Drive source fingerprint is malformed.");
        if(document.Placements.Select(p=>p.InstanceId).Distinct(StringComparer.Ordinal).Count()!=document.Placements.Count)
            throw new InvalidOperationException("Duplicate workspace instance IDs.");
        var target=document.Placements.SingleOrDefault(p=>p.InstanceId==relation.InstanceId)
            ??throw new InvalidOperationException("Drive connection placement is missing.");
        if(target.AssetId!=BeltAsset)throw new InvalidOperationException("Drive connection supports only the existing belt asset.");
        if(target.Position is not {Length:3}||target.Rotation is not {Length:3}||target.Scale is not {Length:3}
            ||target.Position.Concat(target.Rotation).Concat(target.Scale).Any(v=>!double.IsFinite(v))
            ||target.Rotation.Any(v=>v!=0)||target.Scale.Any(v=>v!=1))
            throw new InvalidOperationException("Drive connection requires finite position, zero rotation and unit scale.");
        if(document.Placements.Any(p=>p.InstanceId!=relation.InstanceId&&NormalizeInstanceId(p.InstanceId)==prefix))
            throw new InvalidOperationException("Normalized drive instance ID collides with another placement.");
        var canonical=CanonicalSignals(relation);var links=document.SignalLinks??Array.Empty<WorkspaceSignalLink>();
        if(links.Select(l=>l.Id).Any(string.IsNullOrWhiteSpace)||links.Select(l=>l.Id).Distinct(StringComparer.Ordinal).Count()!=links.Count)
            throw new InvalidOperationException("Signal mapping IDs must be nonblank and unique.");
        foreach(var link in links)
        {
            if(link.InstanceId==relation.InstanceId&&(!canonical.TryGetValue(link.SignalId,out var point)||link.PointName!=point))
                throw new InvalidOperationException("Opted-in drive accepts only its exact canonical signal mappings.");
            if(link.InstanceId!=relation.InstanceId&&canonical.Values.Contains(link.PointName,StringComparer.Ordinal))
                throw new InvalidOperationException("Another asset cannot claim drive-owned points.");
        }
        if(links.Where(l=>l.InstanceId==relation.InstanceId).GroupBy(l=>l.SignalId,StringComparer.Ordinal).Any(g=>g.Count()>1))
            throw new InvalidOperationException("Duplicate drive signal mappings.");
        // Missing canonical links are readable detached engineering work. Eligibility
        // exposes completeness separately; loading a project never repairs its maps.
    }
    public static void Validate(WorkspaceDocument document,SceneDefinition source,AssetCatalogDocument assets)
    {
        ValidateShape(document);if(document.DriveConnection is not { } relation)return;
        if(source.Id!=SourceId||source.Id!=document.SourceSceneId||source.TemplateOverride is not null
            ||Fingerprint(source)!=relation.SourceFingerprint)
            throw new InvalidOperationException("Drive source scene/fingerprint differs; reopen the original source.");
        var asset=assets.Assets.SingleOrDefault(a=>a.Id==BeltAsset)??throw new InvalidOperationException("Drive belt asset is unavailable.");
        RequireSignal("run_command","bool","input",null);RequireSignal("running","bool","output",null);RequireSignal("actual_speed","float32","output","m/s");
        if(source.Simulation.ValueKind!=JsonValueKind.Object||!source.Simulation.TryGetProperty("points",out var points)||points.ValueKind!=JsonValueKind.Array)
            throw new InvalidOperationException("Drive source must declare its existing scene points.");
        var declared=points.EnumerateArray().Select(p=>p.GetProperty("name").GetString()).ToArray();
        if(declared.Distinct(StringComparer.Ordinal).Count()!=declared.Length||CanonicalSignals(relation).Values.Any(p=>declared.Contains(p,StringComparer.Ordinal)))
            throw new InvalidOperationException("Drive point declarations collide with existing source points.");
        void RequireSignal(string id,string type,string direction,string? unit)
        {
            var signals=asset.Signals.Where(s=>s.Id==id).ToArray();
            if(signals.Length!=1||signals[0].DataType!=type||signals[0].Direction!=direction||signals[0].Unit!=unit)
                throw new InvalidOperationException("Drive asset signal type/direction/unit differs: "+id);
        }
    }
    public static WorkspaceDriveConnectionEligibility Eligibility(WorkspaceDocument document)
    {
        ValidateShape(document);if(document.DriveConnection is not { } r)return new(false,false);
        var links=document.SignalLinks??Array.Empty<WorkspaceSignalLink>();
        bool Mapped(string signal,string point)=>links.Any(l=>l.InstanceId==r.InstanceId&&l.SignalId==signal&&l.PointName==point);
        return new(Mapped("run_command",r.RunPoint),Mapped("running",r.RunningPoint)&&Mapped("actual_speed",r.ActualSpeedPoint));
    }
    public static SceneDefinition AugmentSource(SceneDefinition source,WorkspaceDocument document,AssetCatalogDocument assets)
    {
        Validate(document,source,assets);if(document.DriveConnection is not { } r)return source;
        using var buffer=new MemoryStream();
        using(var writer=new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach(var property in source.Simulation.EnumerateObject())
            {
                writer.WritePropertyName(property.Name);
                if(property.Name!="points"){writer.WriteRawValue(property.Value.GetRawText());continue;}
                writer.WriteStartArray();foreach(var point in property.Value.EnumerateArray())writer.WriteRawValue(point.GetRawText());
                writer.WriteRawValue(JsonSerializer.Serialize(new{name=r.RunPoint,type="BOOL",owner="PLC",initial=false,role="output",purpose="Added belt effective run command."}));
                writer.WriteRawValue(JsonSerializer.Serialize(new{name=r.RunningPoint,type="BOOL",owner="PC",initial=false,role="input",purpose="Added belt measured speed exceeds 0.01 m/s."}));
                writer.WriteRawValue(JsonSerializer.Serialize(new{name=r.ActualSpeedPoint,type="REAL",owner="PC",initial=0d,role="input",unit="m/s",purpose="Added belt measured surface speed."}));
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
        // The original Scene1 includes an external-profile reference. The new
        // derived drive I/O has no external address contract; keep this scope offline.
        using var json=JsonDocument.Parse(buffer.ToArray());return source with{Simulation=json.RootElement.Clone(),PlcTestProfile=null};
    }
    private static Dictionary<string,string> CanonicalSignals(WorkspaceDriveConnection r)=>new(StringComparer.Ordinal)
    {{"run_command",r.RunPoint},{"running",r.RunningPoint},{"actual_speed",r.ActualSpeedPoint}};
}
