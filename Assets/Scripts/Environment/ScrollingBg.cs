using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollingBg : MonoBehaviour
{
    public Vector3 scrollVector = new Vector3(0f, -5f, 0);
    Vector3 size;
    bool spawned = false;
    GameObject ship;
    // Start is called before the first frame update
    void Start()
    {
        ship = GameObject.FindWithTag("Player");
        size = GetComponent<Renderer>().bounds.size;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(- ship.transform.position.x * 0.5f, transform.position.y + (scrollVector.y * Time.deltaTime), transform.position.z);
        
        if (transform.position.y < -20)
        {
            Destroy(gameObject);
        }
        else if (!spawned && transform.position.y < 0)
        {
            spawned = true;
            GameObject newBg = (GameObject)Instantiate(gameObject);
            newBg.transform.position = new Vector3(transform.position.x, transform.position.y + size.y, transform.position.z);
            newBg.name = gameObject.name;
        }
    }
}
