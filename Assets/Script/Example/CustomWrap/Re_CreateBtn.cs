using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Leopotam.EcsNew;
using WrapContent;

public class Re_CreateBtn : MonoBehaviour
{
    void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(RecreateLeftScroll);
    }

    void RecreateLeftScroll()
    {
        Debug.LogFormat("Re-Create Left Scroll");
        int count = 100;
        for(int i=0; i<count; i++)
        {
            CreateEventComponent(count, i, DataAction.create);
        }
        CreateScrollComponent(99, 0f, ViewAction.none);
    }

    void CreateEventComponent(int count, int index, DataAction data_act)
    {
        EcsEntity entity = WrapMgr.world.NewEntity();
        ref BaseWrapDataComponent cmp_index = ref entity.Set<BaseWrapDataComponent>();
        cmp_index.array_count = count;
        cmp_index.current_index = index;
        switch(data_act)
        {
            case DataAction.create:
                entity.Set<BaseWrapDataCreateComponent>();
            break;
            case DataAction.add:
                entity.Set<BaseWrapDataAddComponent>();
            break;
            case DataAction.remove:
                entity.Set<BaseWrapDataRemoveComponent>();
            break;
            case DataAction.update:
                entity.Set<BaseWrapDataUpdateComponent>();
            break;
        }
        ref CustomWrapItemData cmp_data = ref entity.Set<CustomWrapItemData>();
        cmp_data.str = string.Format("{0}", index + 100);
    }

    void CreateScrollComponent(int index, float tm_scroll, ViewAction view_act)
    {
        EcsEntity entity = WrapMgr.world.NewEntity();
        ref BaseWrapScrollComponent cmp_index = ref entity.Set<BaseWrapScrollComponent>();
        cmp_index.index = index;
        cmp_index.tm_scroll = tm_scroll;
        cmp_index.view_action = view_act;
        cmp_index.condition = ScrollCondition.none;
        ref CustomWrapItemData cmp_data = ref entity.Set<CustomWrapItemData>();
    }
}
