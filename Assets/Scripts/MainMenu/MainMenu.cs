using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class MainMenu : MonoBehaviour
{
    public AudioSource clickSound;

    public void PlayGame()
    {
        clickSound.Play();     // 🔊 suena al instante
        StartCoroutine(PlayAndLoad());
    }

    IEnumerator PlayAndLoad()
    {
        clickSound.Play();       // 🔊 primero suena
        yield return new WaitForSeconds(0.2f); // ⏳ espera cortita
        SceneManager.LoadScene("Zone_1"); // 🎮 luego carga la escena
    }

    public void QuitGame()
    {
        clickSound.Play();
        Debug.Log("Quit the game...");
        StartCoroutine(QuitDelay());
    }
    IEnumerator QuitDelay()
    {
        yield return new WaitForSeconds(0.2f);
        Application.Quit();
    }
}
