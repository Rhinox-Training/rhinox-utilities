using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;

namespace Assets.Utilities.Editor.Helpers
{
    internal interface ICustomPreProcessBuilder
    {
        void OnPreprocessBuild(BuildReport report);

        //void CompilationPipelineOnCompilationStarted(object compilationContext);

        //void CompilationPipelineOnAssemblyCompilationFinished(string path, CompilerMessage[] messages);

        //void CompilationPipelineOnCompilationFinished(object compilationContext);
    }

    public abstract class CustomPreProcessBuilderWithErrorReport : ICustomPreProcessBuilder
    {
        public void OnPreprocessBuild(BuildReport report)
        {
            PreprocessBuild(report);
        }

        public abstract void PreprocessBuild(BuildReport report);


        //public void CompilationPipelineOnAssemblyCompilationFinished(string path, CompilerMessage[] messages)
        //{
        //    // throw new System.NotImplementedException();
        //}

        //public void CompilationPipelineOnCompilationFinished(object compilationContext)
        //{
        //    //throw new System.NotImplementedException();
        //}

        //public void CompilationPipelineOnCompilationStarted(object compilationContext)
        //{
        //    //throw new System.NotImplementedException();
        //}


    }
}
