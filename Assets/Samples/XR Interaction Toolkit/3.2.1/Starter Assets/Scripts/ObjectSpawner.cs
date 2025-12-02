using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// Behavior with an API for spawning objects from a given set of prefabs.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The camera that objects will face when spawned. If not set, defaults to the main camera.")]
        Camera m_CameraToFace;

        /// <summary>
        /// The camera that objects will face when spawned. If not set, defaults to the <see cref="Camera.main"/> camera.
        /// </summary>
        public Camera cameraToFace
        {
            get
            {
                EnsureFacingCamera();
                return m_CameraToFace;
            }
            set => m_CameraToFace = value;
        }

        [SerializeField]
        [Tooltip("The list of prefabs available to spawn.")]
        List<GameObject> m_ObjectPrefabs = new List<GameObject>();

        /// <summary>
        /// The list of prefabs available to spawn.
        /// </summary>
        public List<GameObject> objectPrefabs
        {
            get => m_ObjectPrefabs;
            set => m_ObjectPrefabs = value;
        }

        [SerializeField]
        [Tooltip("Optional prefab to spawn for each spawned object. Use a prefab with the Destroy Self component to make " +
            "sure the visualization only lives temporarily.")]
        GameObject m_SpawnVisualizationPrefab;

        /// <summary>
        /// Optional prefab to spawn for each spawned object.
        /// </summary>
        /// <remarks>Use a prefab with <see cref="DestroySelf"/> to make sure the visualization only lives temporarily.</remarks>
        public GameObject spawnVisualizationPrefab
        {
            get => m_SpawnVisualizationPrefab;
            set => m_SpawnVisualizationPrefab = value;
        }

        [SerializeField]
        [Tooltip("The index of the prefab to spawn. If outside the range of the list, this behavior will select " +
            "a random object each time it spawns.")]
        int m_SpawnOptionIndex = -1;

        /// <summary>
        /// The index of the prefab to spawn. If outside the range of <see cref="objectPrefabs"/>, this behavior will
        /// select a random object each time it spawns.
        /// </summary>
        /// <seealso cref="isSpawnOptionRandomized"/>
        public int spawnOptionIndex
        {
            get => m_SpawnOptionIndex;
            set => m_SpawnOptionIndex = value;
        }

        /// <summary>
        /// Whether this behavior will select a random object from <see cref="objectPrefabs"/> each time it spawns.
        /// </summary>
        /// <seealso cref="spawnOptionIndex"/>
        /// <seealso cref="RandomizeSpawnOption"/>
        public bool isSpawnOptionRandomized => m_SpawnOptionIndex < 0 || m_SpawnOptionIndex >= m_ObjectPrefabs.Count;

        [SerializeField]
        [Tooltip("Whether to only spawn an object if the spawn point is within view of the camera.")]
        bool m_OnlySpawnInView = true;

        /// <summary>
        /// Whether to only spawn an object if the spawn point is within view of the <see cref="cameraToFace"/>.
        /// </summary>
        public bool onlySpawnInView
        {
            get => m_OnlySpawnInView;
            set => m_OnlySpawnInView = value;
        }

        [SerializeField]
        [Tooltip("The size, in viewport units, of the periphery inside the viewport that will not be considered in view.")]
        float m_ViewportPeriphery = 0.15f;

        /// <summary>
        /// The size, in viewport units, of the periphery inside the viewport that will not be considered in view.
        /// </summary>
        public float viewportPeriphery
        {
            get => m_ViewportPeriphery;
            set => m_ViewportPeriphery = value;
        }

        [SerializeField]
        [Tooltip("When enabled, the object will be rotated about the y-axis when spawned by Spawn Angle Range, " +
            "in relation to the direction of the spawn point to the camera.")]
        bool m_ApplyRandomAngleAtSpawn = true;

        /// <summary>
        /// When enabled, the object will be rotated about the y-axis when spawned by <see cref="spawnAngleRange"/>
        /// in relation to the direction of the spawn point to the camera.
        /// </summary>
        public bool applyRandomAngleAtSpawn
        {
            get => m_ApplyRandomAngleAtSpawn;
            set => m_ApplyRandomAngleAtSpawn = value;
        }

        [SerializeField]
        [Tooltip("The range in degrees that the object will randomly be rotated about the y axis when spawned, " +
            "in relation to the direction of the spawn point to the camera.")]
        float m_SpawnAngleRange = 45f;

        /// <summary>
        /// The range in degrees that the object will randomly be rotated about the y axis when spawned, in relation
        /// to the direction of the spawn point to the camera.
        /// </summary>
        public float spawnAngleRange
        {
            get => m_SpawnAngleRange;
            set => m_SpawnAngleRange = value;
        }

        [SerializeField]
        [Tooltip("Whether to spawn each object as a child of this object.")]
        bool m_SpawnAsChildren;

        /// <summary>
        /// Whether to spawn each object as a child of this object.
        /// </summary>
        public bool spawnAsChildren
        {
            get => m_SpawnAsChildren;
            set => m_SpawnAsChildren = value;
        }

        [SerializeField]
        [Tooltip("Whether to allow only one spawned object at a time. If enabled, a new object cannot be spawned until the existing one is destroyed.")]
        bool m_AllowOnlyOneSpawnedObject = true;

        /// <summary>
        /// Whether to allow only one spawned object at a time.
        /// </summary>
        public bool allowOnlyOneSpawnedObject
        {
            get => m_AllowOnlyOneSpawnedObject;
            set => m_AllowOnlyOneSpawnedObject = value;
        }

        GameObject m_CurrentSpawnedObject;

        /// <summary>
        /// The currently spawned object, if any.
        /// </summary>
        public GameObject currentSpawnedObject => m_CurrentSpawnedObject;

        /// <summary>
        /// Event invoked after an object is spawned.
        /// </summary>
        /// <seealso cref="TrySpawnObject"/>
        public event Action<GameObject> objectSpawned;

        /// <summary>
        /// Event invoked when the spawned object is destroyed.
        /// </summary>
        public event Action objectDestroyed;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Awake()
        {
            EnsureFacingCamera();
        }

        void EnsureFacingCamera()
        {
            if (m_CameraToFace == null)
                m_CameraToFace = Camera.main;
        }

        /// <summary>
        /// Sets this behavior to select a random object from <see cref="objectPrefabs"/> each time it spawns.
        /// </summary>
        /// <seealso cref="spawnOptionIndex"/>
        /// <seealso cref="isSpawnOptionRandomized"/>
        public void RandomizeSpawnOption()
        {
            m_SpawnOptionIndex = -1;
        }

        /// <summary>
        /// Attempts to spawn an object from <see cref="objectPrefabs"/> at the given position. The object will have a
        /// yaw rotation that faces <see cref="cameraToFace"/>, plus or minus a random angle within <see cref="spawnAngleRange"/>.
        /// </summary>
        /// <param name="spawnPoint">The world space position at which to spawn the object.</param>
        /// <param name="spawnNormal">The world space normal of the spawn surface.</param>
        /// <returns>Returns <see langword="true"/> if the spawner successfully spawned an object. Otherwise returns
        /// <see langword="false"/>, for instance if the spawn point is out of view of the camera.</returns>
        /// <remarks>
        /// The object selected to spawn is based on <see cref="spawnOptionIndex"/>. If the index is outside
        /// the range of <see cref="objectPrefabs"/>, this method will select a random prefab from the list to spawn.
        /// Otherwise, it will spawn the prefab at the index.
        /// </remarks>
        /// <seealso cref="objectSpawned"/>
        public bool TrySpawnObject(Vector3 spawnPoint, Vector3 spawnNormal)
        {
            // 이미 소환된 오브젝트가 있고, 하나만 허용하는 경우 새로 소환하지 않음
            if (m_AllowOnlyOneSpawnedObject && m_CurrentSpawnedObject != null)
            {
                // 오브젝트가 파괴되었는지 확인
                if (m_CurrentSpawnedObject == null)
                {
                    m_CurrentSpawnedObject = null;
                }
                else
                {
                    return false;
                }
            }

            if (m_OnlySpawnInView)
            {
                var inViewMin = m_ViewportPeriphery;
                var inViewMax = 1f - m_ViewportPeriphery;
                var pointInViewportSpace = cameraToFace.WorldToViewportPoint(spawnPoint);
                if (pointInViewportSpace.z < 0f || pointInViewportSpace.x > inViewMax || pointInViewportSpace.x < inViewMin ||
                    pointInViewportSpace.y > inViewMax || pointInViewportSpace.y < inViewMin)
                {
                    return false;
                }
            }

            var objectIndex = isSpawnOptionRandomized ? Random.Range(0, m_ObjectPrefabs.Count) : m_SpawnOptionIndex;
            var newObject = Instantiate(m_ObjectPrefabs[objectIndex]);
            if (m_SpawnAsChildren)
                newObject.transform.parent = transform;

            EnsureFacingCamera();

            var facePosition = m_CameraToFace.transform.position;
            var forward = facePosition - spawnPoint;
            BurstMathUtility.ProjectOnPlane(forward, spawnNormal, out var projectedForward);
            var targetRotation = Quaternion.LookRotation(projectedForward, spawnNormal);

            // 시계 방향으로 135도 추가 회전
            targetRotation *= Quaternion.Euler(0, -215, 0);

            if (m_ApplyRandomAngleAtSpawn)
            {
                var randomRotation = Random.Range(-m_SpawnAngleRange, m_SpawnAngleRange);
                targetRotation *= Quaternion.Euler(0, randomRotation, 0);
            }

            // ArticulationBody가 있는 경우 위치 설정을 다르게 처리
            var rootArticulationBody = newObject.GetComponent<UnityEngine.ArticulationBody>();
            if (rootArticulationBody == null)
            {
                // 루트에 없으면 자식에서 루트 ArticulationBody 찾기
                var allArticulationBodies = newObject.GetComponentsInChildren<UnityEngine.ArticulationBody>();
                foreach (var ab in allArticulationBodies)
                {
                    // 부모에 ArticulationBody가 없으면 루트
                    if (ab.transform.parent == null || ab.transform.parent.GetComponent<UnityEngine.ArticulationBody>() == null)
                    {
                        rootArticulationBody = ab;
                        break;
                    }
                }
            }

            if (rootArticulationBody != null)
            {
                // ArticulationBody가 있는 경우 TeleportRoot를 사용하여 위치 설정
                rootArticulationBody.TeleportRoot(spawnPoint, targetRotation);
            }
            else
            {
                // ArticulationBody가 없는 경우 일반 Transform 사용
                newObject.transform.position = spawnPoint;
                newObject.transform.rotation = targetRotation;
            }

            if (m_SpawnVisualizationPrefab != null)
            {
                var visualizationTrans = Instantiate(m_SpawnVisualizationPrefab).transform;
                visualizationTrans.position = spawnPoint;
                visualizationTrans.rotation = newObject.transform.rotation;
            }

            // 현재 소환된 오브젝트로 설정
            m_CurrentSpawnedObject = newObject;

            // 오브젝트가 파괴될 때 추적을 위해 컴포넌트 추가
            var destroyTracker = newObject.GetComponent<SpawnedObjectDestroyTracker>();
            if (destroyTracker == null)
            {
                m_MimickingObject = Instantiate(m_MimickingPrefab);
                if (m_SpawnAsChildren)
                    m_MimickingObject.transform.parent = transform;
                
                // Check if the mimicking object has an ArticulationBody
                m_MimickingArticulationBody = m_MimickingObject.GetComponent<ArticulationBody>();
                
                // Set initial position and rotation
                Vector3 targetPosition = newObject.transform.position + m_MimickingOffset;
                Quaternion targetRotation = m_MimicRotation ? newObject.transform.rotation : m_MimickingObject.transform.rotation;
                
                if (m_MimickingArticulationBody != null)
                {
                    // Use TeleportRoot for ArticulationBody
                    m_MimickingArticulationBody.TeleportRoot(targetPosition, targetRotation);
                }
                else
                {
                    // Use regular transform for non-articulation objects
                    m_MimickingObject.transform.position = targetPosition;
                    if (m_MimicRotation)
                    {
                        m_MimickingObject.transform.rotation = targetRotation;
                    }
                }
                
                // Add PeriodicMovement component for sinusoidal joint rotation
                // Find the "so100" child GameObject which contains the articulation bodies
                Transform so100Child = m_MimickingObject.transform.Find("so100");
                GameObject targetObject = so100Child != null ? so100Child.gameObject : m_MimickingObject;
                
                PeriodicMovement periodicMovement = targetObject.GetComponent<PeriodicMovement>();
                if (periodicMovement == null)
                {
                    periodicMovement = targetObject.AddComponent<PeriodicMovement>();
                    Debug.Log($"[ObjectSpawner] Added PeriodicMovement to {targetObject.name}");
                }
                // Ensure it's enabled
                periodicMovement.enabled = true;
            }
            destroyTracker.Initialize(this);

            objectSpawned?.Invoke(newObject);
            return true;
        }

        /// <summary>
        /// Called when the spawned object is destroyed.
        /// </summary>
        internal void OnSpawnedObjectDestroyed()
        {
            m_CurrentSpawnedObject = null;
            objectDestroyed?.Invoke();
        }
    }

    /// <summary>
    /// Helper component to track when a spawned object is destroyed.
    /// </summary>
    internal class SpawnedObjectDestroyTracker : MonoBehaviour
    {
        ObjectSpawner m_Spawner;

        internal void Initialize(ObjectSpawner spawner)
        {
            m_Spawner = spawner;
        }

        void OnDestroy()
        {
            if (m_Spawner != null)
            {
                m_Spawner.OnSpawnedObjectDestroyed();
            }
        }
    }
}
