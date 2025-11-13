using UnityEngine;
using Solo.MOST_IN_ONE; // ¡EL NAMESPACE CORRECTO!

public class HapticManager : MonoBehaviour
{
    // --- ¡ESTA ES LA FUNCIÓN QUE USARÁS! ---
    // Un "tick" háptico corto y ligero.
    public void VibrateTick()
    {
        // El script del asset (MOST_HapticFeedback.cs) ya comprueba
        // si está en Android/iOS, así que no necesitamos
        // revisiones extras aquí.

        // ¡LA LLAMADA CORRECTA!
        // Llamamos a la función estática "Generate" con el tipo "LightImpact".
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.LightImpact);
    }

    // --- OTRAS OPCIONES (para el futuro) ---
    public void VibrateSuccess()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Success);
    }
    
    public void VibrateFailure()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Failure);
    }

    // (Opcional: Vibración de "Selección", a veces es más ligera)
    public void VibrateSelection()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);
    }
}