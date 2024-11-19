using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private Animator anim;
    [SerializeField] private GameObject popup;
    public string id;
    public string checkpointName;
    public bool activatedCheckpoint;
    public bool lastSavedCheckpoint;

    private bool playerInCheckpoint;

    private void Start()
    {
        anim = GetComponent<Animator>();
        popup.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerInCheckpoint)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                Debug.Log("checkpoint activated");
                ActivatedCheckpoint();
                UpdateLastSaveCheckpoint();
            }
        }
    }

    [ContextMenu("Generate checkpoint id")]
    private void GenerateId()
    {
        id = System.Guid.NewGuid().ToString(); 
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            popup.gameObject.SetActive(true);
            playerInCheckpoint = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            popup.gameObject.SetActive(false);
            playerInCheckpoint = false; 
        }
    }

    public void ActivatedCheckpoint()
    {
        if(activatedCheckpoint == false)
            AudioManager.instance.PlaySFX("Checkpoint", transform);

        activatedCheckpoint = true;
        anim.SetBool("active", true);      
    }

    public void UpdateLastSaveCheckpoint()
    {
        GameManager.instance.ClearAllSaveCheckpoint();
        lastSavedCheckpoint = true;

        UIManager.instance.GetUICheckpoint().OpenMenu();
    }
}
