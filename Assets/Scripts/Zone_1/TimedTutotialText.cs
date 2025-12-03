using UnityEngine;
using System.Collections;

public class TimedTutorialText : MonoBehaviour
{
    [Tooltip("Cuantos segundos se queda el mensaje en pantalla")]
    public float duration = 5.0f;

    // Este método lo llamaremos desde el evento del diálogo
    public void ShowMessage()
    {
        gameObject.SetActive(true);
        
        // Reiniciamos el timer por si acaso
        StopAllCoroutines();
        StartCoroutine(HideRoutine());
    }

    public void HideMessage()
    {
        gameObject.SetActive(false);
    }

    IEnumerator HideRoutine()
    {
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);
    }
}