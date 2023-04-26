using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;

namespace Assets.Utilities.Editor.Helpers
{
    internal interface ICustomPreProcessBuilder
    {
        void OnPreprocessBuild(BuildReport report);

        void CompilationPipelineOnCompilationStarted(object compilationContext);

        void CompilationPipelineOnAssemblyCompilationFinished(string path, CompilerMessage[] messages);

        void CompilationPipelineOnCompilationFinished(object compilationContext);
    }
}
