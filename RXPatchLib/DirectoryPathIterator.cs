using System;
using System.Collections.Generic;
using System.IO;

namespace RXPatchLib
{
    enum InclusionResult
    {
        Include,
        ExcludeEntirely,
        ExcludePartially, // Continue searching for possibly included files deeper in the recursion. (e.g. when .../a/ is excluded, but .../a/b is included.
    }
    class DirectoryPathIterator
    {
        private static IEnumerable<string> GetChildPathsRecursive(DirectoryInfo parentDir, Func<string, InclusionResult> inclusionFilter, string prefix)
        {
            foreach (var childDir in parentDir.GetDirectories())
            {
                if (inclusionFilter(prefix + childDir.Name + Path.DirectorySeparatorChar) != InclusionResult.ExcludeEntirely)
                    foreach (var path in GetChildPathsRecursive(childDir, inclusionFilter, prefix + childDir.Name + Path.DirectorySeparatorChar))
                        yield return path;
            }

            foreach (var file in parentDir.GetFiles())
            {
                if (inclusionFilter(prefix + file.Name) == InclusionResult.Include)
                    yield return prefix + file.Name;
            }
        }
        public static IEnumerable<string> GetChildPathsRecursive(string parentPath, Func<string, InclusionResult> inclusionFilter)
        {
            return GetChildPathsRecursive(new DirectoryInfo(parentPath), inclusionFilter, "");
        }
        public static IEnumerable<string> GetChildPathsRecursive(string parentPath)
        {
            return GetChildPathsRecursive(new DirectoryInfo(parentPath), x => InclusionResult.Include, "");
        }
    }
}
