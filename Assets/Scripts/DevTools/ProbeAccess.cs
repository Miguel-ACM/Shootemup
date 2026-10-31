using System;
using System.Reflection;

// Review probes may address either legacy fields or the runtime's encapsulated properties.
public static class ProbeAccess
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    public static object Get(object target, string name)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, Flags);
            if (field != null) return field.GetValue(target);
            var property = type.GetProperty(name, Flags);
            if (property != null) return property.GetValue(target);
        }
        throw new MissingMemberException(target.GetType().Name, name);
    }
    public static void Set(object target, string name, object value)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, Flags);
            if (field != null) { field.SetValue(target, value); return; }
            var property = type.GetProperty(name, Flags);
            if (property != null) { property.SetValue(target, value); return; }
        }
        throw new MissingMemberException(target.GetType().Name, name);
    }
}
