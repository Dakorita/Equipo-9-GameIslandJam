using UnityEngine;
using TMPro;
using System.Collections;

public class VNUIManager : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    public float revealSpeed = 30f;

    private string fullText;
    private bool isRevealing;
    private Coroutine revealCoroutine;

    public bool IsRevealing => isRevealing;

    public void UpdateUI(VNCharacter character, string dialogue)
    {
        Color fixedColor = character.nameColor;
        fixedColor.a = 1f;

        nameText.text = character.characterName;
        nameText.color = fixedColor;

        StartReveal(dialogue);
    }

    void StartReveal(string text)
    {
        fullText = text;

        dialogueText.text = text;

        dialogueText.ForceMeshUpdate();

        dialogueText.maxVisibleCharacters = 0;

        if (revealCoroutine != null)
            StopCoroutine(revealCoroutine);

        revealCoroutine = StartCoroutine(RevealText());
    }

    IEnumerator RevealText()
    {
        isRevealing = true;

        dialogueText.ForceMeshUpdate();

        int totalChars = dialogueText.textInfo.characterCount;
        float timer = 0f;

        while (dialogueText.maxVisibleCharacters < totalChars)
        {
            timer += Time.deltaTime * revealSpeed;

            dialogueText.maxVisibleCharacters = Mathf.Min(
                totalChars,
                Mathf.FloorToInt(timer)
            );

            yield return null;
        }

        isRevealing = false;
    }

    public void CompleteText()
    {
        if (!isRevealing) return;

        if (revealCoroutine != null)
            StopCoroutine(revealCoroutine);

        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
        isRevealing = false;
    }
}