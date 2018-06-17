using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
/*
 * 
Time Elapsed 00:00:01.10
Could not find a part of the path 'E:\projects\nodatime\nodatime\.tmp\docfx\obj\1.1.x\api\changes.md'.
   at System.IO.FileStream.OpenHandle(FileMode mode, FileShare share, FileOptions options)
   at System.IO.FileStream..ctor(String path, FileMode mode, FileAccess access, FileShare share, Int32 bufferSize, FileOptions options)
   at System.IO.StreamWriter..ctor(String path, Boolean append, Encoding encoding, Int32 bufferSize)
   at System.IO.StreamWriter..ctor(String path, Boolean append)
   at System.IO.File.CreateText(String path)
   at ReleaseDiffGenerator.GenerateReleaseDiff(String oldReleaseDirectory, String newReleaseDirectory) in E:\projects\nodatime\nodatime\build\Nuke\ReleaseDiffGenerator.cs:line 48
   at Build.<get_BuildApiDocs>b__19_1() in E:\projects\nodatime\nodatime\build\Nuke\Build.cs:line 146
   at System.Collections.Generic.List`1.ForEach(Action`1 action)
   at Nuke.Common.Execution.BuildExecutor.Execute(IEnumerable`1 executionList)
   at Nuke.Common.Execution.BuildExecutor.Execute[T](Expression`1 defaultTargetExpression)

Repeating warnings and errors:
Could not find a part of the path 'E:\projects\nodatime\nodatime\.tmp\docfx\obj\1.1.x\api\changes.md'.
 */
class ReleaseDiffGenerator
{
    public static int GenerateReleaseDiff(string oldReleaseDirectory, string newReleaseDirectory)
    {
        var oldRelease = Release.Load(oldReleaseDirectory, Path.GetFileName(oldReleaseDirectory));
        var newRelease = Release.Load(newReleaseDirectory, Path.GetFileName(newReleaseDirectory));

        var oldMemberUids = new HashSet<string>(oldRelease.MembersByUid.Keys);
        var newMemberUids = new HashSet<string>(newRelease.MembersByUid.Keys);

        var addedMembers = newMemberUids
            .Except(oldMemberUids)
            .OrderBy(uid => uid)
            .Select(uid => newRelease.MembersByUid[uid])
            // Don't include members where the parent is also new
            .Where(m => m.Parent == null || oldMemberUids.Contains(m.Parent))
            .ToList();
        var removedMembers = oldMemberUids
            .Except(newMemberUids)
            .OrderBy(uid => uid)
            .Select(uid => oldRelease.MembersByUid[uid])
            // Don't include members where the parent was also removed
            .Where(m => m.Parent == null || newMemberUids.Contains(m.Parent))
            .ToList();

        var newObsoleteGuids = newRelease
            .Members
            .Where(m => m.Obsolete)
            .Select(m => m.Uid);
        var oldObsoleteGuids = oldRelease
            .Members
            .Where(m => m.Obsolete)
            .Select(m => m.Uid);
        var newlyObsoleteMembers = newObsoleteGuids
            .Except(oldObsoleteGuids)
            .OrderBy(uid => uid)
            .Select(uid => newRelease.MembersByUid[uid])
            .ToList();

        // TODO:
        // - Linking of removed items (can't be a normal link, as it has to be to previous version)
        using (var writer = File.CreateText(Path.Combine(newReleaseDirectory, "api", "changes.md")))
        {
            writer.WriteLine($"# API changes from {oldRelease.Version} to {newRelease.Version}");

            WriteChanges(writer, addedMembers, "New", true, "(obsolete)");
            WriteChanges(writer, removedMembers, "Removed", false, "");
            WriteChanges(writer, newlyObsoleteMembers, "Newly obsolete", true, ""); // No need to put "(obsolete)" on everything...
        }

        var tocFile = Path.Combine(newReleaseDirectory, "api", "toc.yml");
        var toc = File.ReadAllLines(tocFile).ToList();
        if (!toc[1].StartsWith("- name: Changes"))
        {
            toc.Insert(1, $"- name: Changes from {oldRelease.Version}");
            toc.Insert(2, "  href: changes.md");
        }
        File.WriteAllLines(tocFile, toc);

        return 0;
    }

    static void WriteChanges(TextWriter writer, IEnumerable<DocfxMember> members, string label, bool link, string obsoleteSuffix)
    {
        // Types and namespaces, individually
        WriteTypes(writer, $"{label} namespaces", members, DocfxMember.TypeKind.Namespace, link, obsoleteSuffix);
        WriteTypes(writer, $"{label} classes", members, DocfxMember.TypeKind.Class, link, obsoleteSuffix);
        WriteTypes(writer, $"{label} structs", members, DocfxMember.TypeKind.Struct, link, obsoleteSuffix);
        WriteTypes(writer, $"{label} interfaces", members, DocfxMember.TypeKind.Interface, link, obsoleteSuffix);
        WriteTypes(writer, $"{label} delegates", members, DocfxMember.TypeKind.Delegate, link, obsoleteSuffix);
        WriteTypes(writer, $"{label} enums", members, DocfxMember.TypeKind.Enum, link, obsoleteSuffix);

        // Now members of types (where the whole type isn't new/removed)
        var membersByType = members
            .Where(m => m.IsTypeMember)
            .GroupBy(m => m.ParentMember)
            .OrderBy(g => g.Key.Uid);

        if (membersByType.Any())
        {
            writer.WriteLine();
            writer.WriteLine($"## {label} type members, by type");
            foreach (var group in membersByType)
            {
                writer.WriteLine();
                var type = group.Key;
                string typeMd = link ? $"[`{type.DisplayName}`](xref:{WebUtility.UrlEncode(type.Uid)})"
                    : $"`{type.DisplayName}`";
                writer.WriteLine($"### {label} members in {typeMd}");
                writer.WriteLine();
                foreach (var member in group)
                {
                    WriteBullet(writer, member, link, obsoleteSuffix);
                }
            }
        }
    }

    static void WriteTypes(TextWriter writer, string title, IEnumerable<DocfxMember> members,
        DocfxMember.TypeKind kind, bool link, string obsoleteSuffix)
    {
        var kindMembers = members.Where(m => m.Type == kind);
        if (!kindMembers.Any())
        {
            return;
        }
        writer.WriteLine();
        writer.WriteLine($"## {title}");
        writer.WriteLine();
        foreach (var member in kindMembers)
        {
            // We generate the "(obsolete)" part even for new members... unlikely, but possible.
            // (We haven't made IsoDayOfWeekExtensions.ToIsoDayOfWeek obsolete in 2.0.x, but
            // it's possible...)
            WriteBullet(writer, member, link, obsoleteSuffix);
        }
    }

    static void WriteBullet(TextWriter writer, DocfxMember member, bool link, string obsoleteSuffix)
    {
        string obsolete = member.Obsolete ? $" {obsoleteSuffix}" : "";
        if (link)
        {
            writer.WriteLine($"- [`{member.DisplayName}`](xref:{WebUtility.UrlEncode(member.Uid)}){obsolete}");
        }
        else
        {
            writer.WriteLine($"- `{member.DisplayName}`{obsolete}");
        }
    }
}