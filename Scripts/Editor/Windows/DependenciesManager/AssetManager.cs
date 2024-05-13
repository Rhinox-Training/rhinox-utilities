using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public class AssetManager
    {
#pragma warning disable CS0618 // Type or member is obsolete
        [ListDrawerSettings(Expanded = true, NumberOfItemsPerPage = 25, IsReadOnly = true), ReadOnly]
#pragma warning restore CS0618 // Type or member is obsolete
        public string[] AllAssets = new string[0];

        public string SearchText { get; private set; }

        private void RecalculateAssets()
        {
            AllAssets = GetAssetsPaths(SearchText);
        }

        public string[] GetAssetsPaths(string filter)
        {
            // filter with : makes you lose focus
            if (filter.EndsWith(":"))
                return AllAssets;

            return AssetDatabase.FindAssets(filter)
                .Distinct()
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => !Directory.Exists(path))
                .ToArray();

        }

        public bool CheckChange(string newTerm)
        {
            var change = SearchText != newTerm;
            SearchText = newTerm;

            if (change)
                RecalculateAssets();

            return change;
        }

        public DependencyAsset[] GetIntersecting(IEnumerable<DependencyAsset> dependencies, Regex[] filesToIgnore,
            Regex[] directoriesToIgnore)
        {
            return dependencies
                .Where(x => !DependencySettings.IsAnyMatch(x.Path, filesToIgnore, directoriesToIgnore))
                .Where(x => AllAssets.Any(path => x.Path == path))
                .OfType<DependencyAsset>().ToArray();
        }

        public DependencyAsset[] InverseOf(IEnumerable<DependencyAsset> dependencies, Regex[] filesToIgnore,
            Regex[] directoriesToIgnore)
        {
            return InverseOf(dependencies.Select(x => x.Path).ToArray(), filesToIgnore, directoriesToIgnore);
        }

        public DependencyAsset[] InverseOf(IReadOnlyList<string> paths, Regex[] filesToIgnore,
            Regex[] directoriesToIgnore)
        {
            return AllAssets
                .Where(path => !paths.Contains(path))
                .Where(path => !DependencySettings.IsAnyMatch(path, filesToIgnore, directoriesToIgnore))
                .Select(path => DependencyAsset.Create(path))
                .ToArray();
        }
    }
}