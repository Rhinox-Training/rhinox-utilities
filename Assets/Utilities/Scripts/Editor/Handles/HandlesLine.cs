using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public struct HandlesLine
    {
        public Vector3 Start => _line.Start;
        public Vector3 End => _line.End;
        public Vector3 Center => _line.Center;
        public Vector3 Direction => _line.Direction;
        public float Length => _line.Length;

        public Color Color;

        private Line _line;

        public HandlesLine(Vector3 start, Vector3 end)
        {
            _line = new Line(start, end);
            Color = Color.white;
        }
        
        public HandlesLine(Vector3 start, Vector3 end, Color color)
        {
            _line = new Line(start, end);
            Color = color;
        }

        public void Draw()
        {
            HandlesExt.PushColor(Color);
            
            Handles.DrawLine(_line.Start, _line.End);
            
            HandlesExt.PopColor();
        }

        public float GetDrawnLength()
        {
            var start = Handles.matrix.MultiplyPoint(Start);
            var end = Handles.matrix.MultiplyPoint(End);
            return (start - end).magnitude;
        }

        public static implicit operator Line(HandlesLine line) => line._line;
        public static implicit operator HandlesLine(Line line) => new HandlesLine(line.Start, line.End);

    }
}