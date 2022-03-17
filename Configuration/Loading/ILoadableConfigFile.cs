namespace Rhinox.Utilities
{
    public interface ILoadableConfigFile : IConfigFile
    {
        string RelativeFilePath { get; }
        
        bool Load(string path);

        bool Save(string path, bool overwrite = false);
    }
}