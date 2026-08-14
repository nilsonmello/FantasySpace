using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
// Se seu projeto usa uma versão do URP anterior à 14 (Unity < 2022.2/6),
// troque o using acima por: using UnityEngine.Experimental.Rendering.Universal;

/// <summary>
/// A API pública do ShadowCaster2D só expõe "shapePath" como getter.
/// Essa extensão acessa os campos privados via reflection para poder
/// gerar shapes proceduralmente em runtime.
/// </summary>
public static class ShadowCaster2DExtensions
{
    private static readonly FieldInfo ShapePathField =
        typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo ShapePathHashField =
        typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>Substitui o path que define o formato do shadow caster.</summary>
    public static void SetPath(this ShadowCaster2D shadowCaster, Vector3[] path)
    {
        ShapePathField.SetValue(shadowCaster, path);
    }

    /// <summary>
    /// Força o rebuild interno da malha de sombra. Precisa ser chamado
    /// depois de SetPath com um valor diferente do hash anterior.
    /// </summary>
    public static void SetPathHash(this ShadowCaster2D shadowCaster, int hash)
    {
        ShapePathHashField.SetValue(shadowCaster, hash);
    }
}
