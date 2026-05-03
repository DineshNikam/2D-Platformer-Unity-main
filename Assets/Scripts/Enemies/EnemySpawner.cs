using UnityEngine;
using System.Collections.Generic;

namespace Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("Prefabs")]
        public GameObject crabPrefab;
        public GameObject fishPrefab;
        public GameObject minePrefab;

        [Header("Settings")]
        [Range(0, 1)] public float globalSpawnChance = 0.5f;
        public float collisionCheckRadius = 0.5f;
        public LayerMask obstacleLayer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SpawnEnemiesInChunk(PlatformChunk chunk)
        {
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
            if (Random.value < 0.3f) // 30% chance to spawn an extra enemy
            {
                SpawnEnemyAwarely(EnemyType.Random, chunk);
            }
        }

        public void SpawnEnemyAwarely(EnemyType type, PlatformChunk chunk)
        {
            // Get chunk bounds (roughly)
            float width = 10f; // Default chunk width
            float xOffset = Random.Range(-width/2f, width/2f);
            Vector3 startPos = chunk.transform.position + new Vector3(xOffset, 5f, 0f);

            // Raycast down to find ground
            RaycastHit2D groundHit = Physics2D.Raycast(startPos, Vector2.down, 10f, obstacleLayer);
            
            if (groundHit.collider != null)
            {
                Vector3 spawnPos = groundHit.point + Vector2.up * collisionCheckRadius;
                
                if (IsSpawnSafe(spawnPos))
                {
                    SpawnEnemy(type, spawnPos, chunk.transform);
                }
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
