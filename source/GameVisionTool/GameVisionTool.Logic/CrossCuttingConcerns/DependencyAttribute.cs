namespace GameVisionTool.Logic.CrossCuttingConcerns;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class DependencyAttribute : Attribute { }