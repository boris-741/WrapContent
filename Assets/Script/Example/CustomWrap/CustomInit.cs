using System.Collections;
using UnityEngine;
using Leopotam.EcsNew;
using WrapContent;

public class CustomInit : MonoBehaviour
{
    void Start()
    {
        //StartCoroutine(AddDataCor());
        //StartCoroutine(StartScrollCor());
        //StartCoroutine(StartScrollCor2());
        //StartCoroutine(UpdateCor());
        //StartCoroutine(InsertCor());
        WrapMgr.Init<CustomFilterInitSystem>();
        //init data array
        int count = 100;
        for(int i=0; i<count; i++)
        {
            WrapMgr.CreateDataComponent(count, i, DataAction.create, new CustomWrapItemData{str = string.Format("{0}", i)});
        }
        //CreateScrollComponent(99, 0f, ViewAction.none);

        //init data 2 array
        count = 100;
        int num = 0;
        for(int i=0; i<count; i++)
        {
            CreateEventComponent_2(count, i, num);
            num += 1;
            if(num > 6)
                num = 0;
        }
        
        //init data array 3
        count = 100;
        for(int i=0; i<count; i++)
        {
            WrapMgr.CreateDataComponent(    count, i, DataAction.create, 
                                            new CustomWrapItemData_3
                                                {str = string.Format("h:{0}", i)}
                                        );
        }
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
        cmp_data.str = string.Format("{0}", index);
    }

    void UpdateEventComponent(int count, int index)
    {
        EcsEntity entity = WrapMgr.world.NewEntity();
        ref BaseWrapDataComponent cmp_index = ref entity.Set<BaseWrapDataComponent>();
        cmp_index.array_count = count;
        cmp_index.current_index = index;
        entity.Set<BaseWrapDataUpdateComponent>();
        ref CustomWrapItemData cmp_data = ref entity.Set<CustomWrapItemData>();
        cmp_data.str = string.Format("{0}", index+300);
    }

    void CreateEventComponent_2(int count, int index, int num)
    {
        EcsEntity entity = WrapMgr.world.NewEntity();
        ref BaseWrapDataComponent cmp_index = ref entity.Set<BaseWrapDataComponent>();
        cmp_index.array_count = count;
        cmp_index.current_index = index;
        entity.Set<BaseWrapDataCreateComponent>();
        ref CustomWrapItemData_2 cmp_data = ref entity.Set<CustomWrapItemData_2>();
        string str = "Big Text";
        for(int i=0; i<num; i++)
        {
            str = string.Concat(str, " ", str);
        }
        cmp_data.str = str;
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

    void CreateScrollComponent_2(int index, float tm_scroll)
    {
        EcsEntity entity = WrapMgr.world.NewEntity();
        ref BaseWrapScrollComponent cmp_index = ref entity.Set<BaseWrapScrollComponent>();
        cmp_index.index = index;
        cmp_index.tm_scroll = tm_scroll;
        ref CustomWrapItemData_2 cmp_data = ref entity.Set<CustomWrapItemData_2>();
    }

    IEnumerator AddDataCor()
    {
        yield return new WaitForSeconds(2f);
        int count = 100;
        for(int i=0; i<count; i++)
        {
            CreateEventComponent(count, i, DataAction.create);
        }
    }

    IEnumerator StartScrollCor()
    {
        yield return new WaitForSeconds(3f);
        CreateEventComponent(1, 100, DataAction.add);
        CreateScrollComponent(100, 0.3f, ViewAction.after_add_view);
        Debug.Break();
    }

    IEnumerator StartScrollCor2()
    {
        yield return new WaitForSeconds(8f);
        CreateScrollComponent(10, 1.2f, ViewAction.after_add_view);
    }

    IEnumerator UpdateCor()
    {
        yield return new WaitForSeconds(1f);
        UpdateEventComponent(1, 97);
    }

    IEnumerator InsertCor()
    {
        yield return new WaitForSeconds(1f);
        int count = 10;
        string data_str;
        for(int i=0; i<count; i++)
        {
            data_str = string.Format("insert:{0}", i);
            WrapMgr.CreateDataComponent(count, 0, i, DataAction.insert, new CustomWrapItemData{str = data_str});
        }
    }
}
