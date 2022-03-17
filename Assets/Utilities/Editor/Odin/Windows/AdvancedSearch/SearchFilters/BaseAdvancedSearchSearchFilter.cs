using System;
using System.Collections.Generic;
using Rhinox.Utilities.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;


[Serializable, HideReferenceObjectPicker]
public abstract class BaseAdvancedSearchSearchFilter
{
    public string Name { get; }

    public event Action<BaseAdvancedSearchSearchFilter> Changed;
    
    protected BaseAdvancedSearchSearchFilter(string name)
    {
        Name = name;
        Enabled = true;
    }

    public bool Enabled { get; set; }

    public virtual void Reset()
    {
        TriggerChanged();
    }

    public abstract ICollection<GameObject> ApplyFilter(ICollection<GameObject> selectedObjs);
    
    public abstract string GetFilterInfo();

    public virtual void HandleDragged(Object draggedObject)
    {
    }

    public void Toggle()
    {
        Enabled = !Enabled;
        TriggerChanged();
    }

    protected void TriggerChanged() => OnChanged();

    protected virtual void OnChanged()
    {
        Changed?.Invoke(this);
    }
}