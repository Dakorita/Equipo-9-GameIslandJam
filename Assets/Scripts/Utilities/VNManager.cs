using SignalNoise.Game;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class VNManager : MonoBehaviour
{
    public VNSceneData sceneData;
    public VNUIManager uiManager;
    public VNCharacterDisplay[] displays;
    public VNVoiceManager voiceManager;
    [SerializeField] GameStateManager gameStateManager;
    bool waitingForEventInput = false;
    [SerializeField] GameObject gridRenderer;
    [SerializeField] GameObject turnManager;
    public string nextSceneName;

    public InputActionReference nextLineAction;

    private Dictionary<VNCharacterSlot, VNCharacterDisplay> displayMap;
    private int currentLine = 0;
    private bool isPaused = false;

    void Awake()
    {
        displayMap = new Dictionary<VNCharacterSlot, VNCharacterDisplay>();

        foreach (var display in displays)
        {
            displayMap[display.slot] = display;
        }
    }

    void OnEnable()
    {
        if (nextLineAction != null)
        {
            nextLineAction.action.performed += OnNextLine;
            nextLineAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (nextLineAction != null)
        {
            nextLineAction.action.performed -= OnNextLine;
            nextLineAction.action.Disable();
        }
    }

    private void OnNextLine(InputAction.CallbackContext context)
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (isPaused) return;
        AudioManager.instance.PlaySFX("clickboton");
        if (waitingForEventInput)
        {
            waitingForEventInput = false;
            isPaused = true;

            TriggerEvent();
            return;
        }

        if (uiManager.IsRevealing)
        {
            uiManager.CompleteText();
            return;
        }
        NextLine();
    }

    void Start()
    {
        turnManager.SetActive(false);
        gridRenderer.SetActive(false);
        ShowLine();
    }

    public void NextLine()
    {
        currentLine++;

        if (currentLine >= sceneData.dialogues.Count)
        {
            Debug.Log("Fin de la escena");
            SCManager.instance.LoadScene(nextSceneName);
            return;
        }

        ShowLine();
    }

    void ShowLine()
    {
        voiceManager.StopVoice();

        var line = sceneData.dialogues[currentLine];

        HideAllCharacters();

        foreach (int charIndex in line.visibleCharacters)
        {
            var character = sceneData.characters[charIndex];

            if (displayMap.TryGetValue(character.slot, out var display))
            {
                display.ShowCharacter(character.defaultSprite);
                display.SetDimmed(true);
            }
        }

        var speakingCharacter = sceneData.characters[line.characterIndex];

        if (displayMap.TryGetValue(speakingCharacter.slot, out var speakingDisplay))
        {
            speakingDisplay.SetDimmed(false);
        }

        if (line.useLocalization && line.localizedText != null)
        {
            var handle = line.localizedText.GetLocalizedStringAsync();

            handle.Completed += op =>
            {
                uiManager.UpdateUI(speakingCharacter, op.Result);
            };
        }
        else
        {
            uiManager.UpdateUI(speakingCharacter, line.text);
        }

        voiceManager.PlayVoice(line.voiceClip);
        if (line.pauseForEvent)
        {
            waitingForEventInput = true;
        }
    }
    void TriggerEvent()
    {
        uiManager.DeactivateUI();
        Debug.Log("Ahora empieza el minijuego");
        if (gridRenderer != null)
        {
            gridRenderer.SetActive(true);
        }
        if (turnManager != null)
        {
            turnManager.SetActive(true);
        }
        if (gameStateManager != null)
        {   
            gameStateManager.StartGame(gameStateManager.ActiveCharacter);
        }
    }
    public void ResumeDialogue()
    {
        gridRenderer.SetActive(false);
        uiManager.ActivateUI();
        isPaused = false;
        NextLine();
    }

    void HideAllCharacters()
    {
        foreach (var display in displays)
        {
            display.Hide();
        }
    }
    public bool IsPaused()
    {
        return isPaused;
    }
}