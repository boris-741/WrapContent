using Leopotam.EcsNew;
using WrapContent;
public class CustomFilterInitSystem : IEcsInitSystem
{
    //custom filter's - 1
    EcsFilter<BaseWrapDataComponent, BaseWrapDataCreateComponent, CustomWrapItemData> _data_create_filter_custom = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataAddComponent, CustomWrapItemData> _data_add_filter_custom = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataInsertComponent, CustomWrapItemData> _data_ins_filter_custom = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataRemoveComponent, CustomWrapItemData> _data_remove_filter_custom = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataUpdateComponent, CustomWrapItemData> _data_update_filter_custom = null;
    EcsFilter<BaseWrapEventComponent, CustomWrapItemData> _event_filter_custom = null;
    EcsFilter<BaseWrapScrollComponent, CustomWrapItemData> _scroll_filter_custom = null;
    //custom filter's - 2
    EcsFilter<BaseWrapDataComponent, BaseWrapDataCreateComponent, CustomWrapItemData_2> _data_create_filter_custom_2 = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataAddComponent, CustomWrapItemData_2> _data_add_filter_custom_2 = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataInsertComponent, CustomWrapItemData_2> _data_insert_filter_custom_2 = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataRemoveComponent, CustomWrapItemData_2> _data_remove_filter_custom_2 = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataUpdateComponent, CustomWrapItemData_2> _data_update_filter_custom_2 = null;
    EcsFilter<BaseWrapEventComponent, CustomWrapItemData_2> _event_filter_custom_2 = null;
    EcsFilter<BaseWrapScrollComponent, CustomWrapItemData_2> _scroll_filter_custom_2 = null;
    //custom filter's - 3
    EcsFilter<BaseWrapDataComponent, BaseWrapDataCreateComponent, CustomWrapItemData_3> _data_create_filter_custom_3 = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataAddComponent, CustomWrapItemData_3> _data_add_filter_custom_3 = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataInsertComponent, CustomWrapItemData_3> _data_insert_filter_custom_3 = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataRemoveComponent, CustomWrapItemData_3> _data_remove_filter_custom_3 = null;
    EcsFilter<BaseWrapDataComponent, BaseWrapDataUpdateComponent, CustomWrapItemData_3> _data_update_filter_custom_3 = null;
    EcsFilter<BaseWrapEventComponent, CustomWrapItemData_3> _event_filter_custom_3 = null;
    EcsFilter<BaseWrapScrollComponent, CustomWrapItemData_3> _scroll_filter_custom_3 = null;

    public void Init() {}
}