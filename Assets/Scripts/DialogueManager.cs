
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("Ink Story")]
    [SerializeField] private Ink.UnityIntegration.InkFile inkFile;

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private Button choiceButtonPrefab;

    private Story story;
    private readonly List<Button> activeButtons = new();

    private void Start()
    {
        StartDialogue();
    }

    public void StartDialogue()
    {
        if (inkFile == null || dialoguePanel == null ||
            dialogueText == null || choicesContainer == null ||
            choiceButtonPrefab == null)
        {
            Debug.LogError("DialogueManager is missing references.");
            return;
        }

        story = new Story(inkFile.storyJson);
        dialoguePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (CameraControl.Instance != null)
        {
            CameraControl.Instance.active = false;
        }
        ContinueStory();
    }

    private void ContinueStory()
    {
        ClearChoices();

        while (story.canContinue)
        {
            string line = story.Continue().Trim();

            if (!string.IsNullOrEmpty(line))
            {
                dialogueText.text = line;
                break;
            }
        }

        // Finish the conversation if there is no more dialogue.
        if (!story.canContinue && story.currentChoices.Count == 0)
        {
            EndDialogue();
            return;
        }

        // Display choices only when Ink has reached them.
        if (!story.canContinue)
        {
            DisplayChoices();
        }
    }

    private void DisplayChoices()
    {
        for (int i = 0; i < story.currentChoices.Count; i++)
        {
            int choiceIndex = i;
            Button button = Instantiate(choiceButtonPrefab, choicesContainer);

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = story.currentChoices[i].text.Trim();

            button.onClick.AddListener(() => SelectChoice(choiceIndex));
            activeButtons.Add(button);
        }
    }

    private void SelectChoice(int choiceIndex)
    {
        story.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    private void ClearChoices()
    {
        foreach (Button button in activeButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
        }

        activeButtons.Clear();
    }

    private void EndDialogue()
    {
        ClearChoices();
        dialoguePanel.SetActive(false);
        story = null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (CameraControl.Instance != null)
        {
            CameraControl.Instance.active = false;
        }
    }
}
