using System;

/// <summary>
/// NOTE: only affects Unity Behaviors
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class ExecutionOrderAttribute : System.Attribute
{
	public int order;

	public ExecutionOrderAttribute(int order)
	{
		this.order = order;
	}
}

/// <summary>
/// NOTE: only affects Unity Behaviors
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class ExecuteAfterAttribute : System.Attribute
{
	public Type targetType;
	public int orderIncrease;

	public ExecuteAfterAttribute(Type targetType)
	{
		this.targetType = targetType;
		this.orderIncrease = 10;
	}
}

/// <summary>
/// NOTE: only affects Unity Behaviors
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class ExecuteBeforeAttribute : System.Attribute
{
	public Type targetType;
	public int orderDecrease;

	public ExecuteBeforeAttribute(Type targetType)
	{
		this.targetType = targetType;
		this.orderDecrease = 10;
	}
}
