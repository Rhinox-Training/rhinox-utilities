﻿using System;
using UnityEngine;

// NOTE DON'T put in an editor folder!
[AttributeUsage(AttributeTargets.Field)]
public class AutohookAttribute : PropertyAttribute
{
}

[AttributeUsage(AttributeTargets.Field)]
public class AutohookFromChildrenAttribute : PropertyAttribute
{
}

[AttributeUsage(AttributeTargets.Field)]
public class AutohookFromParentAttribute : PropertyAttribute
{
}