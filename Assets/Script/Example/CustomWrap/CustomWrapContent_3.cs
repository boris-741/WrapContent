using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WrapContent;

public class CustomWrapContent_3:  BaseWrapContent<CustomWrapItemData_3, WrapItem_3>
{
    void Start()
    {
        StartCoroutine(CustomInitCor());
    }

    IEnumerator CustomInitCor()
    {
        yield return new WaitForSeconds(0.1f);
        Init();
    }
    
    protected override float GetItemSize(CustomWrapItemData_3 data)
    {
        return 134f;
    }
}