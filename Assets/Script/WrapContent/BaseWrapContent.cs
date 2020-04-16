using UnityEngine;
using UnityEngine.EventSystems;
using Leopotam.EcsNew;

namespace WrapContent
{
    public class BaseWrapContent<T, M> : MonoBehaviour, 
                                    IBeginDragHandler, IDragHandler, IEndDragHandler,
                                    IPointerDownHandler, IPointerUpHandler
                                    where T: struct
                                    where M: MonoBehaviour, IBaseItem<T>
                                    
    {
        [SerializeField]
        public Direction wrapDirection;
        [SerializeField]
        public float itemOffset;
        
        RectTransform content_rec;
        GameObject item_go;
        BaseChildSystem<T, M> child_system;

        bool init = false;

        protected bool Init()
        {
            if(init) return true;

            if(WrapMgr.world == null)
            {
                Debug.LogErrorFormat("ecs world not ready for wrap:{0} type:{1}", gameObject.name, typeof(T));
                return false;
            }

            if(transform.childCount == 0)
            {
                Debug.LogErrorFormat("can't get content for wrap:{0} type:{1}", gameObject.name, typeof(T));
                return false;
            }

            Transform content_tr = transform.GetChild(0);
            content_rec = content_tr.GetComponent<RectTransform>();
            
            if(content_tr.childCount == 0)
            {
                Debug.LogErrorFormat("can't get content Item for wrap:{0} type:{1}", gameObject.name, typeof(T));
                return false;
            }

            item_go = content_tr.GetChild(0).gameObject;
            
            float content_size = GetContentSize(wrapDirection, content_rec);
            //create system
            child_system = new BaseChildSystem<T, M>();
            child_system.id = WrapMgr.GetNewId();
            //create config
            BaseChildSystem<T, M>.ChildSystemConfig config = new BaseChildSystem<T, M>.ChildSystemConfig();
            config.direction = wrapDirection;
            config.content_size = content_size;
            config.item_offset = itemOffset;
            config.item_go = item_go;
            config.getItemSize = GetItemSize;
            //set conf
            child_system.SetWrap(config);
            //send system
            EcsEntity entity = WrapMgr.world.NewEntity();
            ref BaseWrapCreateComponent create_cmp = ref entity.Set<BaseWrapCreateComponent>();
            create_cmp.base_cild_system = child_system;
            entity.Set<T>();
            //init flag
            init = true;

            return true;
        }

        protected virtual float GetItemSize(T data)
        {
            return 0;
        }

        float GetContentSize(Direction dir, RectTransform rec)
        {
            switch(dir)
            {
                case Direction.left:
                case Direction.right:
                    return rec.sizeDelta.x;
                case Direction.top:
                case Direction.bottom:
                    return rec.sizeDelta.y;
                default:
                    return rec.sizeDelta.x;
            }
        }

        void CreateEventComponent(EventType wrap_event, PointerEventData eventData)
        {
            EcsEntity entity = WrapMgr.world.NewEntity();
            ref BaseWrapEventComponent cmp = ref entity.Set<BaseWrapEventComponent>();
            cmp.event_type = wrap_event;
            cmp.position = eventData.position;
            cmp.start_position = eventData.pressPosition;
            cmp.delta = eventData.delta;
            entity.Set<T>();
        }

        void Start()
        {
            Init();
        }

        void OnDestroy () 
        {
            if(!init) return;
            if(WrapMgr.applicationIsQuitting) return;

            EcsEntity entity = WrapMgr.world.NewEntity();
            ref BaseWrapDestroyComponent destroy_cmp = ref entity.Set<BaseWrapDestroyComponent>();
            destroy_cmp.id = child_system.id;
        }
        
        void IBeginDragHandler.OnBeginDrag (PointerEventData event_data) 
        {
            if( !Init() ) return;

            CreateEventComponent( EventType.begin_drag, event_data );
        }

        void IDragHandler.OnDrag (PointerEventData event_data) 
        {
            if( !Init() ) return;

            CreateEventComponent( EventType.drag, event_data );
        }

        void IEndDragHandler.OnEndDrag (PointerEventData event_data) 
        {
            if( !Init() ) return;

            CreateEventComponent( EventType.end_drag, event_data );
        }

        void IPointerDownHandler.OnPointerDown (PointerEventData event_data) 
        {
            if( !Init() ) return;

            CreateEventComponent( EventType.point_down, event_data );
        }

        void IPointerUpHandler.OnPointerUp (PointerEventData event_data) 
        {
            if( !Init() ) return;

            CreateEventComponent( EventType.point_up, event_data );
        }
    }
}
