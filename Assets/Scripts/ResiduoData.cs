using System; // Necesario para [Serializable]

// Esto representa CADA objeto dentro de tu lista JSON
[Serializable]
public class Residuo
{
    public int id;
    public string categoria;
    public string subcategoria;
    public string nombre;
    public string material;
    public string submaterial;
    public string descripcion;
    public string keywords;
    public string ruta_imagen;
}

// Esta es una clase "ayudante" que usaremos para
// que JsonUtility pueda leer la lista.
[Serializable]
public class ResiduoList
{
    public Residuo[] residuos;
}