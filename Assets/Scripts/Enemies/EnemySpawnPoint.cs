using UnityEngine;

namespace Enemies
{
    public enum EnemyType
    {
        Random,
        Crab,
        Fish,
        Mine
    }

    public class EnemySpawnPoint : MonoBehaviour
    {
        public EnemyType type = EnemyType.Random;

        [Tooltip("Rolls together with EnemySpawner.globalSpawnChance (effective = global × this).")]
        [Range(0f, 1f)]
        public float spawnProbability = 1f;
        
        // Gizmos to visualize in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.5f);
        }
    }
}
