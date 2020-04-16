using UnityEngine;
using System;
using Leopotam.EcsNew;

#if UNITY_EDITOR
using Leopotam.EcsNew.UnityIntegration;
#endif

namespace WrapContent
{
    public class WrapMgr : Singleton<WrapMgr> 
    {
        public static EcsWorld world{
            get{ 
                if( !init )
                    Debug.LogError("WrapMgr not init yet!");
                return _world; 
            }
        }
        static uint id_counter;
        static EcsWorld _world;
        static bool init = false;

        EcsSystems _systems;

        public static void Init<T>() where T:  class, IEcsInitSystem, new()
        {
            _world = new EcsWorld ();
    #if UNITY_EDITOR
            EcsWorldObserver.Create (_world);
    #endif 
            Instance._systems = new EcsSystems (_world)
                .Add (new T())
                .Add (new WrapSystem());
            Instance._systems.Init ();
            init = true;
        }

        public static uint GetNewId()
        {
            id_counter += 1;
            return id_counter;
        }

        public static void CreateDataComponent<T>(int count, int index, DataAction data_act, in T t) where T: struct
        {
            CreateDataComponent<T>(count, 0, index, data_act, in t);
        }

        public static void CreateDataComponent<T>(int count, int insert_index, int item_index, DataAction data_act, in T t) where T: struct
        {
            EcsEntity entity = world.NewEntity();
            ref BaseWrapDataComponent cmp_index = ref entity.Set<BaseWrapDataComponent>();
            cmp_index.array_count = count;
            cmp_index.ins_index = insert_index;
            cmp_index.current_index = item_index;
            switch(data_act)
            {
                case DataAction.create:
                    entity.Set<BaseWrapDataCreateComponent>();
                break;
                case DataAction.add:
                    entity.Set<BaseWrapDataAddComponent>();
                break;
                case DataAction.insert:
                    entity.Set<BaseWrapDataInsertComponent>();
                break;
                case DataAction.remove:
                    entity.Set<BaseWrapDataRemoveComponent>();
                break;
                case DataAction.update:
                    entity.Set<BaseWrapDataUpdateComponent>();
                break;
            }
            ref T cmp_data = ref entity.Set<T>();
            cmp_data = t;
        }

        public static void CreateScrollComponent<T>(int index, float tm_scroll, ViewAction view_act) where T: struct
        {
            EcsEntity entity = world.NewEntity();
            ref BaseWrapScrollComponent cmp_index = ref entity.Set<BaseWrapScrollComponent>();
            cmp_index.index = index;
            cmp_index.tm_scroll = tm_scroll;
            cmp_index.view_action = view_act;
            cmp_index.condition = ScrollCondition.none;
            ref T cmp_data = ref entity.Set<T>();
        }

        void Update()
        {
            if(!init) return;

            _systems.Run();
        }

    }
}