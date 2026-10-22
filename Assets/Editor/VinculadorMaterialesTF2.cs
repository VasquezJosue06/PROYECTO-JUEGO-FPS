using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class VinculadorMaterialesTF2 : EditorWindow
{
    // =========================================================
    // RUTAS
    // =========================================================

    private const string CARPETA_MATERIALES = "Assets/maps/2fort/Materials";
    private const string CARPETA_TEXTURAS = "Assets/maps/Textures";
    private const string CARPETA_VMT = "Assets/maps/VMT";


    // =========================================================
    // 1. PRIMERA PASADA: NOMBRE MATERIAL -> PNG
    // =========================================================

    [MenuItem("Tools/TF2/Auto Link 2Fort Materials")]
    public static void VincularMaterialesAutomaticamente()
    {
        string[] materialGuids = AssetDatabase.FindAssets(
            "t:Material",
            new[] { CARPETA_MATERIALES }
        );

        string[] textureGuids = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { CARPETA_TEXTURAS }
        );

        Dictionary<string, List<string>> textures =
            BuildTextureNameDictionary(textureGuids);

        int linked = 0;
        int missing = 0;
        int ambiguous = 0;
        int alreadyAssigned = 0;

        foreach (string guid in materialGuids)
        {
            string materialPath =
                AssetDatabase.GUIDToAssetPath(guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
                continue;

            if (!material.HasProperty("_BaseMap"))
                continue;

            // Proteger materiales ya resueltos
            if (material.GetTexture("_BaseMap") != null)
            {
                alreadyAssigned++;
                continue;
            }

            string materialName =
                NormalizarNombre(material.name);

            if (textures.TryGetValue(
                    materialName,
                    out List<string> matches))
            {
                if (matches.Count == 1)
                {
                    AsignarTextura(material, matches[0]);
                    linked++;
                }
                else
                {
                    ambiguous++;

                    Debug.LogWarning(
                        $"[TF2] Coincidencia ambigua para " +
                        $"{material.name}: {matches.Count} texturas."
                    );
                }

                continue;
            }

            missing++;

            Debug.Log(
                $"[TF2] Sin coincidencia exacta: {material.name}"
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string result =
            $"Materiales encontrados: {materialGuids.Length}\n" +
            $"Texturas encontradas: {textureGuids.Length}\n\n" +
            $"Asignados automáticamente: {linked}\n" +
            $"Ya tenían textura: {alreadyAssigned}\n" +
            $"Coincidencias ambiguas: {ambiguous}\n" +
            $"Sin coincidencia: {missing}";

        Debug.Log("[TF2 MATERIAL AUTO LINKER]\n" + result);

        EditorUtility.DisplayDialog(
            "TF2 Material Auto Linker",
            result,
            "Aceptar"
        );
    }


    // =========================================================
    // 2. SEGUNDA PASADA:
    // VMT ÚNICO -> $BASETEXTURE -> PNG
    // =========================================================

    [MenuItem("Tools/TF2/Auto Link Remaining Using VMT")]
    public static void VincularRestantesUsandoVMT()
    {
        string[] materialGuids = AssetDatabase.FindAssets(
            "t:Material",
            new[] { CARPETA_MATERIALES }
        );

        Dictionary<string, List<string>> vmts =
            BuildVmtDictionary();

        Dictionary<string, string> texturesBySourcePath =
            BuildTextureSourcePathDictionary();

        int alreadyAssigned = 0;
        int linkedByVmt = 0;
        int noVmt = 0;
        int ambiguousVmt = 0;
        int noBaseTexture = 0;
        int textureNotFound = 0;

        foreach (string guid in materialGuids)
        {
            string materialPath =
                AssetDatabase.GUIDToAssetPath(guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
                continue;

            if (!material.HasProperty("_BaseMap"))
                continue;

            // Proteger lo que ya funciona
            if (material.GetTexture("_BaseMap") != null)
            {
                alreadyAssigned++;
                continue;
            }

            string materialName =
                NormalizarNombre(material.name);

            if (!vmts.TryGetValue(
                    materialName,
                    out List<string> matchingVmts))
            {
                noVmt++;

                Debug.Log(
                    $"[TF2 VMT] No se encontró VMT: {material.name}"
                );

                continue;
            }

            if (matchingVmts.Count != 1)
            {
                ambiguousVmt++;

                Debug.LogWarning(
                    $"[TF2 VMT] {material.name}: " +
                    $"{matchingVmts.Count} VMT con el mismo nombre."
                );

                continue;
            }

            string vmtPath = matchingVmts[0];

            if (!IntentarObtenerTexturaBase(
                    vmtPath,
                    out string sourceTexture))
            {
                noBaseTexture++;

                Debug.Log(
                    $"[TF2 VMT] {material.name} -> " +
                    $"VMT sin $basetexture:\n{vmtPath}"
                );

                continue;
            }

            if (!texturesBySourcePath.TryGetValue(
                    sourceTexture,
                    out string pngPath))
            {
                textureNotFound++;

                Debug.LogWarning(
                    $"[TF2 VMT] {material.name}\n" +
                    $"$basetexture: {sourceTexture}\n" +
                    "PNG NO ENCONTRADO."
                );

                continue;
            }

            AsignarTextura(material, pngPath);
            linkedByVmt++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string result =
            $"Materiales encontrados: {materialGuids.Length}\n\n" +
            $"Ya tenían textura: {alreadyAssigned}\n" +
            $"Asignados mediante VMT: {linkedByVmt}\n\n" +
            $"Sin VMT: {noVmt}\n" +
            $"VMT ambiguo: {ambiguousVmt}\n" +
            $"VMT sin $basetexture: {noBaseTexture}\n" +
            $"PNG no encontrado: {textureNotFound}";

        Debug.Log(
            "[TF2 VMT AUTO LINK RESULT]\n" +
            result
        );

        EditorUtility.DisplayDialog(
            "TF2 VMT Auto Link",
            result,
            "Aceptar"
        );
    }


    // =========================================================
    // 3. TERCERA PASADA:
    // VMT AMBIGUOS PERO TODOS APUNTAN A LA MISMA TEXTURA
    // =========================================================

    [MenuItem("Tools/TF2/Auto Link Safe Ambiguous VMT")]
    public static void VincularVMTNoAmbiguos()
    {
        string[] materialGuids = AssetDatabase.FindAssets(
            "t:Material",
            new[] { CARPETA_MATERIALES }
        );

        Dictionary<string, List<string>> vmts =
            BuildVmtDictionary();

        Dictionary<string, string> texturesBySourcePath =
            BuildTextureSourcePathDictionary();

        int alreadyAssigned = 0;
        int ambiguousChecked = 0;
        int linked = 0;

        int differentTextures = 0;
        int incomplete = 0;
        int pngNotFound = 0;

        foreach (string guid in materialGuids)
        {
            string materialPath =
                AssetDatabase.GUIDToAssetPath(guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
                continue;

            if (!material.HasProperty("_BaseMap"))
                continue;

            // No tocar los 299 ya resueltos
            if (material.GetTexture("_BaseMap") != null)
            {
                alreadyAssigned++;
                continue;
            }

            string materialName =
                NormalizarNombre(material.name);

            if (!vmts.TryGetValue(
                    materialName,
                    out List<string> candidates))
            {
                continue;
            }

            // Esta pasada SOLO trabaja con ambiguos
            if (candidates.Count <= 1)
                continue;

            ambiguousChecked++;

            HashSet<string> baseTextures =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            bool allHaveBaseTexture = true;

            foreach (string vmtPath in candidates)
            {
                if (IntentarObtenerTexturaBase(
                        vmtPath,
                        out string baseTexture))
                {
                    baseTextures.Add(baseTexture);
                }
                else
                {
                    allHaveBaseTexture = false;
                }
            }

            // Si algún candidato no tiene $basetexture,
            // no hacemos nada.
            if (!allHaveBaseTexture)
            {
                incomplete++;

                Debug.LogWarning(
                    $"[TF2 SAFE] {material.name}: " +
                    "no todos los VMT tienen $basetexture."
                );

                continue;
            }

            // Si apuntan a texturas diferentes,
            // tampoco hacemos nada.
            if (baseTextures.Count != 1)
            {
                differentTextures++;

                Debug.LogWarning(
                    $"[TF2 SAFE] {material.name}: " +
                    $"{baseTextures.Count} $basetexture diferentes. " +
                    "Se deja intacto."
                );

                continue;
            }

            // Obtener la única textura común
            string commonTexture = null;

            foreach (string texture in baseTextures)
            {
                commonTexture = texture;
                break;
            }

            if (string.IsNullOrEmpty(commonTexture))
            {
                incomplete++;
                continue;
            }

            // Buscar el PNG mediante la ruta Source
            if (!texturesBySourcePath.TryGetValue(
                    commonTexture,
                    out string pngPath))
            {
                pngNotFound++;

                Debug.LogWarning(
                    $"[TF2 SAFE] {material.name}\n" +
                    $"Textura común: {commonTexture}\n" +
                    "PNG NO ENCONTRADO."
                );

                continue;
            }

            // Ahora sí es seguro asignarlo
            AsignarTextura(
                material,
                pngPath
            );

            linked++;

            Debug.Log(
                $"[TF2 SAFE OK] {material.name}\n" +
                $"Todos los VMT apuntan a: {commonTexture}\n" +
                $"PNG: {pngPath}"
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string result =
            $"Ya tenían textura: {alreadyAssigned}\n\n" +
            $"Ambiguos analizados: {ambiguousChecked}\n" +
            $"Asignados de forma segura: {linked}\n\n" +
            $"Texturas diferentes: {differentTextures}\n" +
            $"Información incompleta: {incomplete}\n" +
            $"PNG no encontrado: {pngNotFound}";

        Debug.Log(
            "[TF2 SAFE AMBIGUOUS RESULT]\n" +
            result
        );

        EditorUtility.DisplayDialog(
            "TF2 - VMT ambiguos seguros",
            result,
            "Aceptar"
        );
    }


    // =========================================================
    // ANALIZAR AMBIGUOS SIN MODIFICAR NADA
    // =========================================================

    [MenuItem("Tools/TF2/Analyze Ambiguous VMT")]
    public static void AnalizarVMTAmbiguos()
    {
        string[] materialGuids = AssetDatabase.FindAssets(
            "t:Material",
            new[] { CARPETA_MATERIALES }
        );

        Dictionary<string, List<string>> vmts =
            BuildVmtDictionary();

        int ambiguousMaterials = 0;
        int sameTexture = 0;
        int differentTexture = 0;
        int incomplete = 0;

        foreach (string guid in materialGuids)
        {
            string materialPath =
                AssetDatabase.GUIDToAssetPath(guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
                continue;

            if (!material.HasProperty("_BaseMap"))
                continue;

            if (material.GetTexture("_BaseMap") != null)
                continue;

            string materialName =
                NormalizarNombre(material.name);

            if (!vmts.TryGetValue(
                    materialName,
                    out List<string> candidates))
            {
                continue;
            }

            if (candidates.Count <= 1)
                continue;

            ambiguousMaterials++;

            HashSet<string> baseTextures =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            int candidatesWithBaseTexture = 0;

            foreach (string vmtPath in candidates)
            {
                if (IntentarObtenerTexturaBase(
                        vmtPath,
                        out string baseTexture))
                {
                    candidatesWithBaseTexture++;
                    baseTextures.Add(baseTexture);
                }
            }

            if (candidatesWithBaseTexture != candidates.Count)
            {
                incomplete++;
            }
            else if (baseTextures.Count == 1)
            {
                sameTexture++;
            }
            else
            {
                differentTexture++;
            }
        }

        string result =
            $"Materiales VMT ambiguos: {ambiguousMaterials}\n\n" +
            $"Todos usan la misma textura: {sameTexture}\n" +
            $"Usan texturas diferentes: {differentTexture}\n" +
            $"Información incompleta: {incomplete}";

        Debug.Log(
            "[TF2 ANALISIS VMT AMBIGUOS]\n" +
            result
        );

        EditorUtility.DisplayDialog(
            "TF2 - Análisis VMT ambiguos",
            result,
            "Aceptar"
        );
    }


    // =========================================================
    // TEST GENERAL DE VMT
    // =========================================================

    [MenuItem("Tools/TF2/Test VMT Reading")]
    public static void ProbarLecturaVMT()
    {
        string[] vmtGuids =
            AssetDatabase.FindAssets(
                "",
                new[] { CARPETA_VMT }
            );

        int encontrados = 0;
        int conBaseTexture = 0;

        foreach (string guid in vmtGuids)
        {
            string ruta =
                AssetDatabase.GUIDToAssetPath(guid);

            if (!ruta.EndsWith(
                    ".vmt",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            encontrados++;

            if (IntentarObtenerTexturaBase(ruta, out _))
                conBaseTexture++;
        }

        string resultado =
            $"VMT encontrados: {encontrados}\n" +
            $"Con $basetexture: {conBaseTexture}";

        Debug.Log(
            "[TF2 VMT TEST]\n" +
            resultado
        );

        EditorUtility.DisplayDialog(
            "TF2 VMT Test",
            resultado,
            "Aceptar"
        );
    }


    // =========================================================
    // CONSTRUIR DICCIONARIO DE VMT
    // =========================================================

    private static Dictionary<string, List<string>>
        BuildVmtDictionary()
    {
        string[] vmtGuids =
            AssetDatabase.FindAssets(
                "",
                new[] { CARPETA_VMT }
            );

        Dictionary<string, List<string>> result =
            new Dictionary<string, List<string>>();

        foreach (string guid in vmtGuids)
        {
            string ruta =
                AssetDatabase.GUIDToAssetPath(guid);

            if (!ruta.EndsWith(
                    ".vmt",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string nombre =
                Path.GetFileNameWithoutExtension(ruta)
                    .ToLowerInvariant();

            if (!result.ContainsKey(nombre))
            {
                result[nombre] =
                    new List<string>();
            }

            result[nombre].Add(ruta);
        }

        return result;
    }


    // =========================================================
    // LEER $BASETEXTURE DE UN VMT
    // =========================================================

    private static bool IntentarObtenerTexturaBase(
        string vmtPath,
        out string baseTexture
    )
    {
        baseTexture = null;

        string text;

        try
        {
            text = File.ReadAllText(vmtPath);
        }
        catch
        {
            return false;
        }

        Match match = Regex.Match(
            text,
            @"""?\$basetexture""?\s+""([^""]+)""",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
            return false;

        baseTexture =
            NormalizarRutaFuente(
                match.Groups[1].Value
            );

        return !string.IsNullOrEmpty(baseTexture);
    }


    // =========================================================
    // DICCIONARIO:
    // RUTA SOURCE -> PNG UNITY
    // =========================================================

    private static Dictionary<string, string>
        BuildTextureSourcePathDictionary()
    {
        string[] textureGuids =
            AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { CARPETA_TEXTURAS }
            );

        Dictionary<string, string> result =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase
            );

        foreach (string guid in textureGuids)
        {
            string unityPath =
                AssetDatabase.GUIDToAssetPath(guid);

            string sourcePath =
                ConvertirRutaTexturaUnityAFuente(
                    unityPath
                );

            if (string.IsNullOrEmpty(sourcePath))
                continue;

            if (!result.ContainsKey(sourcePath))
            {
                result.Add(
                    sourcePath,
                    unityPath
                );
            }
        }

        return result;
    }


    // =========================================================
    // DICCIONARIO:
    // NOMBRE -> PNG
    // =========================================================

    private static Dictionary<string, List<string>>
        BuildTextureNameDictionary(
            string[] textureGuids
        )
    {
        Dictionary<string, List<string>> result =
            new Dictionary<string, List<string>>();

        foreach (string guid in textureGuids)
        {
            string ruta =
                AssetDatabase.GUIDToAssetPath(guid);

            string nombre =
                Path.GetFileNameWithoutExtension(ruta)
                    .ToLowerInvariant();

            if (!result.ContainsKey(nombre))
            {
                result[nombre] =
                    new List<string>();
            }

            result[nombre].Add(ruta);
        }

        return result;
    }


    // =========================================================
    // UNITY TEXTURE PATH -> SOURCE PATH
    // =========================================================

    private static string ConvertirRutaTexturaUnityAFuente(
        string unityPath
    )
    {
        string normalized =
            unityPath.Replace("\\", "/");

        string prefix =
            CARPETA_TEXTURAS.Replace("\\", "/") + "/";

        if (!normalized.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string relative =
            normalized.Substring(prefix.Length);

        relative =
            Path.ChangeExtension(
                relative,
                null
            );

        return NormalizarRutaFuente(relative);
    }


    // =========================================================
    // NORMALIZAR RUTA SOURCE
    // =========================================================

    private static string NormalizarRutaFuente(
        string ruta
    )
    {
        if (string.IsNullOrEmpty(ruta))
            return "";

        ruta =
            ruta.Trim()
                .Replace("\\", "/")
                .ToLowerInvariant();

        if (ruta.StartsWith("materials/"))
        {
            ruta =
                ruta.Substring(
                    "materials/".Length
                );
        }

        if (ruta.EndsWith(".vtf"))
        {
            ruta =
                ruta.Substring(
                    0,
                    ruta.Length - 4
                );
        }

        if (ruta.EndsWith(".png"))
        {
            ruta =
                ruta.Substring(
                    0,
                    ruta.Length - 4
                );
        }

        return ruta;
    }


    // =========================================================
    // NORMALIZAR NOMBRE MATERIAL
    // =========================================================

    private static string NormalizarNombre(
        string nombre
    )
    {
        nombre =
            nombre.ToLowerInvariant().Trim();

        nombre =
            nombre.Replace(
                " (instance)",
                ""
            );

        // window002.001 -> window002
        if (Regex.IsMatch(
                nombre,
                @"\.\d{3}$"))
        {
            nombre =
                Regex.Replace(
                    nombre,
                    @"\.\d{3}$",
                    ""
                );
        }

        // WOOD_WALL001 1 -> wood_wall001
        nombre =
            Regex.Replace(
                nombre,
                @"\s+\d+$",
                ""
            );

        return nombre;
    }


    // =========================================================
    // ASIGNAR TEXTURA A URP/LIT
    // =========================================================

    private static void AsignarTextura(
        Material material,
        string texturePath
    )
    {
        Texture2D texture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                texturePath
            );

        if (texture == null)
            return;

        Undo.RecordObject(
            material,
            "TF2 Auto Link Texture"
        );

        material.SetTexture(
            "_BaseMap",
            texture
        );

        EditorUtility.SetDirty(material);
    }
    // =========================================================
// LISTAR LOS VMT AMBIGUOS RESTANTES
// NO modifica ningún material.
// =========================================================

[MenuItem("Tools/TF2/List Remaining Ambiguous VMT")]
public static void ListarVMTAmbiguosRestantes()
{
    string[] materialGuids = AssetDatabase.FindAssets(
        "t:Material",
        new[] { CARPETA_MATERIALES }
    );

    Dictionary<string, List<string>> vmts =
        BuildVmtDictionary();

    int encontrados = 0;

    Debug.Log(
        "========== TF2 - VMT AMBIGUOS RESTANTES =========="
    );

    foreach (string guid in materialGuids)
    {
        string materialPath =
            AssetDatabase.GUIDToAssetPath(guid);

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                materialPath
            );

        if (material == null)
            continue;

        if (!material.HasProperty("_BaseMap"))
            continue;

        // Ignorar los 310 materiales ya resueltos
        if (material.GetTexture("_BaseMap") != null)
            continue;

        string materialName =
            NormalizarNombre(material.name);

        if (!vmts.TryGetValue(
                materialName,
                out List<string> candidates))
        {
            continue;
        }

        // Solo queremos los que tienen varios VMT
        if (candidates.Count <= 1)
            continue;

        HashSet<string> baseTextures =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        List<string> detalles =
            new List<string>();

        foreach (string vmtPath in candidates)
        {
            if (IntentarObtenerTexturaBase(
                    vmtPath,
                    out string baseTexture))
            {
                baseTextures.Add(baseTexture);

                detalles.Add(
                    $"VMT: {vmtPath}\n" +
                    $"$basetexture: {baseTexture}"
                );
            }
            else
            {
                detalles.Add(
                    $"VMT: {vmtPath}\n" +
                    "$basetexture: NO ENCONTRADO"
                );
            }
        }

        // Nos interesan solamente los que realmente
        // tienen texturas diferentes.
        if (baseTextures.Count <= 1)
            continue;

        encontrados++;

        string mensaje =
            $"\n========== MATERIAL {encontrados} ==========\n" +
            $"NOMBRE: {material.name}\n" +
            $"NORMALIZADO: {materialName}\n" +
            $"CANDIDATOS: {candidates.Count}\n\n";

        foreach (string detalle in detalles)
        {
            mensaje +=
                detalle +
                "\n\n";
        }

        Debug.Log(mensaje);
    }

    Debug.Log(
        "========== FIN ==========\n" +
        $"Materiales ambiguos diferentes encontrados: {encontrados}"
    );

    EditorUtility.DisplayDialog(
        "TF2 - VMT ambiguos restantes",
        $"Se encontraron {encontrados} materiales.\n\n" +
        "Los detalles fueron escritos en la Console.",
        "Aceptar"
    );
}
// =========================================================
// CORRECCIONES DE ALTA CONFIANZA PARA 2FORT
// Solo modifica 5 materiales pendientes.
// =========================================================

[MenuItem("Tools/TF2/Fix High Confidence 2Fort Materials")]
public static void CorregirMateriales2FortConfiables()
{
    Dictionary<string, string> fixes =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            {
                "concretefloor003",
                "concrete/concretefloor003"
            },
            {
                "oildrum001",
                "models/props_2fort/oildrum001"
            },
            {
                "window001",
                "models/props_2fort/window001"
            },
            {
                "window002",
                "models/props_2fort/window002"
            }
        };

    Dictionary<string, string> textures =
        BuildTextureSourcePathDictionary();

    string[] materialGuids =
        AssetDatabase.FindAssets(
            "t:Material",
            new[] { CARPETA_MATERIALES }
        );

    int assigned = 0;
    int alreadyAssigned = 0;
    int notFound = 0;

    foreach (string guid in materialGuids)
    {
        string materialPath =
            AssetDatabase.GUIDToAssetPath(guid);

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                materialPath
            );

        if (material == null)
            continue;

        if (!material.HasProperty("_BaseMap"))
            continue;

        string normalizedName =
            NormalizarNombre(material.name);

        // Ignorar cualquier material que no esté
        // explícitamente en nuestra lista.
        if (!fixes.TryGetValue(
                normalizedName,
                out string sourceTexture))
        {
            continue;
        }

        // No sobrescribir nada existente
        if (material.GetTexture("_BaseMap") != null)
        {
            alreadyAssigned++;

            Debug.Log(
                $"[TF2 HIGH CONFIDENCE] {material.name} " +
                "ya tenía Base Map. No se modificó."
            );

            continue;
        }

        if (!textures.TryGetValue(
                sourceTexture,
                out string pngPath))
        {
            notFound++;

            Debug.LogWarning(
                $"[TF2 HIGH CONFIDENCE] {material.name}\n" +
                $"No se encontró PNG para:\n{sourceTexture}"
            );

            continue;
        }

        AsignarTextura(
            material,
            pngPath
        );

        assigned++;

        Debug.Log(
            $"[TF2 HIGH CONFIDENCE OK]\n" +
            $"Material: {material.name}\n" +
            $"Source: {sourceTexture}\n" +
            $"PNG: {pngPath}"
        );
    }

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    string result =
        $"Asignados: {assigned}\n" +
        $"Ya tenían textura: {alreadyAssigned}\n" +
        $"PNG no encontrado: {notFound}";

    Debug.Log(
        "[TF2 HIGH CONFIDENCE RESULT]\n" +
        result
    );

    EditorUtility.DisplayDialog(
        "TF2 - High Confidence",
        result,
        "Aceptar"
    );
}
// =========================================================
// LISTAR OBJETOS QUE USAN MATERIALES SIN RESOLVER
// NO modifica absolutamente nada.
// =========================================================

[MenuItem("Tools/TF2/List Objects Using Unresolved Materials")]
public static void ListarObjetosConMaterialesSinResolver()
{
    string[] materialGuids = AssetDatabase.FindAssets(
        "t:Material",
        new[] { CARPETA_MATERIALES }
    );

    // Materiales que todavía no tienen Base Map
    HashSet<Material> unresolved =
        new HashSet<Material>();

    foreach (string guid in materialGuids)
    {
        string ruta =
            AssetDatabase.GUIDToAssetPath(guid);

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(ruta);

        if (material == null)
            continue;

        if (!material.HasProperty("_BaseMap"))
            continue;

        if (material.GetTexture("_BaseMap") == null)
        {
            unresolved.Add(material);
        }
    }


    Debug.Log(
        "========== TF2 - MATERIALES SIN RESOLVER =========="
    );

    int materialsFound = 0;


    // =====================================================
    // REVISAR CADA MATERIAL PENDIENTE
    // =====================================================

    foreach (Material material in unresolved)
    {
        List<string> users =
            new List<string>();


        // Buscar todos los Renderer cargados actualmente
        Renderer[] renderers =
            Resources.FindObjectsOfTypeAll<Renderer>();


        foreach (Renderer renderer in renderers)
        {
            // Ignorar objetos que no pertenecen a una escena
            // ni a un prefab/model cargado.
            if (renderer == null)
                continue;


            Material[] sharedMaterials =
                renderer.sharedMaterials;


            foreach (Material usedMaterial in sharedMaterials)
            {
                if (usedMaterial != material)
                    continue;


                string objectName =
                    renderer.gameObject.name;


                string hierarchy =
                    ObtenerRutaJerarquia(
                        renderer.transform
                    );


                users.Add(
                    $"OBJETO: {objectName}\n" +
                    $"TIPO: {renderer.GetType().Name}\n" +
                    $"JERARQUIA: {hierarchy}"
                );


                break;
            }
        }


        // =================================================
        // MOSTRAR RESULTADO DEL MATERIAL
        // =================================================

        materialsFound++;


        string mensaje =
            $"\n========== MATERIAL {materialsFound} ==========\n" +
            $"NOMBRE: {material.name}\n" +
            $"USADO POR: {users.Count} objeto(s)\n\n";


        if (users.Count == 0)
        {
            mensaje +=
                "No se encontró ningún Renderer cargado " +
                "utilizando este material.\n";
        }
        else
        {
            foreach (string user in users)
            {
                mensaje +=
                    user +
                    "\n\n";
            }
        }


        Debug.Log(mensaje);
    }


    // =====================================================
    // RESUMEN
    // =====================================================

    Debug.Log(
        "========== FIN ==========\n" +
        $"Materiales sin Base Map: {unresolved.Count}\n" +
        $"Materiales analizados: {materialsFound}"
    );


    EditorUtility.DisplayDialog(
        "TF2 - Materiales sin resolver",
        $"Materiales sin Base Map: {unresolved.Count}\n\n" +
        "Los objetos que utilizan cada material fueron " +
        "escritos en la Console.",
        "Aceptar"
    );
}


// =========================================================
// OBTENER RUTA COMPLETA DEL OBJETO EN LA JERARQUÍA
//
// Ejemplo:
// ctf_2fort
//   -> AUTO_prop
//      -> Body_model
// =========================================================

private static string ObtenerRutaJerarquia(
    Transform transform
)
{
    if (transform == null)
        return "";


    string ruta =
        transform.name;


    Transform parent =
        transform.parent;


    while (parent != null)
    {
        ruta =
            parent.name +
            "/" +
            ruta;

        parent =
            parent.parent;
    }


    return ruta;
}
// =========================================================
// CORREGIR MATERIALES CONFIRMADOS DE 2FORT
//
// Estos materiales fueron verificados usando:
// - prop_path de SourceIO para los props.
// - full_path de SourceIO para world_geometry.
//
// Solo modifica materiales cuyo Base Map esté vacío.
// =========================================================

[MenuItem("Tools/TF2/Fix Confirmed 2Fort Materials")]
public static void CorregirMateriales2FortConfirmados()
{
    // Material Unity -> ruta Source de la textura correcta.
    Dictionary<string, string> confirmedMaterials =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Props confirmados mediante prop_path
            { "Lamp002",   "models/props_farm/lamp002" },
            { "sign001",   "models/props_mining/sign001" },
            { "silo001",   "models/props_farm/silo001" },
            { "vent001",   "models/props_farm/vent001" },
            { "wood_pile", "models/props_farm/wood_pile" },

            // Geometría confirmada mediante full_path de SourceIO
            { "DIRTGROUND003", "nature/dirtground003" },
            { "WALL015",       "wood/wall015" },
            { "WALL028",       "metal/wall028" },
            { "WOOD_BEAM03",   "wood/wood_beam03" },
            { "blendgroundtogravel001_wvt_patch", "nature/dirtground001" }
        };


    // Source path -> ruta del PNG dentro de Unity.
    Dictionary<string, string> texturePaths =
        BuildTextureSourcePathDictionary();


    string[] materialGuids =
        AssetDatabase.FindAssets(
            "t:Material",
            new[] { CARPETA_MATERIALES }
        );


    int assigned = 0;
    int alreadyAssigned = 0;
    int textureNotFound = 0;
    int materialNotListed = 0;


    foreach (string guid in materialGuids)
    {
        string materialPath =
            AssetDatabase.GUIDToAssetPath(guid);

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                materialPath
            );


        if (material == null)
            continue;


        // -------------------------------------------------
        // Buscar si este material está en nuestra lista
        // confirmada.
        // -------------------------------------------------

        string sourceTexturePath;

        if (!confirmedMaterials.TryGetValue(
                material.name,
                out sourceTexturePath))
        {
            materialNotListed++;
            continue;
        }


        // -------------------------------------------------
        // No sobrescribir materiales ya resueltos.
        // -------------------------------------------------

        if (material.HasProperty("_BaseMap") &&
            material.GetTexture("_BaseMap") != null)
        {
            alreadyAssigned++;

            Debug.Log(
                $"[YA TENÍA TEXTURA] {material.name}"
            );

            continue;
        }


        // -------------------------------------------------
        // Buscar PNG usando la ruta Source completa.
        // -------------------------------------------------

        string normalizedSourcePath =
            NormalizarRutaFuente(sourceTexturePath);


        if (!texturePaths.TryGetValue(
                normalizedSourcePath,
                out string unityTexturePath))
        {
            textureNotFound++;

            Debug.LogWarning(
                $"[PNG NO ENCONTRADO] {material.name}\n" +
                $"Ruta Source esperada: {sourceTexturePath}"
            );

            continue;
        }


        Texture2D texture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                unityTexturePath
            );


        if (texture == null)
        {
            textureNotFound++;

            Debug.LogWarning(
                $"[NO SE PUDO CARGAR PNG] {material.name}\n" +
                $"Unity path: {unityTexturePath}"
            );

            continue;
        }


        // -------------------------------------------------
        // Asignar Base Map
        // -------------------------------------------------

        Undo.RecordObject(
            material,
            "Fix Confirmed TF2 Material"
        );


        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture(
                "_BaseMap",
                texture
            );
        }
        else
        {
            Debug.LogWarning(
                $"[SIN _BaseMap] {material.name}"
            );

            continue;
        }


        EditorUtility.SetDirty(material);

        assigned++;


        Debug.Log(
            $"[ASIGNADO] {material.name}\n" +
            $"Source: {sourceTexturePath}\n" +
            $"PNG: {unityTexturePath}"
        );
    }


    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();


    // =====================================================
    // RESULTADO
    // =====================================================

    string result =
        "========== TF2 - CONFIRMED MATERIALS ==========\n" +
        $"Asignados: {assigned}\n" +
        $"Ya tenían textura: {alreadyAssigned}\n" +
        $"PNG no encontrado: {textureNotFound}\n" +
        $"Materiales confirmados esperados: {confirmedMaterials.Count}";


    Debug.Log(result);


    EditorUtility.DisplayDialog(
        "TF2 - Materiales confirmados",
        $"Asignados: {assigned}\n" +
        $"Ya tenían textura: {alreadyAssigned}\n" +
        $"PNG no encontrado: {textureNotFound}\n\n" +
        $"Materiales confirmados: {confirmedMaterials.Count}",
        "Aceptar"
    );
}
}