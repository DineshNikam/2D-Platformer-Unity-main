using UnityEngine;
using System.Collections.Generic;

namespace Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        private static EnemySpawner _instance;
        public static EnemySpawner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<EnemySpawner>();
                }
                return _instance;
            }
        }

        [Header("Prefabs")]
        public GameObject crabPrefab;
        public GameObject fishPrefab;
        public GameObject minePrefab;

        [Header("Settings")]
        [Range(0, 1)] public float globalSpawnChance = 0.5f;
        public float collisionCheckRadius = 0.5f;
        public LayerMask obstacleLayer = 1; // Default to Layer 0 (Default)

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void SpawnEnemiesInChunk(PlatformChunk chunk)
        {
            Debug.Log($"[Spawner] SpawnEnemiesInChunk called for {chunk.name}");
            EnemySpawnPoint[] spawnPoints = chunk.GetComponentsInChildren<EnemySpawnPoint>();
            
            foreach (var sp in spawnPoints)
            {
                if (Random.value > globalSpawnChance) continue;

                if (IsSpawnSafe(sp.transform.position))
                {
                    SpawnEnemy(sp.type, sp.transform.position, chunk.transform);
                }
            }

            // Also try to spawn some enemies "awarely" in empty spaces
            if (Random.value < 0.7f) // Increased to 70% for testing/debugging
            {
                SpawnEnemyAwarely(EnemyType.Random, chunk);
            }
        }

        public void SpawnEnemyAwarely(EnemyType type, PlatformChunk chunk)
        {
            float leftX = chunk.GetLeftX();
            float rightX = chunk.GetRightX();
            float xPos = Random.Range(leftX, rightX);
            
            // Start high to ensure we hit platforms
            Vector3 startPos = new Vector3(xPos, chunk.transform.position.y + 10f, 0f);

            // Raycast down to find ground
            RaycastHit2D groundHit = Physics2D.Raycast(startPos, Vector2.down, 20f, obstacleLayer);
            
            if (groundHit.collider != null)
            {
                Vector3 spawnPos = groundHit.point;
                // Offset upward so we don't spawn inside the ground
                spawnPos.y += collisionCheckRadius + 0.1f; 
                
                if (IsSpawnSafe(spawnPos))
                {
                    Debug.Log($"[Spawner] Aware spawn successful in {chunk.name} at {spawnPos} on {groundHit.collider.name}");
                    SpawnEnemy(type, spawnPos, chunk.transform);
                }
                else
                {
                    Debug.Log($"[Spawner] Aware spawn at {spawnPos} failed: Position not safe.");
                }
            }
            else
            {
                Debug.Log($"[Spawner] Aware spawn failed: Raycast at X:{xPos} missed everything on layer {obstacleLayer.value}.");
            }
        }

        private bool IsSpawnSafe(Vector3 position)
        {
            // Check if there are any obstacles at the spawn point
            Collider2D hit = Physics2D.OverlapCircle(position, collisionCheckRadius, obstacleLayer);
            return hit == null;
        }

        private void SpawnEnemy(EnemyType type, Vector3 position, Transform parent)
        {
            GameObject prefabToSpawn = null;

            if (type == EnemyType.Random)
            {
                int r = Random.Range(1, 4);
                type = (EnemyType)r;
            }

            switch (type)
            {
                case EnemyType.Crab:
                    prefabToSpawn = crabPrefab;
                    break;
                case EnemyType.Fish:
                    prefabToSpawn = fishPrefab;
                    break;
                case EnemyType.Mine:
                    prefabToSpawn = minePrefab;
                    break;
            }

            if (prefabToSpawn != null)
            {
                Instantiate(prefabToSpawn, position, Quaternion.identity, parent);
            }
        }

        public void ClearEnemiesInChunk(PlatformChunk chunk)
        {
            // Simple way: destroy all children with EnemyBase component
            EnemyBase[] enemies = chunk.GetComponentsInChildren<EnemyBase>();
            foreach (var enemy in enemies)
            {
                Destroy(enemy.gameObject);
            }
        }
    }
}
