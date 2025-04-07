using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaSound : MonoBehaviour
{
    [SerializeField] private string areaSoundName;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            if(AudioManager.instance != null)
                AudioManager.instance.PlaySFX(areaSoundName);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            if(AudioManager.instance != null)
                AudioManager.instance.StopSFXWithFade(areaSoundName, .5f);
        }
    }
}
