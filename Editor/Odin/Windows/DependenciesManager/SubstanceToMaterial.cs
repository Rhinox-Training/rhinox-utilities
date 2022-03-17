#if !UNITY_2017_1_OR_NEWER
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

namespace Rhinox.Utilities.Odin.Editor
{
    public struct SubstanceInfo
    {
        public ProceduralMaterial SubstanceMaterial;
        public Material NewMaterial;
        public List<Texture> Textures;
    }
    
    public class SubstanceToMaterial : OdinEditorWindow
    {
        public SubstanceArchive[] SubstanceList;
    
        private const string TempDirectory = "EXPORT_HERE";
    
        [InfoBox("If empty, defaults to a folder at the path of the substance material With the name of the material.")]
        public string MaterialSavePath;
        
        [InfoBox("If empty, defaults to a folder at the path of the substance material With the name of the material.")]
        public string TextureSavePath;
        
        [MenuItem("Tools/Substance/Export Textures")]
        static void Init() => GetWindow(typeof(SubstanceToMaterial));
    
        [Button(ButtonSizes.Medium)]
        private void Extract()
        {
            List<SubstanceInfo> infos;
            Extract(SubstanceList, out infos);
        }
    
        public static SubstanceInfo Extract(SubstanceArchive archive, string materialDir = null, string textureDir = null, string extractFolder = TempDirectory)
        {
            List<SubstanceInfo> infos;
            Extract(new[] {archive}, out infos, materialDir, textureDir, extractFolder);
    
            return infos.FirstOrDefault();
        }
        
        public static void Extract(SubstanceArchive[] archives, out List<SubstanceInfo> extractInfos, string materialDir = null, string textureDir = null, string extractFolder = TempDirectory)
        {
            extractInfos = new List<SubstanceInfo>();
            foreach (var substance in archives)
            {
                if (substance == null)
                    continue;
    
                string substancePath = AssetDatabase.GetAssetPath(substance.GetInstanceID());
                SubstanceImporter substanceImporter = AssetImporter.GetAtPath(substancePath) as SubstanceImporter;
                int substanceMaterialCount = substanceImporter.GetMaterialCount();
                ProceduralMaterial[] substanceMaterials = substanceImporter.GetMaterials();
    
                if (substanceMaterialCount <= 0)
                    continue;
    
                string basePath = substancePath.Replace("/" + substance.name + ".sbsar", "");
    
                if (!Directory.Exists(basePath + "/" + substance.name))
                {
                    AssetDatabase.CreateFolder(basePath, substance.name);
                    AssetDatabase.ImportAsset(basePath + "/" + substance.name);
                }
    
                if (!Directory.Exists( extractFolder ))
                    Directory.CreateDirectory(extractFolder);
    
                System.Type substanceImporterType = typeof(SubstanceImporter);
                MethodInfo exportBitmaps = substanceImporterType.GetMethod("ExportBitmaps", BindingFlags.Instance | BindingFlags.Public);
    
                var materialCount = substanceMaterials.Length;
                
                foreach (ProceduralMaterial substanceMaterial in substanceMaterials)
                {
                    bool generateAllOutputs = substanceImporter.GetGenerateAllOutputs(substanceMaterial);
    
                    var defaultDir = Path.Combine(basePath, substance.name, substanceMaterial.name);
                    if (materialCount == 1) // if just 1 material, can just skip this
                        defaultDir = Path.GetDirectoryName(defaultDir);
                    
                    Material newMaterial = new Material(substanceMaterial.shader);
                    string newMaterialDir = materialDir;
                    if (string.IsNullOrWhiteSpace(newMaterialDir))
                        newMaterialDir = defaultDir;
                    
                    var matFileName = Path.Combine(newMaterialDir, $"{substanceMaterial.name}.mat");
    
                    string newTextureDir = textureDir;
                    if (string.IsNullOrWhiteSpace(newTextureDir))
                        newTextureDir = defaultDir;
                    
                    if (!Directory.Exists(newMaterialDir))
                    {
                        AssetDatabase.CreateFolder(Path.GetDirectoryName(newMaterialDir), Path.GetFileName(newMaterialDir));
                        AssetDatabase.ImportAsset(newMaterialDir);
                    }
                    
                    if (File.Exists(matFileName))
                    {
                        var m =  AssetDatabase.LoadAssetAtPath<Material>( matFileName );
                        newMaterial = m;
                    }
                    else
                    {
                        newMaterial.CopyPropertiesFromMaterial(substanceMaterial);
                        AssetDatabase.CreateAsset(newMaterial, matFileName);
                        AssetDatabase.ImportAsset(matFileName);
                    }
                    
                    if (!Directory.Exists(newTextureDir))
                    {
                        AssetDatabase.CreateFolder(Path.GetDirectoryName(newTextureDir), Path.GetFileName(newTextureDir));
                        AssetDatabase.ImportAsset(newTextureDir);
                    }
                    
                    substanceImporter.SetGenerateAllOutputs(substanceMaterial, true);
                    exportBitmaps.Invoke(substanceImporter, new object[] { substanceMaterial, newTextureDir, true });
    
                    if (!generateAllOutputs)
                    {
                        substanceImporter.SetGenerateAllOutputs(substanceMaterial, false);
                    }
    
                    string[] exportedTextures = Directory.GetFiles(extractFolder);
    
                    if (exportedTextures.Length > 0)
                    {
                        foreach (string exportedTexture in exportedTextures)
                            File.Move(exportedTexture, Path.Combine(newTextureDir, exportedTexture.Replace(extractFolder, "")));
                    }
    
                    AssetDatabase.Refresh();
    
                    int propertyCount = ShaderUtil.GetPropertyCount(newMaterial.shader);
                    Texture[] materialTextures = substanceMaterial.GetGeneratedTextures();
    
                    if ((materialTextures.Length <= 0) || (propertyCount <= 0))
                        continue;
    
                    var info = new SubstanceInfo
                    {
                        NewMaterial = newMaterial,
                        SubstanceMaterial = substanceMaterial,
                        Textures = new List<Texture>()
                    };
    
                    foreach (ProceduralTexture materialTexture in materialTextures)
                    {
                        string newTexturePath = Path.Combine(newTextureDir, materialTexture.name + ".tga");
                        Texture newTextureAsset = AssetDatabase.LoadAssetAtPath(newTexturePath, typeof(Texture)) as Texture;
    
                        for (int i = 0; i < propertyCount; i++)
                        {
                            if (ShaderUtil.GetPropertyType(newMaterial.shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                                continue;
                            
                            var propertyName = ShaderUtil.GetPropertyName(newMaterial.shader, i);
                            var oldTex = newMaterial.GetTexture(propertyName);
    
                            if (oldTex != null)
                            {
                                if (newMaterial.GetTexture(propertyName).name == newTextureAsset.name)
                                    newMaterial.SetTexture(propertyName, newTextureAsset);
                            }
                            else
                            {
                                Debug.LogFormat("Property {0} has null Texture", propertyName);
                            }
                        }
    
                        if (materialTexture.GetProceduralOutputType() == ProceduralOutputType.Normal)
                        {
                            TextureImporter textureImporter = AssetImporter.GetAtPath(newTexturePath) as TextureImporter;
                            textureImporter.textureType = TextureImporterType.NormalMap;
                            AssetDatabase.ImportAsset(newTexturePath);
                        }
                        
                        info.Textures.Add(newTextureAsset);
                    }
                    
                    extractInfos.Add(info);
                }
    
                if (Directory.Exists(extractFolder))
                    Directory.Delete(extractFolder);
            }
        }
    }
}
#endif