using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Black_Knight_Summon_Controller : MonoBehaviour
{
    [SerializeField] private GameObject summonController;
    [SerializeField] private GameObject summonPosition;
    [SerializeField] private List<GameObject> skeletonPrefab = new List<GameObject>();
    private float summonRate;
    private int summonCount;

    private void Update()
    {
        if (IsSummonOver())
        {
            summonController.gameObject.SetActive(false);
        }
    }

    public void SetUpSummon(int numberOfSummons)
    {
        summonCount = numberOfSummons;
        summonRate = Random.Range(2, 5);
        summonController.gameObject.SetActive(true);
        StartCoroutine(SpawnEnemy());
    }


    public bool IsSummonOver()
    {
        if (summonCount > 0)
            return false;
        
        return true;
    }

    IEnumerator SpawnEnemy()
    {
        int count = summonCount;
        int enemySize = skeletonPrefab.Count;

        for (int i = 0; i < count; i++)
        {
            int enemyType = Random.Range(0, enemySize);

            SpawnSkeleton(skeletonPrefab[enemyType]);
            summonCount--;
            yield return new WaitForSeconds(summonRate);
        }
    }

    void SpawnSkeleton(GameObject enemy)
    {
        Instantiate(enemy, summonPosition.transform.position, Quaternion.identity);
    }
}
