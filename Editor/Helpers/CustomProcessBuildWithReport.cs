using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Rhinox.Utilities.Editor
{
    public abstract class CustomProcessBuildWithReport : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static bool _buildDone;
        private static BuildReport _buildReport;

        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            _buildDone = false;
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
            if (_buildDone)
                return;
            _buildDone = true;

            PostprocessBuild(report);
        }
    }
}
