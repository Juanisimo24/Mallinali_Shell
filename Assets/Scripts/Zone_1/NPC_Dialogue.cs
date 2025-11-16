using System.Collections;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class NPC_Dialogue : MonoBehaviour
{
    public Dialogue_1 dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;

private int dialogueIndex;
private bool isTyping,isDialogueActive;
    public void beInteracted()
    {
        if (dialogueData == null || !isDialogueActive)
            return;

        if (isDialogueActive)
        {
            NextLine();
        }
        else
        {
            StartDialogue();
        }
    }

    private void OnTriggerEnter2D()
    {
        isDialogueActive=true;
        StartDialogue();
    }

    void StartDialogue()
    {
        isDialogueActive = true;
        dialogueIndex = 0;
        nameText.SetText(dialogueData.npcName);
        portraitImage.sprite = dialogueData.npcPortrait;
        dialoguePanel.SetActive(true);
        
        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(dialogueData.dialogueLine[dialogueIndex]);
            isTyping=false;
        }else if (++dialogueIndex < dialogueData.dialogueLine.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");
        if (dialogueData.autoProgressLine[dialogueIndex])
        {
            portraitImage.sprite = dialogueData.npcPortrait;
        }
        else
        {
            portraitImage.sprite = dialogueData.playerPortrait;
        }
        foreach(char letter in dialogueData.dialogueLine[dialogueIndex])
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        isTyping = false;

        if(dialogueData.autoProgressLine.Length > dialogueIndex  )
        {
            yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            NextLine();
        }
    }

    public void EndDialogue()
    {
        StopAllCoroutines();
        isDialogueActive = false;
        dialogueText.SetText("");
        dialoguePanel.SetActive(false);
        dialogueData.result();
    }


}
