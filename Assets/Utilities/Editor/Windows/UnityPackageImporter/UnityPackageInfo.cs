using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class UnityPackageInfo
{
    public class UnityPackageFile
    {
        public readonly string GUID;
        public string TarPath;
        public string ActualPath;
        public bool HasAsset;
        public MemorySize Size;
        public Texture Preview;

        public UnityPackageFile(string guid)
        {
            GUID = guid;
        }
    }

    public readonly string Name;
    public readonly string FilePath;
    public readonly string Folder;
    public readonly FileInfo FileInfo;
    public readonly MemorySize CompressedFileSize;
    
    public bool IsLoaded { get; private set; }
    public string Root { get; private set; }
    public MemorySize UncompressedFileSize { get; private set; }


    public readonly List<UnityPackageFile> Contents = new List<UnityPackageFile>();
    
    public UnityPackageInfo(string path)
    {
        FilePath = path.ToLinuxSafePath();
        Folder = Path.GetDirectoryName(FilePath);
        Name = Path.GetFileNameWithoutExtension(path);
        FileInfo = new FileInfo(FilePath);
        // Convert bytes to MB (power of 2 = bytes - kilobytes - mb)
        CompressedFileSize = MemorySize.From(FileInfo.Length);
    }

    public void Load()
    {
        var sw = new Stopwatch();
        sw.Start();

        UnityPackageFile file = null;
        string guid = null;
        foreach (var (entry, tarStream) in EnumerateContents())
        {
            if (guid == null || !entry.Name.StartsWith(guid))
            {
                // Add previous file
                if (file?.HasAsset == true)
                    Contents.Add(file);
                if (entry.Name.TryGetIndexOf("/", out int index))
                    guid = entry.Name.Substring(0, index);
                else
                    guid = entry.Name;
                file = new UnityPackageFile(guid);
            }

            if (entry.IsDirectory)
                continue;
                    
            string filename = Path.GetFileName(entry.Name);
            if (filename == "asset")
            {
                file.HasAsset = true;
                file.Size = new MemorySize(entry.Size);
            }
            else if (filename == "pathname")
            {
                string actualPath = Encoding.UTF8.GetString(GetData(tarStream));
                // idk why but somehow this thing sometimes has a \n00 appended
                if (actualPath.TryGetIndexOf("\n", out int index))
                    actualPath = actualPath.Substring(0, index);
                file.ActualPath = actualPath;
                file.TarPath = entry.Name;
            }
            else if (filename == "preview.png")
            {
                var data = GetData(tarStream);
                var tex = new Texture2D(1, 1);
                tex.LoadImage(data);
                file.Preview = tex;
            }
        }
        
        // Add last file
        if (file?.HasAsset == true)
            Contents.Add(file);
        
        Debug.Log($"Time taken to load: {sw.Elapsed.TotalSeconds:F}s");

        Contents.SortBy(x => x.ActualPath);

        Root = Contents.Select(x => Path.GetDirectoryName(x.ActualPath)).ToArray().GetCommonPrefix().ToLinuxSafePath();
        UncompressedFileSize = new MemorySize(Contents.Select(x => x.Size));
            
        IsLoaded = true;
    }

    public IEnumerable<(TarEntry entry, TarInputStream stream)> EnumerateContents()
    {
        using (var tar = new GZipInputStream(new BufferedStream(File.OpenRead(FilePath))))
        {
            using (TarInputStream tarStream = new TarInputStream(new BufferedStream(tar), TarBuffer.BlockSize))
            {
                TarEntry entry;
                UnityPackageFile file = null;
                while ((entry = tarStream.GetNextEntry()) != null) // Go through all files from archive
                {
                    yield return (entry, tarStream);
                }
            }
        }
    }

    public void ImportGuids(string[] guids, string targetRoot)
    {
        string guid = null;
        byte[] data = null, metaData = null;
        foreach (var (entry, tarStream) in EnumerateContents())
        {
            if (guid == null || !entry.Name.StartsWith(guid))
            {
                if (entry.Name.TryGetIndexOf("/", out int index))
                    guid = entry.Name.Substring(0, index);
                else
                    guid = entry.Name;
                data = null;
                metaData = null;
            }
            if (!guids.Contains(guid))
                continue;

            string filename = Path.GetFileName(entry.Name);

            if (filename == "asset")
                data = GetData(tarStream);
            else if (filename == "asset.meta")
                metaData = GetData(tarStream);
            else if (filename == "pathname")
            {
                if (data == null || metaData == null)
                    continue;
                string actualPath = Encoding.UTF8.GetString(GetData(tarStream));
                // idk why but somehow this thing sometimes has a \n00 appended
                if (actualPath.TryGetIndexOf("\n", out int index))
                    actualPath = actualPath.Substring(0, index);
                var path = actualPath.Replace(Root, "");
                if (path.StartsWith("/")) path = path.Substring(1);
                path = Path.Combine(targetRoot, path);
                path = Path.GetFullPath(path).ToLinuxSafePath();
                var metaPath = path + ".meta";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, data);
                File.WriteAllBytes(metaPath, metaData);
            }
        }
        
        EditorApplication.delayCall += AssetDatabase.Refresh;
    }

    private byte[] GetData(TarInputStream stream)
    {
        using (MemoryStream fs = new MemoryStream())
        {
            stream.CopyEntryContents(fs);
            return fs.ToArray();
        }
    }
}