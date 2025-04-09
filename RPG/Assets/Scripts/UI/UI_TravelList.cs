using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_TravelList : MonoBehaviour
{
    [SerializeField] private Transform travelSlotParent;
    [SerializeField] private GameObject travelSlotPrefab;

    private List<CheckpointData> checkpointList;

    public void SetupTravelList()
    {
        checkpointList = CheckpointManager.instance.GetActivatedCheckpoints();
        CreateTravelSlot();
    }

    private void CreateTravelSlot()
    {
        if (travelSlotParent.childCount > 0)
        {
            for (int i = 0; i < travelSlotParent.childCount; i++)
            {
                Destroy(travelSlotParent.GetChild(i).gameObject);
            }
        }
        if (checkpointList.Count == 0)
            return;

        for (int i = 0; i < checkpointList.Count; i++)
        {
            GameObject createSlot = Instantiate(travelSlotPrefab, travelSlotParent);
            createSlot.GetComponent<UI_TravelSlot>().SetupTravelSlot(checkpointList[i]);
        }
    }
}
