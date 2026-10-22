
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

    private bool awaitingDecision = false;
    private bool showingDecisionResponse = false;

    private void Start()
    {
        StartDialogue();
    }

    public void StartDialogue()
    {
        if (inkFile == null ||
            dialoguePanel == null ||
            dialogueText == null ||
            choicesContainer == null ||
            choiceButtonPrefab == null)
        {
            Debug.LogError(
                "DialogueManager is missing references."
            );
            return;
        }

        story = new Story(inkFile.storyJson);

        awaitingDecision = false;
        showingDecisionResponse = false;

        OpenDialoguePanel();
        ContinueStory();
    }

    private void OpenDialoguePanel()
    {
        dialoguePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (CameraControl.Instance != null)
        {
            CameraControl.Instance.active = false;
        }
    }

    private void ContinueStory()
    {
        if (story == null)
            return;

        ClearChoices();

        while (story.canContinue)
        {
            string line = story.Continue().Trim();

            if (!string.IsNullOrEmpty(line))
            {
                dialogueText.text = line;

                // If Ink has choices ready, display them.
                if (story.currentChoices.Count > 0)
                {
                    DisplayChoices();
                }
                else if (story.canContinue)
                {
                    AddActionButton(
                        "Continue",
                        ContinueStory
                    );
                }
                else
                {
                    AddActionButton(
                        showingDecisionResponse
                            ? "Close"
                            : "Finish",
                        showingDecisionResponse
                            ? EndCustomerInteraction
                            : FinishQuestioning
                    );
                }

                return;
            }
        }

        if (story.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
        else if (showingDecisionResponse)
        {
            EndCustomerInteraction();
        }
        else
        {
            FinishQuestioning();
        }
    }

    private void DisplayChoices()
    {
        for (int i = 0; i < story.currentChoices.Count; i++)
        {
            int choiceIndex = i;

            AddActionButton(
                story.currentChoices[i].text.Trim(),
                () => SelectChoice(choiceIndex)
            );
        }
    }

    private void AddActionButton(
        string label,
        UnityEngine.Events.UnityAction action)
    {
        Button button = Instantiate(
            choiceButtonPrefab,
            choicesContainer
        );

        TMP_Text buttonText =
            button.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            buttonText.text = label;
        }

        button.onClick.AddListener(action);

        activeButtons.Add(button);
    }

    private void SelectChoice(int choiceIndex)
    {
        if (story == null)
            return;

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

    private void FinishQuestioning()
    {
        ClearChoices();

        dialoguePanel.SetActive(false);

        // Keep the Ink story and its variables alive.
        awaitingDecision = true;
        showingDecisionResponse = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (CameraControl.Instance != null)
        {
            CameraControl.Instance.active = false;
        }
    }

    public void ConfirmRefund()
    {
        ShowDecisionResponse("refund_response");
    }

    public void DenyRefund()
    {
        ShowDecisionResponse("no_refund_response");
    }

    private void ShowDecisionResponse(string path)
    {
        if (!awaitingDecision || story == null)
        {
            Debug.LogWarning(
                "No customer is awaiting a refund decision."
            );
            return;
        }

        awaitingDecision = false;
        showingDecisionResponse = true;

        story.ChoosePathString(path);

        OpenDialoguePanel();
        ContinueStory();
    }

    private void EndCustomerInteraction()
    {
        ClearChoices();

        dialoguePanel.SetActive(false);

        story = null;

        awaitingDecision = false;
        showingDecisionResponse = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (CameraControl.Instance != null)
        {
            CameraControl.Instance.active = false;
        }

        Debug.Log("Customer interaction complete.");
    }
}
