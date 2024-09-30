using SimpleInjector.Advanced;
using System.Reflection;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

public class DependencyAttributePropertySelectionBehavior : IPropertySelectionBehavior
{
    public bool SelectProperty(Type type, PropertyInfo prop) =>
        prop.GetCustomAttributes(typeof(DependencyAttribute)).Any();
}