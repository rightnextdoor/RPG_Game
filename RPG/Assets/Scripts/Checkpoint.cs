using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    private Animator anim;
    [SerializeField] private GameObject popup;
    public CheckpointData checkpointData;

    private bool playerInCheckpoint;
    public bool menuOpen;

    private void Start()
    {
        anim = GetComponent<Animator>();
        popup.gameObject.SetActive(false);
        
    }

    private void Update()
    {
        menuOpen = UIManager.instance.GetUICheckpoint().isMenuOpen;
        if (playerInCheckpoint)
        {
            if (Input.GetKeyDown(KeyCode.W) && !menuOpen)
            {
                checkpointData.position = transform.position;
                AudioManager.instance.PlaySFX("Checkpoint", transform);
                CheckpointManager.instance.ActivatedCheckpoint(checkpointData);
                CheckpointManager.instance.UpdateLastSaveCheckpoint(checkpointData);
                UIManager.instance.GetUICheckpoint().OpenMenu();
            }
        }
    }

    public void ActivateAnim()
    {
        anim.SetBool("active", true);
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
    
}
