using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SacredDialogueUI : MonoBehaviour
{
    public static SacredDialogueUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup; // Panel raíz
    [SerializeField] private TextMeshProUGUI textTMP; // Texto principal
    [SerializeField] private Image frameImage;        // Marco decorativo (opcional)

    [Header("Typewriter")]
    [SerializeField] private float charsPerSecond = 40f;
    [SerializeField] private KeyCode advanceKey = KeyCode.Space;

    private string[] _lines;
    private int _index;
    private bool _isShowing;
    private Coroutine _typeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        HideInstant();
    }

    public void Show(string[] lines, float autoHideDelay = 0f)
    {
        if (lines == null || lines.Length == 0) return;

        _lines = lines;
        _index = 0;
        _isShowing = true;

        StopAllCoroutines();
        StartCoroutine(RunDialogue(autoHideDelay));
    }

    private IEnumerator RunDialogue(float autoHideDelay)
    {
        ShowInstant();

        while (_index < _lines.Length)
        {
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            _typeRoutine = StartCoroutine(TypeLine(_lines[_index]));

            // Esperar a que el jugador pulse Space o se termine el tipeo y pulse una vez
            yield return new WaitUntil(() => Input.GetKeyDown(advanceKey));

            // Si el tipeo no terminó, lo forzamos a terminar
            if (_typeRoutine != null)
            {
                StopCoroutine(_typeRoutine);
                textTMP.maxVisibleCharacters = int.MaxValue; // Mostrar todo
                _typeRoutine = null;

                // Espera otra pulsación para avanzar a la siguiente línea
                yield return new WaitUntil(() => Input.GetKeyDown(advanceKey));
            }

            _index++;
        }

        if (autoHideDelay > 0f) yield return new WaitForSeconds(autoHideDelay);
        HideInstant();
        _isShowing = false;
    }

    private IEnumerator TypeLine(string line)
    {
        textTMP.text = line;
        textTMP.maxVisibleCharacters = 0;

        int total = textTMP.textInfo.characterCount;
        // Pequeño “delay” por frame para asegurar textInfo
        yield return null; 
        total = textTMP.textInfo.characterCount;

        float t = 0f;
        while (textTMP.maxVisibleCharacters < total)
        {
            t += Time.unscaledDeltaTime * charsPerSecond;
            textTMP.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(t), 0, total);
            yield return null;
        }
    }

    private void ShowInstant()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        Time.timeScale = 0f; // Pausa opcional para “momento sagrado”
    }

    private void HideInstant()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        Time.timeScale = 1f;
    }
}
