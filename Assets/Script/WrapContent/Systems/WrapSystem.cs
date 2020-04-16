using Leopotam.EcsNew;

namespace WrapContent
{
    public class WrapSystem : IEcsRunSystem
    {
        EcsWorld _world;
        EcsFilter<BaseWrapCreateComponent> _wrap_create_filter = null;
        EcsFilter<BaseWrapDestroyComponent> _wrap_destroy_filter = null;
        EcsFilter<BaseWrapEventComponent> _event_filter = null;
        EcsFilter<BaseWrapScrollComponent> _scroll_filter = null;
        EcsFilter<BaseWrapDataComponent> _data_filter = null;

        public void Run () 
        {
            //destroy wrap conent child system by id
            foreach (int d in _wrap_destroy_filter) 
            {
                ref BaseWrapDestroyComponent del_cmp = ref _wrap_destroy_filter.Get1 (d);
                uint del_id = del_cmp.id;
                foreach (int i in _wrap_create_filter) 
                {
                    ref BaseWrapCreateComponent cmp = ref _wrap_create_filter.Get1 (i);
                    if(cmp.base_cild_system.id == del_id)
                    {
                        cmp.base_cild_system.OnDestroy();
                        ref EcsEntity entity = ref _wrap_create_filter.GetEntity( i );
                        entity.Destroy();
                    }
                }
            }
            //run wrap content's
            foreach (int i in _wrap_create_filter) 
            {
                ref BaseWrapCreateComponent cmp = ref _wrap_create_filter.Get1 (i);
                cmp.base_cild_system.Run();
            }
            //clear event's
            foreach (int i in _event_filter) 
            {
                ref EcsEntity entity = ref _event_filter.GetEntity( i );
                entity.Destroy();
            }
            //clear destroy wrap conent child
            foreach (int i in _wrap_destroy_filter) 
            {
                ref EcsEntity entity = ref _wrap_destroy_filter.GetEntity( i );
                entity.Destroy();

            }
        }
    }
}