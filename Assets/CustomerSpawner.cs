
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using BossFight;

public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] List<GameObject> customerPrefabs;
    [SerializeField] List<GameObject> linePositions;
    [SerializeField] public List<CustomerBehaviour> activeCustomers;
    [SerializeField] GameObject witch;

    [Header("Opening Cutscene")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Ink.UnityIntegration.InkFile openingInkFile;

    int customerIndex = -1;

    [Header("Witch Cutscene")]
    [SerializeField] private Ink.UnityIntegration.InkFile witchInkFile;

    private bool witchCutsceneStarted = false;

    private void Start()
    {
        if (dialogueManager != null && openingInkFile != null)
        {
            dialogueManager.StartCutscene(openingInkFile, NewCustomer);
        }
        else
        {
            Debug.LogWarning(
                "Opening cutscene not configured. Spawning first customer normally."
            );

            NewCustomer();
        }
    }

    public void NewCustomer()
    {
        customerIndex++;

        if (customerIndex >= customerPrefabs.Count)
        {
            if (!witchCutsceneStarted)
            {
                witchCutsceneStarted = true;
                StartCoroutine(WaitForLastCustomer());
            }

            return;
        }

        var customer = customerPrefabs[customerIndex];
        print(customer.name);

        customer.gameObject.SetActive(true);
        customer.transform.position = transform.position;

        CustomerBehaviour customerBehaviour =
            customer.GetComponent<CustomerBehaviour>();

        customerBehaviour.lineIndex = activeCustomers.Count;
        activeCustomers.Add(customerBehaviour);

        print(customerBehaviour.lineIndex);
        print(linePositions[customerBehaviour.lineIndex].transform);

        customerBehaviour.agent.SetDestination(
            linePositions[customerBehaviour.lineIndex].transform.position
        );

        customerBehaviour.agent.speed = 6;
    }

    public void UpdateLinePosition()
    {
        print("Moving forward in line");

        foreach (CustomerBehaviour customerBehaviour in activeCustomers)
        {
            if (!customerBehaviour.done)
            {
                print(
                    customerBehaviour.gameObject.name +
                    "moved forward in line"
                );

                customerBehaviour.lineIndex--;

                if (customerBehaviour.lineIndex >= 0)
                {
                    customerBehaviour.SetTarget(
                        linePositions[customerBehaviour.lineIndex].transform
                    );
                }
                else
                {
                    customerBehaviour.SetTarget(
                        linePositions[0].transform
                    );
                }
            }
        }
    }

    public void WitchComes()
    {
        foreach (GameObject customer in customerPrefabs)
        {
            customer.SetActive(false);
        }

        CustomerBehaviour customerBehaviour =
            witch.GetComponent<CustomerBehaviour>();

        customerBehaviour.agent.SetDestination(
            linePositions[customerBehaviour.lineIndex].transform.position
        );

        customerBehaviour.agent.speed = 6;
    }

    private IEnumerator WaitForLastCustomer()
    {
        CustomerBehaviour lastCustomer =
            customerPrefabs[customerPrefabs.Count - 1]
                .GetComponent<CustomerBehaviour>();

        // Wait until the final customer has exited.
        yield return new WaitUntil(
            () => !lastCustomer.gameObject.activeInHierarchy
        );

        if (dialogueManager == null || witchInkFile == null)
        {
            Debug.LogError("Witch cutscene references are missing.");
            yield break;
        }

        dialogueManager.StartCutscene(
            witchInkFile,
            () =>
            {
                MoneyManager money =
                    FindFirstObjectByType<MoneyManager>();

                ReputationManager reputation =
                    FindFirstObjectByType<ReputationManager>();

                if (money == null || reputation == null)
                {
                    Debug.LogError(
                        "Cannot enter Boss Scene: missing money or reputation manager."
                    );
                    return;
                }

                // Preserve MainScene values for the boss fight.
                BossRunState.Save(
                    money.CurrentMoney,
                    reputation.CurrentReputation,
                    AngeredCustomerRegistry.Count
                );

                Debug.Log(
                    $"Witch cutscene finished. Loading Boss Scene. " +
                    $"Money: {money.CurrentMoney}, " +
                    $"Reputation: {reputation.CurrentReputation}, " +
                    $"Angered customers: {AngeredCustomerRegistry.Count}"
                );

                if (SceneLoadingManager.Instance != null)
                {
                    SceneLoadingManager.Instance.LoadSceneByName("Boss Scene");
                }
                else
                {
                    Debug.LogError(
                        "SceneLoadingManager instance is NULL!"
                    );
                }
            }
        );
    }
}
