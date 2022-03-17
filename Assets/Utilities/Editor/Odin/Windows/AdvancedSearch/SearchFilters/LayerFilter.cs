using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
using UnityEditorInternal;
using UnityEngine;

[Serializable]
public class LayerFilter : BaseAdvancedSearchSearchFilter
{
    [ShowInInspector, LabelText("Layers"), CustomValueDrawer(nameof(DrawLabelMask))]
    [OnValueChanged(nameof(TriggerChanged))]
    private int _layerMask = ~0;

    private List<string> _layers = new List<string>();

    public LayerFilter() : base("Layer")
    {
    }

    public override void Reset()
    {
        _layers.Clear();
        
        _layerMask = ~0;

        base.Reset();
    }

    private int DrawLabelMask(int value, GUIContent label)
    {
        return eUtility.LayerMaskField(label, value);
    }

    public override ICollection<GameObject> ApplyFilter(ICollection<GameObject> selectedObjs)
    {
        if (_layerMask == 0)
        {
            selectedObjs.RemoveAll(x => x.layer != 0);
        }
        else
        {
            var mask = (LayerMask) _layerMask;
            selectedObjs.RemoveAll(x => !mask.HasLayer(x.layer));
        }

        return selectedObjs;
    }

    public override string GetFilterInfo()
    {
        var mask = (LayerMask) _layerMask;

        // if mask is not set or it encompasses all layers; show nothing
        if (_layerMask == ~0 || InternalEditorUtility.layers.All(x => mask.HasLayer(x)))
            return string.Empty;
        
        eUtility.LayerMaskToLayers(_layerMask, ref _layers);

        string searchInfo = "";
        if (_layers.Count == 0)
            searchInfo += "No layer";
        else if (_layers.Count == 1)
            searchInfo += "A layer of: ";
        else if (_layers.Count > 1)
            searchInfo += $"One of the following layers:{Environment.NewLine}";

        searchInfo += string.Join(", ", _layers);

        return searchInfo;
    }
}