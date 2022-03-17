using System.Collections;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities
{
    internal static class GameObjectGrouping
    {
        private const string _groupOpName = "GameObject/Group Object(s) %g";
        
        [MenuItem(_groupOpName, false, 10)]
        public static void GroupOperation(MenuCommand menuCommand)
        {
            //Prevent executing multiple times when right-clicking.
            if (Selection.objects.Length > 1)
            {
                if (menuCommand.context != null && menuCommand.context != Selection.objects[0])
                    return;
            }
            Selection.transforms.GroupTransforms(recordUndo: true);
        }
        
        [MenuItem(_groupOpName, true)]
        private static bool ValidateGroupOperation()
        {
            return Selection.transforms.Length != 0;
        }
    }
}
