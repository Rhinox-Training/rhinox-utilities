/*
#if ODIN_INSPECTOR 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEngine.Events;
using Object = System.Object;

[HideReferenceObjectPicker]
public struct InspectorEvent
{
    [TitleGroup("$Name", Alignment = TitleAlignments.Centered)]
    [HideLabel]
    public BetterEvent Event;

    public string Name => Info.Name;

    [SerializeField, HideInInspector]
    private EventInfo _info;
    public EventInfo Info => _info;
    
    private object _subscribedTarget;

    private Delegate _invokeHandler;
    
    public InspectorEvent(EventInfo info)
    {
        _info = info;
        Event = new BetterEvent { Events = new List<BetterEventEntry>() };
        
        _invokeHandler = null;
        _subscribedTarget = null;
    }

    public void Subscribe(object target)
    {
        if (target == null) return;
        
        Unsubscribe();

        _subscribedTarget = target;
        _invokeHandler = CreateEventHandler(Info, Event.Invoke);
        
        Info.AddEventHandler(_subscribedTarget, _invokeHandler);
    }

    public void Unsubscribe()
    {
        if (_subscribedTarget == null) return;
        
        Info.RemoveEventHandler(_subscribedTarget, _invokeHandler);
    }
    
    private static Delegate CreateEventHandler(EventInfo eventInfo,  Action action)
    {
        var parameters = eventInfo.EventHandlerType
            .GetMethod("Invoke")
            .GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType))
            .ToArray();

        var handler = Expression.Lambda(
                eventInfo.EventHandlerType, 
                Expression.Call(Expression.Constant(action), "Invoke", Type.EmptyTypes), 
                parameters
            )
            .Compile();

        return handler;
    }
}

public class InspectorEvents : SerializedMonoBehaviour
{
    [OnValueChanged(nameof(FetchEventInfos))]
    public Object Target;
    
    [ListDrawerSettings(HideAddButton = true, Expanded = true)]
    public List<InspectorEvent> Events = new List<InspectorEvent>();
    
    [LabelText("Add new Event")]
    [InlineButton(nameof(AppendEvent), "Add")]
    [ShowInEditor, ValueDropdown(nameof(GetEventOptions))]
    private EventInfo _selectedInfo;

    private EventInfo[] _infos;

    private void Awake()
    {
        _infos = Target.GetType().GetEvents();
    }

    private void OnEnable()
    {
        if (Target == null || Events == null)
        {
            enabled = false;
            return;
        }
        
        foreach (var e in Events)
            e.Subscribe(Target);
    }

    private void OnDisable()
    {
        if (Events == null) return;
        
        foreach (var e in Events)
            e.Unsubscribe();
    }

    private void FetchEventInfos()
    {
        _infos = Target == null ? Array.Empty<EventInfo>() : Target.GetType().GetEvents();
    }

    private void AppendEvent()
    {
        Events.Add(new InspectorEvent(_selectedInfo));
        _selectedInfo = null;
    }

    private ValueDropdownItem<EventInfo>[] GetEventOptions()
    {
        if (_infos == null)
            FetchEventInfos();
        
        if (!_infos.Any())
            return Array.Empty<ValueDropdownItem<EventInfo>>();

        return _infos
            .Where(ei => Events.All(e => e.Info != ei))
            .Select(x => new ValueDropdownItem<EventInfo>(x.Name, x))
            .ToArray();
    }
}
#endif
*/