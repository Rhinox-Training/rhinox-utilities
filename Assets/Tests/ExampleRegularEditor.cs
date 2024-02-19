using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using UnityEngine;

public class ExampleRegularEditor : MonoBehaviour
{
    [AssignableTypeFilter(typeof(MonoBehaviour))]
    public SerializableType Type;
    
    public SerializableGuid Guid;
    
    public SceneReference Scene;
    
    public GenericPairList TestPairList = new GenericPairList();

}