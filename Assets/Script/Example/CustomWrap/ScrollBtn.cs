using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Leopotam.EcsNew;
using WrapContent;

public class ScrollBtn : MonoBehaviour
{
    void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(ScrollLeftScroll);
    }

    void ScrollLeftScroll()
    {
        WrapMgr.CreateScrollComponent<CustomWrapItemData>(53, 0.5f, ViewAction.none);
    }
}
