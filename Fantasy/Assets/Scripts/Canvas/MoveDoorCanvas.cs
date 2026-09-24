using UnityEngine;
using UnityEngine.UI;

public class MoveDoorCanvas : MonoBehaviour
{
    [SerializeField] private PlayerHideState hideState;
    [SerializeField] private GameObject player;
    [SerializeField] GameObject doorImage;

    void Update()
    {
        FindPlayer();
        ShowImage(hideState.IsHidden);
    }

    void ShowImage(bool active)
    {
        doorImage.SetActive(active);
    }

    void FindPlayer()
    {
        if(player == null)
        {
            player = GameObject.FindWithTag("Player");
            hideState = player.GetComponent<PlayerHideState>();
        }
    }
}