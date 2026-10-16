
using UnityEngine;

public class EnemySpawnerTester : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            enemySpawner.StartBattle();
        }
    }
}
