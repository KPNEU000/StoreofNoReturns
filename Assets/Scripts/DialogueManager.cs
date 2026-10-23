
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
    [SerializeField] private MoneyManager moneyManager;

    private Story story;

    private readonly List<Button> activeButtons = new();

    private bool awaitingDecision = false;
    private bool showingDecisionResponse = false;

    private bool isCutscene = false;
    private System.Action onCutsceneFinished;

    private void Start()
    {
        dialoguePanel.SetActive(false);
    }

    public void StartDialogue(Ink.UnityIntegration.InkFile customerInkFile)
    {
        isCutscene = false;
        if (customerInkFile == null ||
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

        story = new Story(customerInkFile.storyJson);

        awaitingDecision = false;
        showingDecisionResponse = false;

        OpenDialoguePanel();
        ContinueStory();
    }

    public void StartCutscene(
        Ink.UnityIntegration.InkFile cutsceneInkFile,
        System.Action onFinished = null)
    {
        if (cutsceneInkFile == null ||
            dialoguePanel == null ||
            dialogueText == null ||
            choicesContainer == null ||
            choiceButtonPrefab == null)
        {
            Debug.LogError("Cutscene is missing references.");
            return;
        }

        story = new Story(cutsceneInkFile.storyJson);

        isCutscene = true;
        onCutsceneFinished = onFinished;
        awaitingDecision = false;
        showingDecisionResponse = false;

        OpenDialoguePanel();
        ContinueStory();
    }

    private void OpenDialoguePanel()
    {
        dialoguePanel.SetActive(true);
        StoreofNoReturnsSfx.Post(
            StoreofNoReturnsSfx.UiOpenDialogue,
            gameObject
        );

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
                        () =>
                        {
                            StoreofNoReturnsSfx.Post(
                                StoreofNoReturnsSfx.UiAdvance,
                                gameObject
                            );
                            ContinueStory();
                        }
                    );
                }
                else
                {
                    if (isCutscene)
                    {
                        AddActionButton(
                            "Continue",
                            () =>
                            {
                                StoreofNoReturnsSfx.Post(
                                    StoreofNoReturnsSfx.UiAdvance,
                                    gameObject
                                );
                                EndCutscene();
                            }
                        );
                    }
                    else
                    {
                        AddActionButton(
                            showingDecisionResponse ? "Close" : "Finish",
                            showingDecisionResponse
                                ? EndCustomerInteraction
                                : FinishQuestioning
                        );
                    }
                }

                return;
            }
        }

        if (story.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
        else if (isCutscene)
        {
            EndCutscene();
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
        if (story == null) return;

        StoreofNoReturnsSfx.Post(
            StoreofNoReturnsSfx.UiChoice,
            gameObject
        );

        string choiceText = story.currentChoices[choiceIndex].text.Trim();

        switch (choiceText)
        {
            case "Accuse of slight damage":
                moneyManager.ReasoningCheck("SlightlyDamaged");
                break;

            case "Accuse of a stain":
                moneyManager.ReasoningCheck("Stained");
                break;

            case "Accuse of missing accessories":
                moneyManager.ReasoningCheck("MissingAccessories");
                break;

            case "Accuse of a missing receipt":
                moneyManager.ReasoningCheck("NoReceipt");
                break;
        }

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
        StoreofNoReturnsSfx.Post(
            StoreofNoReturnsSfx.UiConfirm,
            gameObject
        );
        ShowDecisionResponse("refund_response");
    }

    public void DenyRefund()
    {
        StoreofNoReturnsSfx.Post(
            StoreofNoReturnsSfx.UiDeny,
            gameObject
        );
        ShowDecisionResponse("no_refund_response");
    }

    private void ShowDecisionResponse(string path)
    {
        if (story == null || showingDecisionResponse)
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
        StoreofNoReturnsSfx.Post(
            StoreofNoReturnsSfx.UiBack,
            gameObject
        );
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

    private void EndCutscene()
    {
        ClearChoices();
        dialoguePanel.SetActive(false);

        story = null;
        isCutscene = false;

        var callback = onCutsceneFinished;
        onCutsceneFinished = null;

        callback?.Invoke();
    }
}
