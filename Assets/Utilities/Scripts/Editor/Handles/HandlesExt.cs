using Rhinox.GUIUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rhinox.Utilities.Editor
{
    public static class HandlesExt
    {
        /// ================================================================================================================
        /// COLOR
        private static readonly GUIFrameAwareStack<Color> ColorStack = new GUIFrameAwareStack<Color>();

        public static void PushColor(Color c)
        {
            ColorStack.Push(Handles.color);
            Handles.color = c;
        }

        public static void PopColor()
        {
            Handles.color = ColorStack.Pop();
        }

        /// ================================================================================================================
        /// Z-TEST
        private static readonly GUIFrameAwareStack<CompareFunction> ZTestStack = new GUIFrameAwareStack<CompareFunction>();

        public static void PushZTest(CompareFunction f)
        {
            ZTestStack.Push(Handles.zTest);
            Handles.zTest = f;
        }

        public static void PopZTest()
        {
            Handles.zTest = ZTestStack.Pop();
        }

        /// ================================================================================================================
        /// MATRICES
        private static readonly GUIFrameAwareStack<Matrix4x4> MatrixStack = new GUIFrameAwareStack<Matrix4x4>();

        /// <summary>
        /// Pushes a matrix with the given offset; use PopMatrix to undo
        /// </summary>
        public static void PushPositionOffset(Vector3 offset)
        {
            HandlesExt.PushMatrix(Handles.matrix * Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one));
        }

        /// <summary>
        /// Pushes a matrix with the given scale; use PopMatrix to undo
        /// </summary>
        public static void PushScale(Vector3 scale)
        {
            HandlesExt.PushMatrix(Handles.matrix * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale));
        }

        /// <summary>
        /// Pushes a matrix with the given scale; use PopMatrix to undo
        /// </summary>
        public static void PushScale(float uniformScale) => PushScale(uniformScale * Vector3.one);

        public static void PushMatrix(Matrix4x4 matrix)
        {
            HandlesExt.MatrixStack.Push(Handles.matrix);
            Handles.matrix = matrix;
        }

        /// <summary>
        /// Pops the GUI matrix pushed by <see cref="M:Sirenix.Utilities.Editor.GUIHelper.PushMatrix(UnityEngine.Matrix4x4)" />.
        /// </summary>
        public static void PopMatrix()
        {
            Handles.matrix = HandlesExt.MatrixStack.Pop();
        }
        
        
        public static void DrawSolidArc(Vector3 center, Vector3 startDir, Vector3 endDir, float radius, Color color)
        {
            Color oldColor = Gizmos.color;
            Handles.color = color;

            Handles.DrawSolidArc(center, Vector3.Cross(startDir, endDir), startDir, Vector3.Angle(startDir, endDir),
                radius);

            Handles.color = oldColor;
        }
    }
}