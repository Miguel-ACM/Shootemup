using UnityEngine;
using System.Collections;

public class CloudSpawner : MonoBehaviour
{
    [SerializeField] private float duration = 2f;
    [SerializeField] private float delay = 0.25f;
    [SerializeField] private float horizontalSwayAmount = 1f;
    [SerializeField] private float horizontalOffsetRange = 3f;
    [SerializeField] private int cloudCount = 6;
    [SerializeField] private bool reverse = false;
    [SerializeField] private bool onSpawn = true;
    
    private Camera mainCamera;

    private void Start() {
        mainCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
        if (onSpawn) SpawnClouds(reverse);
    }

    public void SpawnClouds(bool reverse) {
        for (int i = 0; i < cloudCount; i++) {
            Debug.Log("SpawnSingleCloud1");
            Debug.Log((duration / cloudCount + i * delay).ToString());
            StartCoroutine(SpawnSingleCloud(reverse, duration / cloudCount + i * delay));
        }
    }

    private IEnumerator SpawnSingleCloud(bool reverse, float waitTime) {
        GameObject cloudPrefab = Resources.Load<GameObject>("Environment/cloud");
        GameObject cloud = Instantiate(cloudPrefab);

        // Set initial position below screen
        float randomX = Random.Range(-30, 30);
        Vector3 centerPos = mainCamera.transform.position;
        centerPos.z = 100;
        Vector3 outPos = new Vector3(mainCamera.transform.position.x + randomX,  mainCamera.transform.position.y - (reverse ? 30 : -30), 100);
        Vector3 startPos = reverse 
            ? centerPos
            : outPos;
        Vector3 endPos = reverse
            ? outPos
            : centerPos;
        
        cloud.transform.position = startPos;
        if (!reverse) cloud.transform.rotation = Quaternion.Euler(180, 0, 0);

        // Animate the cloud
        yield return new WaitForSeconds(waitTime);
        Debug.Log("SpawnSingleCloud");
        StartCoroutine(AnimateCloud(cloud, startPos, endPos, reverse));
    }

    private IEnumerator AnimateCloud(GameObject cloud, Vector3 startPos, Vector3 targetPos, bool reverse = false)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            
            // Smooth animation curve
            float smoothProgress = Mathf.SmoothStep(0, 1, normalizedTime);
            
            // Add horizontal sway using sine wave
            float horizontalOffset = Mathf.Sin(normalizedTime * Mathf.PI * 2) * horizontalSwayAmount;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, smoothProgress);
            currentPos += new Vector3(horizontalOffset, 0, 0);
            
            cloud.transform.position = currentPos;
            yield return null;
        }

        if (reverse) Destroy(cloud); // Clean up when animation is done
    }
}