using UnityEngine;
using System.Collections;

public class ChitinDropManager : MonoBehaviour
{
    [Header("Chitin Drop Settings")]
    [SerializeField] private GameObject chitinPrefab; // Prefab of collectible chitin object
    [SerializeField] private float scatterRadius = 1.5f; // How far to scatter chitin pieces
    [SerializeField] private float dropHeight = 0.5f; // Height above ground to drop from
    [SerializeField] private float maxDropForce = 3f; // Maximum force to apply to dropped chitin
    
    [Header("Physics Settings")]
    [SerializeField] private LayerMask groundLayers; // Layers considered as ground
    
    // Singleton pattern for easy access
    private static ChitinDropManager _instance;
    public static ChitinDropManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ChitinDropManager>();
                
                if (_instance == null)
                {
                    GameObject managerObj = new GameObject("ChitinDropManager");
                    _instance = managerObj.AddComponent<ChitinDropManager>();
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public void DropChitin(Vector3 position, int amount)
    {
        StartCoroutine(SpawnChitinPieces(position, amount));
    }
    
    private IEnumerator SpawnChitinPieces(Vector3 position, int amount)
    {
        if (chitinPrefab == null)
        {
            Debug.LogError("Chitin prefab not assigned to ChitinDropManager!");
            yield break;
        }
        
        // Spawn one piece at a time with a tiny delay to avoid physics issues
        for (int i = 0; i < amount; i++)
        {
            // Random position within scatter radius
            Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;
            Vector3 dropPosition = position + new Vector3(randomCircle.x, dropHeight, randomCircle.y);
            
            // Create the chitin piece
            GameObject chitinObject = Instantiate(chitinPrefab, dropPosition, Quaternion.Euler(0, Random.Range(0, 360), 0));
            
            // Add physics force if it has a rigidbody
            Rigidbody rb = chitinObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply random force to scatter
                Vector3 force = new Vector3(
                    Random.Range(-maxDropForce, maxDropForce),
                    Random.Range(1f, maxDropForce),
                    Random.Range(-maxDropForce, maxDropForce)
                );
                rb.AddForce(force, ForceMode.Impulse);
            }
            
            // Add collectible component if not already present
            if (chitinObject.GetComponent<ChitinCollectible>() == null)
            {
                chitinObject.AddComponent<ChitinCollectible>();
            }
            
            // Small delay between spawns
            yield return new WaitForSeconds(0.05f);
        }
    }
} 