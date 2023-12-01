namespace Rhinox.Utilities.Editor
{
    public interface IJobProcessor
    {
        AssetChanges OnCompleted(IImportJob job, ImportState completedState, AssetChanges importChanges);
    }
}