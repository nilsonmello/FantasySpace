using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class ShadowCaster2DExtensions
{
    private static readonly FieldInfo ShapePathField =
        typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo ShapePathHashField =
        typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void SetPath(this ShadowCaster2D shadowCaster, Vector3[] path)
    {
        ShapePathField.SetValue(shadowCaster, path);
    }

    public static void SetPathHash(this ShadowCaster2D shadowCaster, int hash)
    {
        ShapePathHashField.SetValue(shadowCaster, hash);
    }
}
