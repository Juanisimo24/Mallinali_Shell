using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue_1", menuName = "npcDialogue")]
public class Dialogue_1 : ScriptableObject
{
    public string npcName;
    public Sprite npcPortrait;
    public Sprite playerPortrait;
    public string[] dialogueLine;
    public float typingSpeed = 0.05f;
    //public AudioClip voiceSound;
    //public float voicePitch = 1f;
    public bool[] autoProgressLine;
    public float autoProgressDelay = 1.5f;

    //Cosas que se van a activar al terminar el dialogo 

}
