using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestFunc : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
    public void TestFunction_Update()
    {
        BuildingDataConfig.Instance.EnterUpgradeMode(GameManager.Instance.currentPlayer);

    }
}
