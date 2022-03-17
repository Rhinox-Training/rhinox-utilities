using UnityEngine;

public class Example : MonoBehaviour
{
    // Rotate a button 10 degrees clockwise when presed.

    float rotAngle = 0;
    Vector2 pivotPoint;

    void OnGUI()
    {
        GUILayout.Label("Test Before");
        var mat = GUI.matrix;
        GUIUtility.RotateAroundPivot(rotAngle, pivotPoint);
        if (GUILayout.Button("Rotate"))
        {
            rotAngle += 10;
        }
        GUI.matrix = mat;
        pivotPoint = GUILayoutUtility.GetLastRect().center;

        
        GUILayout.Label("Test After");
    }
}