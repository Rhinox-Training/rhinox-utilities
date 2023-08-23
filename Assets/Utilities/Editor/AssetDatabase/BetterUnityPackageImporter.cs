#if SHARPZIPLIB
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using UnityEditor;

namespace Rhinox.Utilities.Editor
{
    public class UnityPackageResponse
    {
        public string[] ImportedAssets;
        public int PackageSize;
        public int ImportedAssetCount => ImportedAssets?.Length ?? 0;
    }
    
    public static class BetterUnityPackageImporter
    {
        private const string UNITYPACKAGE_EXTENSION = ".unitypackage";
        private const string DEFAULT_ASSET_ROOT = "Assets/";

        public enum ImportMode
        {
            Root,
            CommonParent,
            Flatten
        }
        
        public enum OverwriteMode
        {
            Dont,
            OverwriteKeep,
            OverwriteReplace
        }
        
        // Public API
        public static bool TryListPackageContent(string packagePath, out ICollection<string> contents, string destinationPrefix = DEFAULT_ASSET_ROOT, ImportMode mode = ImportMode.Root)
        {
            if (!IsValidUnityPackage(packagePath))
            {
                contents = Array.Empty<string>();
                return false;
            }
            
            try
            {
                ICollection<string> result = Array.Empty<string>();
                using (var reader = new UnityPackageReader(packagePath))
                {
                    var catalog = reader.LoadCatalog(destinationPrefix, mode);
                    result = catalog.Select(x => x.AssetPath).ToArray();
                }

                contents = result;
                return true;
            }
            catch (Exception e)
            {
                PLog.Error($"ERROR with importing '{packagePath}': {e.ToString()}");
                contents = Array.Empty<string>();
                return false;
            }
        }
        
        public static ICollection<string> ListPackageContent(string packagePath, string destinationPrefix = DEFAULT_ASSET_ROOT, ImportMode mode = ImportMode.Root)
        {
            if (!IsValidUnityPackage(packagePath))
                return Array.Empty<string>();

            try
            {
                ICollection<string> result = Array.Empty<string>();
                using (var reader = new UnityPackageReader(packagePath))
                {
                    var catalog = reader.LoadCatalog(destinationPrefix, mode);
                    result = catalog.Select(x => x.AssetPath).ToArray();
                }

                return result;
            }
            catch (Exception e)
            {
                PLog.Error($"ERROR with importing '{packagePath}': {e.ToString()}");
                return Array.Empty<string>();
            }
        }

        public static bool ImportPackage(string packagePath, string destinationPrefix = "Assets/", ImportMode mode = ImportMode.Root, OverwriteMode overwrite = OverwriteMode.Dont, Action<UnityPackageResponse> response = null)
        {
            if (!IsValidUnityPackage(packagePath))
                return false;

            try
            {
                AssetDatabase.StartAssetEditing();
                ICollection<UnityPackageReader.AssetEntry> catalog = Array.Empty<UnityPackageReader.AssetEntry>();
                using (var reader = new UnityPackageReader(packagePath))
                {
                    catalog = reader.LoadCatalog(destinationPrefix, mode);
                }

                var packageResponse = new UnityPackageResponse()
                {
                    PackageSize = catalog.Count
                };

                if (catalog != null && catalog.Count != 0)
                {
                    ICollection<string> importedAssets = Array.Empty<string>();
                    using (var reader2 = new UnityPackageReader(packagePath))
                    {
                        importedAssets = reader2.Import(catalog, overwrite);
                        if (importedAssets != null)
                            packageResponse.ImportedAssets = importedAssets.ToArray();

                    }
                }

                response?.Invoke(packageResponse);
                return true;
            }
            catch (Exception e)
            {
                PLog.Error($"ERROR with importing '{packagePath}': {e.ToString()}");
                return false;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        // READER
        private class UnityPackageReader : IDisposable
        {
            public class AssetEntry
            {
                public string AssetPath;
                public string AssetGuid;
                public string AssetKey;
                public string AssetMetaKey;

                public string PackageHash { get; }

                public AssetEntry(string hash)
                {
                    PackageHash = hash;
                }
            }
            
            private readonly FileStream _fileReader;
            private readonly GZipInputStream _gzipStream;
            private readonly TarInputStream _tarInputStream;

            private Dictionary<string, AssetEntry> _cache; // key = hash

            public UnityPackageReader(string path)
            {
                _fileReader = File.OpenRead(path);
                _gzipStream = new GZipInputStream(_fileReader);
                _tarInputStream = new TarInputStream(_gzipStream);
                // Fourth step?
            }

            private const string ASSET_KEY = "asset";
            private const string ASSET_META_KEY = "asset.meta";
            private const string PATHNAME_KEY = "pathname";

            private bool IsParseableAssetFile(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return false;
                
                string hash = GetHash(filePath);
                if (hash == null)
                    return false;
                
                string fileName = Path.GetFileName(filePath);
                return fileName.EqualsOneOf(ASSET_KEY, ASSET_META_KEY, PATHNAME_KEY);
            }

            public ICollection<AssetEntry> LoadCatalog(string destinationPrefix = DEFAULT_ASSET_ROOT, ImportMode mode = ImportMode.Root)
            {
                var dict = new Dictionary<string, AssetEntry>();
                
                while (true)
                {
                    var tarEntry = _tarInputStream.GetNextEntry();
                    if (tarEntry == null)
                        break;
                    
                    if (tarEntry.IsDirectory)
                        continue;
                    
                    // Parse entry
                    string filePath = tarEntry.Name;
                    if (!IsParseableAssetFile(filePath))
                        continue;
                    
                    string hash = GetHash(filePath);
                    AssetEntry entry;
                    if (!dict.ContainsKey(hash))
                    {
                        entry = new AssetEntry(hash);
                        dict.Add(hash, entry);
                    }
                    else
                        entry = dict[hash];
                    
                    string fileName = Path.GetFileName(filePath);
                    switch (fileName)
                    {
                        case ASSET_KEY:
                            entry.AssetKey = filePath;
                            break;
                        case ASSET_META_KEY:
                            var metaBytes = ReadEntryFromStream(_tarInputStream);
                            string metaFileContents = Encoding.UTF8.GetString(metaBytes);
                            string[] lines = metaFileContents.SplitLines();

                            const string regexCode = "\\s*guid:\\s*([0-9a-zA-Z]+)";
                            foreach (string line in lines)
                            {
                                var match = Regex.Match(line, regexCode);
                                if (match.Success)
                                {
                                    string guid = match.Groups[1].Value;
                                    entry.AssetGuid = guid;
                                    break;
                                }
                            }
                            
                            if (entry.AssetGuid == null)
                                PLog.Error($"Something went wrong with detecting asset guid in '{filePath}'");
                            
                            entry.AssetMetaKey = filePath;
                            break;
                        case PATHNAME_KEY:
                            var bytes = ReadEntryFromStream(_tarInputStream);
                            string assetPath = Encoding.UTF8.GetString(bytes);
                            entry.AssetPath = assetPath;
                            break;
                    }

                    dict[hash] = entry;
                }

                var entries = dict.Values.Where(x => x.AssetKey != null).ToArray(); // NOTE: AssetKey == null for AssetFolders
                MorphAssetPaths(entries, destinationPrefix, mode);

                return entries;
            }

            public ICollection<string> Import(ICollection<AssetEntry> entries, OverwriteMode mode = OverwriteMode.Dont)
            {
                // Parse overwrites
                entries = entries.ToList(); // Enable removal
                foreach (var entry in entries.ToArray())
                {
                    string existingAssetPath = AssetDatabase.GUIDToAssetPath(entry.AssetGuid);
                    if (!string.IsNullOrEmpty(existingAssetPath))
                    {
                        if (mode == OverwriteMode.Dont)
                        {
                            entries.Remove(entry);
                            continue;
                        }

                        switch (mode)
                        {
                            case OverwriteMode.OverwriteKeep:
                                entry.AssetPath = existingAssetPath;
                                break;
                            case OverwriteMode.OverwriteReplace:
                                string fullPath = FileHelper.GetFullPath(existingAssetPath, FileHelper.GetProjectPath());
                                if (File.Exists(fullPath)) // If AssetDatabase is wrong again
                                    File.Delete(fullPath);
                                if (File.Exists(fullPath + ".meta")) // If AssetDatabase is wrong again
                                    File.Delete(fullPath + ".meta");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                        }
                    }
                }

                List<string> importedAssets = new List<string>();
                while (true)
                {
                    var tarEntry = _tarInputStream.GetNextEntry();
                    if (tarEntry == null)
                        break;
                    
                    if (tarEntry.IsDirectory)
                        continue;

                    // Parse entry
                    string filePath = tarEntry.Name;
                    if (!IsParseableAssetFile(filePath))
                        continue;
                    
                    string hash = GetHash(filePath);
                    string fileName = Path.GetFileName(filePath);
                    if (!fileName.EqualsOneOf(ASSET_KEY, ASSET_META_KEY))
                        continue;
                    
                    AssetEntry entry = entries.FirstOrDefault(x =>
                        x.PackageHash.Equals(hash, StringComparison.InvariantCulture));

                    if (entry == null)
                        continue;
                    
                    switch (fileName)
                    {
                        case ASSET_KEY:
                            // Better performant copy for large files
                            EnsureDirectoryForFile(entry.AssetPath);
                            using (var fileStream = new FileStream(entry.AssetPath, FileMode.Create, FileAccess.ReadWrite))
                            {
                                int bytesWritten = CopyEntryToStream(_tarInputStream, fileStream);
                                if (bytesWritten > 0)
                                {
                                    importedAssets.Add(entry.AssetPath);
                                }
                                else
                                {
                                    PLog.Error($"File ({hash}/{ASSET_KEY}) read failed for '{entry.AssetPath}'");
                                    entries.Remove(entry); // Clear entry
                                }
                            }
                            
                            // var assetBytes = ReadEntryFromStream(_tarInputStream);
                            // if (assetBytes.Length > 0)
                            // {
                            //     if (WriteAllBytes($"{entry.AssetPath}", assetBytes, true)) // TODO: is this safe?
                            //         importedAssets.Add(entry.AssetPath);
                            //     
                            //     // TODO: check if meta was already written?
                            //         
                            // }
                            // else
                            // {
                            //     AppLog.GetLog().Log($"File ({hash}/{ASSET_KEY}) read failed for '{entry.AssetPath}'");
                            //     entries.Remove(entry); // Clear entry
                            //     // TODO: check if meta was already written?
                            //     
                            // }

                            break;
                        case ASSET_META_KEY:
                            var assetMetaBytes = ReadEntryFromStream(_tarInputStream);
                            if (assetMetaBytes.Length > 0)
                            {
                                WriteAllBytes($"{entry.AssetPath}.meta", assetMetaBytes, true);
                                
                                // TODO: check if asset was already written, if this failed?
                            }
                            else
                            {
                                PLog.Error($"File ({hash}/{ASSET_META_KEY}) read failed for '{entry.AssetPath}'");
                                entries.Remove(entry); // Clear entry
                                // TODO: check if asset was already written, if this failed?
                            }
                            break;
                    }
                }

                return importedAssets;
            }

            public void Dispose()
            {
                _tarInputStream?.Close();
                _gzipStream?.Close();
                _fileReader?.Close();
            }

            
            // Unzip entry: 15df1416de33e234f90edd45e0f4ad1b/ = IsFile:False
            //     Unzip entry: 15df1416de33e234f90edd45e0f4ad1b/asset = IsFile:True
            //     Unzip entry: 15df1416de33e234f90edd45e0f4ad1b/asset.meta = IsFile:True
            //     Unzip entry: 15df1416de33e234f90edd45e0f4ad1b/pathname = IsFile:True
            //     Unzip entry: 15df1416de33e234f90edd45e0f4ad1b/preview.png = IsFile:True
            private void MorphAssetPaths(AssetEntry[] entries, string rootDir = DEFAULT_ASSET_ROOT, ImportMode mode = ImportMode.Root)
            {
                var assetPaths = entries.Select(x => x.AssetPath).ToArray();
                
                string commonParent = DEFAULT_ASSET_ROOT;
                if (mode == ImportMode.CommonParent)
                {
                    commonParent = Utility.GetLongestCommonPrefix(assetPaths);
                    if (string.IsNullOrWhiteSpace(commonParent))
                        commonParent = DEFAULT_ASSET_ROOT;
                    else
                    {
                        char separator = !commonParent.Contains(Path.AltDirectorySeparatorChar)
                            ? Path.DirectorySeparatorChar
                            : Path.AltDirectorySeparatorChar;
                        // Strip until last folder
                        int index = commonParent.LastIndexOf(separator);
                        if (index != -1)
                        {
                            commonParent = commonParent.Substring(0, index + 1);
                            commonParent = FileHelper.StripLastFolder(commonParent,
                                !commonParent.Contains(Path.AltDirectorySeparatorChar), true);
                        }
                        else
                        {
                            commonParent = DEFAULT_ASSET_ROOT;
                        }
                    }

                    PLog.Trace($"CommonParent detected as: '{commonParent}'");
                }

                foreach (var entry in entries)
                {
                    switch (mode)
                    {
                        case ImportMode.Root:
                            entry.AssetPath =
                                entry.AssetPath.Replace(DEFAULT_ASSET_ROOT, rootDir ?? DEFAULT_ASSET_ROOT);
                            break;
                        case ImportMode.CommonParent:
                            entry.AssetPath = entry.AssetPath.Replace(commonParent, rootDir ?? DEFAULT_ASSET_ROOT);
                            break;
                        case ImportMode.Flatten:
                            entry.AssetPath = Path.Combine(rootDir ?? DEFAULT_ASSET_ROOT,
                                Path.GetFileName(entry.AssetPath));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                    }
                }
            }

            private byte[] ReadEntryFromStream(TarInputStream stream)
            {

                bool canRead = true;
                List<byte> bytes = new List<byte>();
                while (canRead)
                {
                    byte[] tempBuffer = new byte[32 * 1024];
                    int readCount = stream.Read(tempBuffer, 0, tempBuffer.Length);
                    if (readCount <= 0)
                    {
                        canRead = false;
                        break;
                    }

                    if (readCount < tempBuffer.Length)
                    {
                        tempBuffer = tempBuffer.Take(readCount).ToArray();
                        canRead = false;
                    }

                    try
                    {
                        bytes.AddRange(tempBuffer);
                    }
                    catch (ArgumentException)
                    {
                        PLog.Error($"Bytes at {bytes.Count} bytes, or {bytes.Count / (1024 * 1024)} MBs");
                        throw;
                    }

                    if (bytes.Count > (512 * 1024 * 1024)) // TODO: 512 MB max size?
                    {
                        
                        PLog.Error($"File exceeds 512 MB file cap, skipping");
                        bytes.Clear();
                        break;
                    }
                }

                return bytes.ToArray();
            }
            
            private int CopyEntryToStream(TarInputStream stream, Stream outputStream)
            {
                byte[] tempBuffer = new byte[32 * 1024];

                int bytesWritten = 0;
                while (true)
                {
                    int numRead = stream.Read(tempBuffer, 0, tempBuffer.Length);
                    if (numRead <= 0)
                    {
                        break;
                    }

                    bytesWritten += numRead;
                    outputStream.Write(tempBuffer, 0, numRead);
                }

                return bytesWritten;
            }

            private string GetHash(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return null;

                string hash = key.Split('/').First();
                return hash;
            }
        }
        
        // Util
        private static bool IsValidUnityPackage(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || !packagePath.HasExtension(UNITYPACKAGE_EXTENSION))
                return false;

            return File.Exists(packagePath);
        }

        private static void EnsureDirectoryForFile(string filePath)
        {
            string containingFolder = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrWhiteSpace(containingFolder))
                Directory.CreateDirectory(containingFolder); // Ensure folder
        }

        private static bool WriteAllBytes(string path, byte[] bytes, bool overwriteIfExists = false)
        {
            if (File.Exists(path))
            {
                if (overwriteIfExists)
                    File.Delete(path);
                else
                    return false;
            }

            EnsureDirectoryForFile(path);

            File.WriteAllBytes(path, bytes);
            return true;
        }
    }
}
#endif