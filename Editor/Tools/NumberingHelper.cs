using System.Linq;
using UnityEditor;
using Rhinox.Lightspeed;

namespace Rhinox.Utilities.Editor
{
    public static class NumberingHelper
    {
        const string HELPER_NAME = "Update Alphabetical numbering";
        private const string AlphabeticNumberingHeader = "Tools/Rhinox/" + HELPER_NAME;

        [MenuItem(AlphabeticNumberingHeader, priority = 10000)]
        private static void UpdateAlphabeticNumbering()
        {
            int n = -1;

            var objs = Selection.gameObjects 
                // TODO actual order over multiple Parents?
                .OrderBy(x => x.transform.GetSiblingIndex())
                .ToArray();

            for (int i = 0; i < objs.Length; ++i)
            {
                var go = objs[i];
                ++n;
                
                var numberings = Utility.FindAlphabetNumbering(go.name);

                if (numberings.Length >= 1)
                {
                    var grp = numberings[0];
                    if (n == 0)
                    {
                        n = Utility.AlphabetToNum(grp.Value);
                        continue;
                    }
                    var alphaNum = Utility.NumToAlphabet(n);
                    Undo.RegisterCompleteObjectUndo(go, HELPER_NAME);
                    go.name = go.name.Replace(grp.Index, grp.Length, alphaNum);
                }
                else
                {
                    ++n;
                    Undo.RegisterCompleteObjectUndo(go, HELPER_NAME);
                    go.name += " " + Utility.NumToAlphabet(n);
                }
            }
        }

        [MenuItem(AlphabeticNumberingHeader, validate = true)]
        private static bool HasMultiSelection()
        {
            return Selection.gameObjects.Length > 1;
        }
    }
}