using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

[Serializable]
public class ResiduoCatalog
{
    public int schema_version;
    public string actualizado;
    public Residuo[] residuos;
}

[Serializable]
public class Residuo
{
    public long id;
    public string categoria;
    public string subcategoria;
    public string nombre;
    public string material;
    public string submaterial;
    public string descripcion;
    public string[] keywords;
    public string img;

    public IEnumerable<string> AllSearchTokens()
    {
        yield return ResiduoStringUtility.Normalize(nombre);
        if (keywords != null)
        {
            foreach (var keyword in keywords)
            {
                yield return ResiduoStringUtility.Normalize(keyword);
            }
        }
        if (!string.IsNullOrWhiteSpace(material))
        {
            yield return ResiduoStringUtility.Normalize(material);
        }
        if (!string.IsNullOrWhiteSpace(submaterial))
        {
            yield return ResiduoStringUtility.Normalize(submaterial);
        }
    }
}

public static class ResiduoStringUtility
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lower = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(lower.Length);
        foreach (var c in lower)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool ContainsToken(string source, string term)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(term))
        {
            return false;
        }

        return Normalize(source).Contains(term);
    }
}
