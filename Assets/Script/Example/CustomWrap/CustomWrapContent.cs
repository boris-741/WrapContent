using System.Collections;
using UnityEngine;
using WrapContent;

public class CustomWrapContent:  BaseWrapContent<CustomWrapItemData, WrapItem>
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
    
    protected override float GetItemSize(CustomWrapItemData data)
    {
        return 60f;
    }
}