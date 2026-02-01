namespace IndexSmith.EFCore.SqlServer.Attributes;

/// <summary>
/// Prevents automatic index creation on the decorated property,
/// overriding any heuristic rules that would otherwise apply.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class NoAutoIndexAttribute : Attribute
{
}
