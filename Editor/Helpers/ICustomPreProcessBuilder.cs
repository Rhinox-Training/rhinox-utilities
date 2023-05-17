using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Assets.Utilities.Editor.Helpers
{
    public abstract class CustomPreProcessBuilderWithErrorReport : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static bool _BuildDone;
        private static BuildReport _buildReport;

        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            _BuildDone = false;
            _buildReport = report;

            EditorApplication.update += BuildCheck;

            PreprocessBuild(report);
        }

        public abstract void PreprocessBuild(BuildReport report);
        public abstract void PostprocessBuild(BuildReport report);

        private void BuildCheck()
        {
            if (!BuildPipeline.isBuildingPlayer)
            {
                EditorApplication.update -= BuildCheck;
                OnPostprocessBuild(_buildReport);
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (_BuildDone)
                return;
            _BuildDone = true;

            PostprocessBuild(report);
        }
    }
}
