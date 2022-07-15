using Rhinox.Utilities;
using UnityEditor;
using UnityEngine.AI;

namespace Tests.Editor
{
    public static class NavMeshGeneratorTest
    {
        [MenuItem("Test/Regenerate Nav Mesh")]
        public static void RegenerateNavMesh()
        {
            NavMeshHelper.BakeNavMesh(NavMeshSearchSettings.AllMeshRenderers);
        }
    }
}