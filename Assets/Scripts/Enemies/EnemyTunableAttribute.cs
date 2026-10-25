using System;

[AttributeUsage(AttributeTargets.Field)]
public sealed class EnemyTunableAttribute : Attribute
{
    public readonly string Label;
    public readonly float Minimum, Maximum;
    public EnemyTunableAttribute(string label, float minimum, float maximum)
    {
        Label = label;
        Minimum = minimum;
        Maximum = maximum;
    }
}
