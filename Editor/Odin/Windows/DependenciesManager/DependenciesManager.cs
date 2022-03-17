using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public class DependenciesManager
    {
        [ListDrawerSettings(Expanded = true, NumberOfItemsPerPage = 25, IsReadOnly = true)]
        public readonly List<Dependency> Dependencies = new List<Dependency>();

        public void FindDependencies(IEnumerable<Object> objects, Regex[] filesToIgnore = null,
            Regex[] directoriesToIgnore = null)
        {
            Dependencies.Clear();

            foreach (var o in objects)
            {
                var path = AssetDatabase.GetAssetPath(o);

                if (Directory.Exists(path))
                {
                    foreach (var itemPath in GetAtPath(path))
                    {
                        foreach (var dependencyPath in AssetDatabase.GetDependencies(itemPath))
                        {
                            // Returns itself for some reason?
                            if (itemPath == dependencyPath) continue;
                            
                            if (DependencySettings.IsAnyMatch(dependencyPath, filesToIgnore, directoriesToIgnore))
                                continue;
                            AddDependency(dependencyPath, AssetDatabase.LoadAssetAtPath<Object>(itemPath));
                        }
                    }
                }
                else
                {
                    foreach (var dependencyPath in AssetDatabase.GetDependencies(path))
                    {
                        // Returns itself for some reason?
                        if (path == dependencyPath) continue;
                        
                        if (DependencySettings.IsAnyMatch(dependencyPath, filesToIgnore, directoriesToIgnore))
                            continue;
                        AddDependency(dependencyPath, o);
                    }
                }
            }

            Dependencies.Sort((x, y) => x.Path.CompareTo(y.Path));
        }

        // public void FindDependant(ICollection<Object> objects)
        // {
        //     Dependencies.Clear();
        // 	
        // 	
        // }

        private Dependency AddDependency(Object o, params Object[] users)
        {
            if (o == null)
                return null;

            var path = AssetDatabase.GetAssetPath(o);

            return AddDependency(path, users);
        }

        private Dependency AddDependency(string path, params Object[] users)
        {
            var d = Dependencies.FirstOrDefault(x => x.Path == path);

            if (d == null)
            {
                d = new Dependency(path);
                Dependencies.Add(d);
            }

            foreach (var user in users)
                d.Users.AddUnique(user);

            return d;
        }

        private static string GetFullAssetPath(string path)
        {
            return Path.Combine(Application.dataPath, "..", path);
        }

        public static List<string> GetAtPath(string path)
        {
            var paths = new List<string>();
            string[] fileEntries = Directory.GetFiles(GetFullAssetPath(path));

            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path;

            foreach (string file in fileEntries)
            {
                string filename = Path.GetFileName(file);
                string localPath = Path.Combine(path, filename);

                if (!string.IsNullOrWhiteSpace(localPath))
                    paths.Add(localPath);
            }

            return paths;
        }
    }
}