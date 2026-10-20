
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ReputationManager reputationManager;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Enemy Count")]
    [SerializeField] private int minimumEnemies = 5;
    [SerializeField] private int maximumEnemies = 25;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1.5f;

    private bool battleStarted = false;

    public void StartBattle()
    {
        if (battleStarted)
            return;

        if (reputationManager == null ||
            enemyPrefab == null ||
            spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawner is missing required references.");
            return;
        }

        battleStarted = true;

        int reputation = reputationManager.CurrentReputation;

        float difficulty = Mathf.InverseLerp(100, 0, reputation);

        int enemyCount = Mathf.RoundToInt(
            Mathf.Lerp(minimumEnemies, maximumEnemies, difficulty)
        );

        Debug.Log(
            $"Battle started! Reputation: {reputation}, Enemies: {enemyCount}"
        );

        StartCoroutine(SpawnEnemies(enemyCount));
    }

    private IEnumerator SpawnEnemies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Transform spawnPoint = spawnPoints[
                Random.Range(0, spawnPoints.Length)
            ];

            Instantiate(
                enemyPrefab,
                spawnPoint.position + Vector3.up * 0.5f, //slight offset to enemy spawns above ground
                spawnPoint.rotation
            );

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
