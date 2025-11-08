using UnityEngine;

public class ResiduoListGenerator : MonoBehaviour
{
    // --- Campos para conectar en el Inspector ---
    // La plantilla que usaremos para crear cada item
    public GameObject residuoItemPrefab;
    // El objeto "Content" que es el padre de todos los items
    public Transform contentParent;
    // ------------------------------------------
    
    // Ruta al JSON (debe estar en Assets/Resources/DB/residuos.json)
    public string jsonFilePath = "DB/residuos"; 

    void Start()
    {
        LoadResiduosFromJson();
    }

    void LoadResiduosFromJson()
    {
        // 1. Cargar el archivo JSON como un archivo de texto
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFilePath);

        if (jsonFile == null)
        {
            Debug.LogError("Error: No se encontró el JSON en Resources/" + jsonFilePath);
            return;
        }

        // 2. ¡EL TRUCO!
        // Tu JSON es un array ( empieza con [...] ).
        // JsonUtility de Unity solo puede leer objetos ( empiezan con {...} ).
        // Así que, manualmente "envolvemos" tu texto JSON para que sea un objeto
        // que contenga una lista llamada "residuos".
        string jsonText = "{\"residuos\":" + jsonFile.text + "}";
        // Ahora el texto se ve así: {"residuos": [ ... tu json ... ]}
        // Esto YA COINCIDE con tu clase "ResiduoList".

        // 3. Convertir el texto JSON a nuestras clases de C#
        ResiduoList residuoList = JsonUtility.FromJson<ResiduoList>(jsonText);

        if (residuoList == null || residuoList.residuos == null)
        {
            Debug.LogError("Error al deserializar el JSON. Revisa el formato.");
            return;
        }

        // 4. El bucle de creación (La Magia)
        // Por cada residuo que encontró en el JSON...
        foreach (Residuo residuo in residuoList.residuos)
        {
            // ...crea un nuevo objeto usando el prefab...
            GameObject newItem = Instantiate(residuoItemPrefab, contentParent);
            
            // ...obtiene su script "cerebro" (ResiduoItemDisplay)...
            ResiduoItemDisplay display = newItem.GetComponent<ResiduoItemDisplay>();
            
            // ...y le "entrega" los datos llamando a la función Setup.
            display.Setup(residuo);
        }
    }
}