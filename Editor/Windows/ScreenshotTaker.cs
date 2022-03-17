//C# Example

using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Screenshot : EditorWindow
{
    public Camera myCamera;
    public string lastScreenshot = "";
    
    private int _resWidth = Screen.width * 4;
    private int _resHeight = Screen.height * 4;
    private int _scale = 1;
    private float _lastTime;
    private string _path = "";
    private bool _takeHiResShot;
    private bool _isTransparent;
    private RenderTexture _renderTexture;

    internal static void ShowWindow()
    {
        var editorWindow = GetWindow(typeof(Screenshot));
        editorWindow.autoRepaintOnSceneChange = true;
        editorWindow.titleContent.text = "Screenshot";
        editorWindow.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
        _resWidth = EditorGUILayout.IntField("Width", _resWidth);
        _resHeight = EditorGUILayout.IntField("Height", _resHeight);

        EditorGUILayout.Space();

        _scale = EditorGUILayout.IntSlider("Scale", _scale, 1, 15);

        EditorGUILayout.HelpBox(
            "The default mode of screenshot is crop - so choose a proper width and height. The scale is a factor " +
            "to multiply or enlarge the renders without loosing quality.", MessageType.None);

        EditorGUILayout.Space();

        GUILayout.Label("Save Path", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField(_path, GUILayout.ExpandWidth(false));
        if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
            _path = EditorUtility.SaveFolderPanel("Path to Save Images", _path, Application.dataPath);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Choose the folder in which to save the screenshots ", MessageType.None);
        EditorGUILayout.Space();

        GUILayout.Label("Select Camera", EditorStyles.boldLabel);

        myCamera = EditorGUILayout.ObjectField(myCamera, typeof(Camera), true, null) as Camera;

        if (myCamera == null)
            myCamera = Camera.main;

        _isTransparent = EditorGUILayout.Toggle("Transparent Background", _isTransparent);

        EditorGUILayout.HelpBox(
            "Choose the camera of which to capture the render. You can make the background transparent using the transparency option.",
            MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);

        if (GUILayout.Button("Set To Screen Size"))
        {
            _resHeight = (int) Handles.GetMainGameViewSize().y;
            _resWidth = (int) Handles.GetMainGameViewSize().x;
        }

        if (GUILayout.Button("Default Size"))
        {
            _resHeight = 1440;
            _resWidth = 2560;
            _scale = 1;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Screenshot will be taken at " + _resWidth * _scale + " x " + _resHeight * _scale + " px", EditorStyles.boldLabel);

        if (GUILayout.Button("Take Screenshot", GUILayout.MinHeight(60)))
        {
            if (_path == "") 
                _path = EditorUtility.SaveFolderPanel("Path to Save Images", _path, Application.dataPath);
            
            TakeHiResShot();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Open Last Screenshot", GUILayout.MaxWidth(160), GUILayout.MinHeight(40)))
        {
            if (lastScreenshot != "")
            {
                Application.OpenURL("file://" + lastScreenshot);
                Debug.Log("Opening File " + lastScreenshot);
            }
        }

        if (GUILayout.Button("Open Folder", GUILayout.MaxWidth(100), GUILayout.MinHeight(40)))
            Application.OpenURL("file://" + _path);

        if (GUILayout.Button("More Assets", GUILayout.MaxWidth(100), GUILayout.MinHeight(40)))
            Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/publisher/5951");

        EditorGUILayout.EndHorizontal();

        if (_takeHiResShot)
        {
            var resWidthN = _resWidth * _scale;
            var resHeightN = _resHeight * _scale;
            var rt = new RenderTexture(resWidthN, resHeightN, 24);

            if (myCamera == null)
            {
                Debug.LogError("Camera is null!");
                return;
            }
            
            myCamera.targetTexture = rt;
            var tFormat = _isTransparent ? TextureFormat.ARGB32 : TextureFormat.RGB24;
            var screenShot = new Texture2D(resWidthN, resHeightN, tFormat, false);
            
            myCamera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, resWidthN, resHeightN), 0, 0);
            myCamera.targetTexture = null;
            RenderTexture.active = null;
            
            var bytes = screenShot.EncodeToPNG();
            var filename = ScreenShotName(resWidthN, resHeightN);

            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log($"Screenshot saved to: {filename}");
            Application.OpenURL(filename);
            _takeHiResShot = false;
        }

        EditorGUILayout.HelpBox("In case of any error, make sure you have Unity Pro as the plugin requires Unity Pro to work.", MessageType.Info);
    }

    public string ScreenShotName(int width, int height)
    {
        var path = $"{_path}/screen_{width}x{height}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        lastScreenshot = path;
        return path;
    }

    public void TakeHiResShot()
    {
        Debug.Log("Taking Screenshot");
        _takeHiResShot = true;
    }
}