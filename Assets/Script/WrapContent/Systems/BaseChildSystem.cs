using System;
using UnityEngine;
using Leopotam.EcsNew;

namespace WrapContent
{
    public interface IBaseChildSystem
    {
        uint id{get; set;}
        void Run(); 
        void OnDestroy();
    } 

    public interface IBaseItem<T> where T: struct
    {
        int index{set; get;} 
        void SetData(int index, ref T data);
        void SetSize(float size);
        //одноразовая акция без конттроля со стороны BaseChildSystem
        void SetAction(ViewAction action);
        //акция с контролем состороны BaseChildSystem
        void SetAction(float tm_action, float tm_cur, ViewAction action);
        void Show(bool show);
    }

    public class BaseChildSystem<T, M> :    IBaseChildSystem 
                                            where T: struct
                                            where M: MonoBehaviour, IBaseItem<T>
    {
        /// <summary>
        /// config struct
        /// </summary>
        public struct ChildSystemConfig
        {
            public Direction direction;
            public float content_size;
            public float item_offset; 
            public GameObject item_go;
            public System.Func<T, float> getItemSize;
        }
        //item info
        struct ItemInfo
        {
            public RectTransform rec_tr;
            public IBaseItem<T> item;
            public float pos;
            public float size;
            public float prev_item_pos;
            public int index;
            public bool is_deleted;
            public bool is_deleted_move;
            public bool is_show;
        }
        //scroll to data index item params
        struct ScrollToItemInfo
        {
            public float time;
            public float cur_time;
            public float from_scroll_val;
            public float to_scroll_val;
            public int to_item_index;
            public ViewAction view_action;
            public bool init_view_act;
        }
        //scroll back to wrap bound param
        struct ScrollBackToBoundInfo
        {
            public float from_scroll_val;
            public float to_scroll_val;
            public float tm_move_scroll_back;
            public float curtm_move_scroll_back;
        }
        struct MoveDelInfo
        {
            public float tm_delete;
            public float curtm_delete;
            public float tm_move_del;
            public float curtm_move_del;
        }
        //start config
        ChildSystemConfig start_config;
        //ecs
        EcsWorld world;
        EcsFilter<BaseWrapDataComponent, BaseWrapDataCreateComponent, T> _data_create_filter;
        EcsFilter<BaseWrapDataComponent, BaseWrapDataAddComponent, T> _data_add_filter;
        EcsFilter<BaseWrapDataComponent, BaseWrapDataInsertComponent, T> _data_ins_filter;
        EcsFilter<BaseWrapDataComponent, BaseWrapDataRemoveComponent, T> _data_remove_filter;
        EcsFilter<BaseWrapDataComponent, BaseWrapDataUpdateComponent, T> _data_update_filter;
        EcsFilter<BaseWrapEventComponent, T> _event_filter;
        EcsFilter<BaseWrapScrollComponent, T> _scroll_item_filter;
        //param's
        float content_size;
        float content_size_half;
        float content_size_fourth;
        float scroll_val;
        float last_scroll_val;
        float item_offset;
        Direction direction;
        Transform parent_tr;
        GameObject item_go;
        System.Func<T, float> getItemSize;
        //data array
        T[] data_arr;
        float[] size_arr;
        float total_size;
        int delta_view_count = 3;
        //cashe
        ItemInfo[] item_arr;
        float move_direction;//1 or -1 (up/down - left/right)
        float velocity;
        float deceleration_rate = 0.03f;
        //state flag
        bool init = false;
        bool is_wrap_ready = false;
        bool is_scrollable = false;
        bool is_data_ready = false;
        bool is_view_items_ready = false;
        bool is_view_items_check_add = false;
        bool is_view_items_check_create = false;
        bool is_moveback_scroll_val = false;
        bool is_movedel = false;
        bool is_item_scroll = false;
        bool is_inertia = false;
        //tmp
        Vector2 tmp_pos;
        ScrollToItemInfo scroll_item_info;
        ScrollBackToBoundInfo back_scroll_info;
        MoveDelInfo move_del_info;
        ItemInfo fake_item_view;
           
        public uint id{get; set;}

        void SetDataDefault()
        {
            is_data_ready = false;
            is_scrollable = false;
            is_view_items_check_create = false;
            is_moveback_scroll_val = false;
            is_movedel = false;
            is_inertia = false;
            velocity = 0;
            total_size = 0;

            back_scroll_info.from_scroll_val = 0;
            back_scroll_info.to_scroll_val = 0;
            move_del_info.curtm_move_del = 0;
            back_scroll_info.curtm_move_scroll_back = 0;
            move_direction = 0;
            scroll_val = 0;
            fake_item_view.index = -1;
        }

        bool Init()
        {
            if(init) return true;

            world = WrapMgr.world;
            if(world == null) return false;

            _data_create_filter = world.GetFilter<BaseWrapDataComponent, BaseWrapDataCreateComponent, T>();
            _data_add_filter = world.GetFilter<BaseWrapDataComponent, BaseWrapDataAddComponent, T>();
            _data_ins_filter = world.GetFilter<BaseWrapDataComponent, BaseWrapDataInsertComponent, T>();
            _data_remove_filter = world.GetFilter<BaseWrapDataComponent, BaseWrapDataRemoveComponent, T>();
            _data_update_filter = world.GetFilter<BaseWrapDataComponent, BaseWrapDataUpdateComponent, T>();
            _event_filter = world.GetFilter<BaseWrapEventComponent, T>();
            _scroll_item_filter = world.GetFilter<BaseWrapScrollComponent, T>();
            init = true;

            return true;
        }
        /// <summary>
        /// Set Wrap main param
        /// </summary>
        /// <param name="conf">struct param</param>
        public void SetWrap(ChildSystemConfig conf)
        {
            this.start_config = conf;
            this.direction = conf.direction;
            this.content_size = conf.content_size;
            this.item_go = conf.item_go;
            this.getItemSize = conf.getItemSize;

            parent_tr = item_go.transform.parent;
            scroll_val = 0;
            content_size_half = content_size/2f;
            content_size_fourth = content_size/4f;
            back_scroll_info.tm_move_scroll_back = 0.35f;
            is_wrap_ready = true;
        }
        
        public void Run () 
        {
            if(!Init()) return;

            //data array
            OnDataArrayFilter();
            //create view's
            CreateViews();
            //check view on re-create
            CheckViewsCreate();
            //check views on add insert;
            CheckViewsAdd();
            //event's
            OnEventFilter();
            //scroll to item
            OnScrollItemFilter();
            //pre delete action
            OnMovePreDelViews();
            //move del view's
            OnMoveDelViews();
            //move scroll to item
            OnMoveScrollItem();
            //move back scroll
            OnMoveBackScroll();
            //move inertia by velocity
            OnMoveInertia();
        }

        public void OnDestroy()
        {
            //check init
            if( !init ) return;

            ClearAllFilters();
        }

        //event's func
        void OnEventFilter()
        {
            if( _event_filter.IsEmpty() ) return;
            //move back in action
            if(is_moveback_scroll_val) return;
            //can't scroll
            if(!is_scrollable) return;
            //move delete in action
            if(is_movedel) return;

            is_inertia = false; 
            foreach (int i in _event_filter) 
            {
                ref BaseWrapEventComponent cmp = ref _event_filter.Get1(i);
                switch(cmp.event_type)
                {
                    case EventType.point_down:
                        OnPointDown(cmp.position);
                    break;
                    case EventType.drag:
                        OnDrag(cmp.delta);
                    break;
                    case EventType.end_drag:
                        OnEndDrag(cmp.delta);
                    break;
                }
            }
        }

        float GetScrollValForIndex(int index)
        {
            switch(direction)
            {
                case Direction.top:
                case Direction.right:
                    return scroll_val - (GetItemPos(index) - content_size_half + GetCasheItemSize(index)/2f);
                case Direction.bottom:
                case Direction.left:
                    return scroll_val - (GetItemPos(index) + content_size_half - GetCasheItemSize(index)/2f);
                default:
                    return scroll_val - (GetItemPos(index) - content_size_half + GetCasheItemSize(index)/2f);
            }
        }

        float GetMaxScrollVal()
        {
            switch(direction)
            {
                case Direction.top:
                case Direction.right:
                    return total_size - content_size;
                case Direction.bottom:
                case Direction.left:
                    return -(total_size - content_size);
                default:
                    return total_size - content_size;

            }
        }
        float GetMinScrollVal()
        {
            return 0;
        }

        void OnScrollItemFilter()
        {
            if(_scroll_item_filter.IsEmpty()) return;
            if(is_moveback_scroll_val) return;
            if(!is_view_items_ready) return;

            foreach (int i in _scroll_item_filter) 
            {
                ref BaseWrapScrollComponent cmp = ref _scroll_item_filter.Get1(i);
                //check start index
                if(cmp.index < 0)
                {
                    cmp.index = 0;
                }
                else if(cmp.index > data_arr.Length - 1)
                {
                    cmp.index = item_arr.Length - 1;
                }
                //start scroll
                is_item_scroll = true;
                scroll_item_info.time = cmp.tm_scroll;
                scroll_item_info.cur_time = 0f;
                scroll_item_info.init_view_act = false;
                scroll_item_info.from_scroll_val = scroll_val;
                scroll_item_info.view_action = cmp.view_action;
                scroll_item_info.to_item_index = cmp.index;
                switch(direction)
                {
                    case Direction.top:
                    case Direction.right:
                        scroll_item_info.to_scroll_val = GetScrollValForIndex(cmp.index);
                        if(scroll_item_info.to_scroll_val > total_size - content_size)
                            scroll_val = total_size - content_size;
                        else if(scroll_val < 0)
                            scroll_val = 0;

                        if(scroll_item_info.to_scroll_val > scroll_val)
                            move_direction = 1f;
                        else
                            move_direction = -1f;
                    break;
                    case Direction.bottom:
                    case Direction.left:
                        scroll_item_info.to_scroll_val = GetScrollValForIndex(cmp.index);
                        if(scroll_item_info.to_scroll_val < -(total_size - content_size))
                            scroll_val = -(total_size - content_size);
                        else if(scroll_val > 0)
                            scroll_val = 0;

                        if(scroll_item_info.to_scroll_val > scroll_val)
                            move_direction = 1f;
                        else
                            move_direction = -1f;
                    break;
                }

                if(scroll_item_info.time< Time.deltaTime)
                {
                    //immidiantly move views to index
                    //exit from loop
                    scroll_item_info.time = 0f;
                    scroll_item_info.cur_time = 0f;
                    scroll_item_info.init_view_act = false;
                    is_item_scroll = false;
                    if(cmp.index >= data_arr.Length - item_arr.Length)
                    {
                        scroll_val = GetMaxScrollVal();
                        cmp.index = data_arr.Length - item_arr.Length;
                        if(cmp.index < 0)
                            cmp.index = 0;
                        scroll_item_info.to_scroll_val = scroll_val;
                    }
                    SetPosViewsFromIndx( from_index:        cmp.index, 
                                         to_index:          scroll_item_info.to_item_index, 
                                         new_scroll_val:    scroll_item_info.to_scroll_val, 
                                         view_act:          cmp.view_action );
                    break;
                }
            }
            //clear scroll entities
            foreach (int i in _scroll_item_filter) 
            {
                ref EcsEntity entity = ref _scroll_item_filter.GetEntity( i );
                entity.Destroy();
            }
            Debug.LogFormat("BaseChildSyste: OnScrollItemFilter!!!");
        }

        /// <summary>
        /// get check scroll var vith borders
        /// </summary>
        /// <param name="val">cur var</param>
        /// <param name="in_borders">new var with check in bounds</param>
        /// <returns>new value check with borders</returns>
        float TryGetScrollVar(float val, out bool in_borders)
        {
            in_borders = true;
            switch(direction)
            {
                case Direction.top:
                case Direction.right:
                    if(val < 0)
                    {
                        in_borders = false;
                        val = 0;
                    }
                    else if(val > total_size - content_size)
                    {
                        in_borders = false;
                        val = total_size - content_size;
                        if(total_size < content_size)
                            val = 0;
                    }
                break;
                case Direction.bottom:
                case Direction.left:
                    if(val > 0)
                    {
                        in_borders = false;
                        val = 0;
                    }
                    else if(val < -(total_size - content_size))
                    {
                        in_borders = false;
                        val = -(total_size - content_size);
                        if(total_size < content_size)
                            val = 0;
                    }
                break;
            }
            return val;
        }

        void SetDefaultDelMoveItems()
        {
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                item_info.is_deleted = false;
                item_info.is_deleted_move = false;
            }
        }

        void OnMovePreDelViews()
        {
            if(!is_movedel) return;

            if(move_del_info.curtm_delete > move_del_info.tm_delete) return;
            move_del_info.curtm_delete += Time.deltaTime;
            
            int del_total_count = 0;
            if(move_del_info.curtm_delete > move_del_info.tm_delete)
                del_total_count = GetUpDeletedCount();

            int move_count = 0;
            float size_dir = GetSizeDirection();
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                if(!item_info.is_deleted) continue;

                item_info.item.SetAction(   tm_action: move_del_info.tm_delete,
                                            tm_cur: move_del_info.curtm_delete,
                                            action: ViewAction.before_delete_view);
                //clear result on del time is up
                if(move_del_info.curtm_delete > move_del_info.tm_delete)
                {
                    int next_index = GetMaxItemIndex() + 1;
                    item_info.is_deleted = false;
                    if(next_index < data_arr.Length)
                    {
                        float size = GetCasheItemSize( next_index );
                        float last_pos =  GetItemPos(next_index) + -size_dir * (item_info.size + item_offset);
                        SetViewInfoDataIndex(ref item_info, next_index);
                        SetItemPos(ref item_info, last_pos);
                        item_info.is_deleted_move = true;
                        item_info.prev_item_pos = item_info.pos;
                        item_info.pos = GetItemPos(item_info.index);
                    }
                    else if(data_arr.Length > item_arr.Length)
                    {
                        int fist_index = GetMinItemIndex() - 1;
                        SetViewInfoDataIndex(ref item_info, fist_index);
                        item_info.is_deleted_move = false;
                        SetItemPos(ref item_info, GetItemPos(item_info.index));
                    }
                    else
                    {
                        //destroy ViewItem
                        ShowItemInfo(ref item_info, false);
                    }
                    move_count += 1;
                }
            }
        }

        void OnMoveDelViews()
        {
            if(!is_movedel) return;
            if(move_del_info.curtm_delete <= move_del_info.tm_delete) return;

            move_del_info.curtm_move_del += Time.deltaTime;
            float tm_val = move_del_info.curtm_move_del/move_del_info.tm_move_del;
            float pos;
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                if(!item_info.is_deleted_move) continue;

                pos = Mathf.Lerp(item_info.prev_item_pos, item_info.pos, tm_val);
                if(move_del_info.curtm_move_del >= move_del_info.tm_move_del)
                {
                    pos = item_info.pos;
                    item_info.is_deleted_move = false;
                }
                switch(direction)
                {
                    case Direction.top:
                    case Direction.bottom:
                        tmp_pos.x = 0;
                        tmp_pos.y = pos;
                        item_info.rec_tr.anchoredPosition = tmp_pos;
                    break;
                    case Direction.left:
                    case Direction.right:
                        tmp_pos.x = pos;
                        tmp_pos.y = 0;
                        item_info.rec_tr.anchoredPosition = tmp_pos;
                    break;
                }
            }

            if(move_del_info.curtm_move_del >= move_del_info.tm_move_del)
            {
                is_movedel = false;
            }

        }

        void OnMoveScrollItem()
        {
            if(!is_item_scroll) return;

            scroll_item_info.cur_time += UnityEngine.Time.deltaTime;
            scroll_val = Mathf.Lerp(scroll_item_info.from_scroll_val, scroll_item_info.to_scroll_val, 
                            scroll_item_info.cur_time/scroll_item_info.time);

            //check scroll bounds
            bool in_borders; 
            scroll_val = TryGetScrollVar(scroll_val, out in_borders);

            OnViewsPosVal();

            if(!in_borders)
                is_item_scroll = false;
            if(scroll_item_info.cur_time >= scroll_item_info.time)
                is_item_scroll = false;

            //init action !by the end of scroll only!
            if( !scroll_item_info.init_view_act &&
                scroll_item_info.view_action != ViewAction.none
                )
            {
                bool start_show = ( !is_item_scroll ||
                                    ( is_item_scroll && 
                                      Mathf.Abs( Mathf.Abs(scroll_item_info.from_scroll_val) - 
                                                 Mathf.Abs(scroll_item_info.to_scroll_val)) < content_size)
                                  );
                if(start_show)
                {
                    IBaseItem<T> item_view = GetBaseItemByArrayIndex(scroll_item_info.to_item_index);
                    if(item_view != null)
                    {
                        item_view.SetAction(scroll_item_info.view_action);
                        scroll_item_info.init_view_act = true;
                    }
                }
            }
        }

        void OnMoveInertia()
        {
            if(is_moveback_scroll_val) return;
            if(!is_inertia) return;
            if(Mathf.Abs(velocity) < 0.001f) is_inertia = false;

            velocity *= Mathf.Pow(deceleration_rate, Time.deltaTime);
            scroll_val = scroll_val + velocity;
            bool in_borders;
            scroll_val = TryGetScrollVar(scroll_val, out in_borders);
            if(!in_borders)
                StopInertia();

            OnViewsPosVal();

            //Debug.LogFormat("BaseChildSystem: OnMoveInertia scroll_val={0}, start_scroll={1} velocity={2}", scroll_val, start_scroll, velocity);
        }

        void OnMoveBackScroll()
        {
            if(!is_moveback_scroll_val) return;
            if(is_inertia) return;

            back_scroll_info.curtm_move_scroll_back += UnityEngine.Time.deltaTime;
            scroll_val = Mathf.Lerp(back_scroll_info.from_scroll_val, back_scroll_info.to_scroll_val, 
                            back_scroll_info.curtm_move_scroll_back/back_scroll_info.tm_move_scroll_back);

            OnViewsPosVal();
            if(back_scroll_info.curtm_move_scroll_back >= back_scroll_info.tm_move_scroll_back)
                is_moveback_scroll_val = false;
            //Debug.LogFormat("BaseChildSystem: OnMoveBackScroll scroll_val={0}", scroll_val);
        }

        void OnEndDrag(Vector2 delta)
        {
            SetVelocity(delta);
            back_scroll_info.from_scroll_val = scroll_val;
            back_scroll_info.curtm_move_scroll_back = 0;
            bool in_borders;
            back_scroll_info.to_scroll_val = TryGetScrollVar(scroll_val, out in_borders);
            if(in_borders)
            {
                is_inertia = true;  
                is_moveback_scroll_val = false;
            }
            else
            {
                is_inertia = false;  
                is_moveback_scroll_val = true;
            }
        }

        void OnPointDown(Vector3 pos)
        {
            StopInertia();
        }

        void OnDrag(Vector2 delta)
        {
            StopInertia();
            GetMoveDirecton(delta);
            OnScroll(delta);
            OnViewsPosVal();
        }

        void StopInertia()
        {
            velocity = 0f;
            is_inertia = false;
        }

        void SetVelocity(Vector2 delta)
        {
            if(!is_view_items_ready) return;

            float val = 0;
            switch(direction)
            {
                case Direction.top:
                case Direction.bottom:
                    val = delta.y;
                break;
                case Direction.left:
                case Direction.right:
                    val = delta.x;
                break;
            }
            if(velocity > 0 && val < 0)
                velocity = 0;
            if(velocity < 0 && val > 0)
                velocity = 0;
            float newVelocity = val / (Time.deltaTime * 50f);
            if(Mathf.Abs(velocity) > 0.001f)
                velocity = Mathf.Lerp(velocity, newVelocity, Time.deltaTime * 10f);
            else
                velocity = newVelocity;
        }

        void OnScroll(Vector2 delta)
        {
            if(!is_view_items_ready) return;

            switch(direction)
            {
                case Direction.top:
                case Direction.bottom:
                    scroll_val += CheckDeltaScrollVal(delta.y);
                break;
                case Direction.left:
                case Direction.right:
                    scroll_val += CheckDeltaScrollVal(delta.x);
                break;
            }
        }

        float CheckDeltaScrollVal(float delta)
        {
            if(Mathf.Abs(delta) > content_size_fourth)
            {
                if(delta > 0)
                    return content_size_fourth;
                else
                    return -content_size_fourth;
            }
            else
            {
                return delta;
            }
        }

        void OnViewsPosVal()
        {
            if(!is_view_items_ready) return;

            SetViews();
            SetViewsPosVal();
        }

        void SetViewsPosVal()
        {
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                SetItemPos(ref item_info, GetItemPos(item_info.index));
            }
        }

        void SetItemSize(ref ItemInfo item_info, float size)
        {
            item_info.size = size;
            item_info.item.SetSize(size);
        }

        void SetItemPos(ref ItemInfo item_info, float pos)
        {
            item_info.pos = pos;
            switch(direction)
            {
                case Direction.top:
                case Direction.bottom:
                    tmp_pos.x = 0;
                    tmp_pos.y = pos;
                    item_info.rec_tr.anchoredPosition = tmp_pos;
                break;
                case Direction.left:
                case Direction.right:
                    tmp_pos.x = pos;
                    tmp_pos.y = 0;
                    item_info.rec_tr.anchoredPosition = tmp_pos;
                break;
            }
        }

        void GetMoveDirecton(Vector2 delta)
        {
            Vector2 ndelta = delta.normalized;
            switch(direction)
            {
                case Direction.top:
                case Direction.bottom:
                    if(ndelta.y > 0f)
                        move_direction = 1f;
                    if(ndelta.y < 0f)
                        move_direction = -1f;
                break;
                case Direction.left:
                case Direction.right:
                    if(ndelta.x > 0f)
                        move_direction = 1f;
                    if(ndelta.x < 0f)
                        move_direction = -1f;
                break;
            }
        }
        //event's func

        //array 
        void OnDataArrayFilter()
        {
            if(!is_wrap_ready) return;
            //create data array
            OnDataArrayCreate();
            //add array;
            OnDataArrayAdd();
            //insert array
            OnDataArrayIns();
            //remove data
            OnDataArrayRemove();
            //update data
            OnDataArrayUpdate();
        }

        void OnDataArrayCreate()
        {
            if(_data_create_filter.IsEmpty()) return;
            //check view
            if(is_view_items_ready)
                is_view_items_check_create = false;

            //set all to default            
            SetDataDefault();

            //set and clear data
            foreach (int i in _data_create_filter) 
            {
                ref EcsEntity entity = ref _data_create_filter.GetEntity( i );
                ref BaseWrapDataComponent cmp_info = ref _data_create_filter.Get1(i);
                if(!is_data_ready)
                {
                    //create new array
                    CreateDataArray(cmp_info.array_count);
                    is_data_ready = true;
                }
                ref T cmp_data = ref _data_create_filter.Get3(i);
                SetDataArrayItem(cmp_info.current_index, cmp_data);
                entity.Destroy();
            }
        }

        void OnDataArrayAdd()
        {
            if(!is_data_ready) return;

            if(_data_add_filter.IsEmpty()) return;

            if(is_view_items_ready)
                is_view_items_check_add = false;

            bool add_array = false;
            foreach (int i in _data_add_filter) 
            {
                ref EcsEntity entity = ref _data_add_filter.GetEntity( i );
                ref BaseWrapDataComponent cmp_info = ref _data_add_filter.Get1(i);
                if(!add_array)
                {
                    AddDataArray(cmp_info.array_count);
                    add_array = true;
                }
                ref T cmp_data = ref _data_add_filter.Get3(i);
                SetDataArrayItem(cmp_info.current_index, cmp_data);
                entity.Destroy();
            } 
        }

        void OnDataArrayIns()
        {
            if(!is_data_ready) return;

            if(_data_ins_filter.IsEmpty()) return;

            if(is_view_items_ready)
                is_view_items_check_add = false;

            int max_index = 0;
            int min_index = 0;
            if(is_view_items_ready)
            {
                max_index = GetMaxItemIndex();
                min_index = GetMinItemIndex();
            }

            bool ins_array = false;
            foreach (int i in _data_ins_filter) 
            {
                ref EcsEntity entity = ref _data_ins_filter.GetEntity( i );
                ref BaseWrapDataComponent cmp_info = ref _data_ins_filter.Get1(i);
                if(!ins_array)
                {
                    InsDataArray(cmp_info.ins_index, cmp_info.array_count);
                    ins_array = true;
                }
                ref T cmp_data = ref _data_ins_filter.Get3(i);
                SetDataArrayItem(cmp_info.current_index, cmp_data);
                //set view data
                if(is_view_items_ready)
                {
                    if( cmp_info.current_index <= max_index && 
                        cmp_info.current_index >= min_index )
                    {
                        ref ItemInfo item_info = ref GetItemViewByArrayIndex(cmp_info.current_index);
                        if(item_info.index == cmp_info.current_index)
                        {
                            SetViewInfoDataIndex(ref item_info, item_info.index);
                        }
                    }
                }
                //destroy entity
                entity.Destroy();
            } 
        }

        void OnDataArrayRemove()
        {
            //data not ready
            if(!is_data_ready) return;
            //view not ready
            if(!is_view_items_ready) return;
            //move back - can't remove
            if(is_moveback_scroll_val) return;

            if(_data_remove_filter.IsEmpty()) return;
            
            StopInertia();

            bool view_del_array = false;
            int max_index = GetMaxItemIndex();
            int min_index = GetMinItemIndex();
            foreach (int i in _data_remove_filter) 
            {
                ref EcsEntity entity = ref _data_remove_filter.GetEntity( i );
                ref BaseWrapDataComponent cmp_info = ref _data_remove_filter.Get1(i);
                RemoveDataArray(cmp_info.current_index);
                if( cmp_info.current_index <= max_index && 
                    cmp_info.current_index >= min_index )
                {
                    ref ItemInfo item_info = ref GetItemViewByArrayIndex(cmp_info.current_index);
                    if(item_info.index == cmp_info.current_index)
                    {
                        item_info.is_deleted_move = false;
                        item_info.is_deleted = true;
                        view_del_array = true;
                    }
                }
                entity.Destroy();
            }

            if(view_del_array)
            {
                //set delete data to Item Info
                int view_count = GetViewCount();
                CheckScrollable( view_count );
                int del_count;
                for(int i=0; i<item_arr.Length; i++)
                {
                    ref ItemInfo item_info = ref item_arr[i];
                    if(!item_info.is_deleted)
                    {
                        del_count = GetUpDeletedCount(item_info.index);
                        SetUpDeleteInfo(ref item_info, del_count);
                    }
                }
                move_del_info.curtm_delete = 0;
                move_del_info.tm_delete = 0.25f;
                move_del_info.curtm_move_del = 0;
                move_del_info.tm_move_del = 0.4f;
                is_movedel = true;
            }
        }

        void OnDataArrayUpdate()
        {
            if(!is_data_ready) return;

            if(_data_update_filter.IsEmpty()) return;

            int max_index = GetMaxItemIndex();
            int min_index = GetMinItemIndex();
            foreach (int i in _data_update_filter) 
            {
                ref EcsEntity entity = ref _data_update_filter.GetEntity( i );
                ref BaseWrapDataComponent cmp_info = ref _data_update_filter.Get1(i);
                ref T cmp_data = ref _data_update_filter.Get3(i);
                UpdateDataArrayItem(cmp_info.current_index, cmp_data);
                if( is_view_items_ready &&
                    cmp_info.current_index <= max_index && 
                    cmp_info.current_index >= min_index )
                {
                    ref ItemInfo item_info = ref GetItemViewByArrayIndex(cmp_info.current_index);
                    if(item_info.index == cmp_info.current_index)
                    {
                        //Debug.LogFormat("Update data index={0}", cmp_info.current_index);
                        SetViewInfoDataIndex(ref item_info, cmp_info.current_index);
                    }
                }
                entity.Destroy();
            }
            //update view
            OnViewsPosVal();
        }

        void ClearAllFilters()
        {
            _data_create_filter.ClearSimple();
            _data_add_filter.ClearSimple();
            _data_ins_filter.ClearSimple();
            _data_remove_filter.ClearSimple();
            _data_update_filter.ClearSimple();
            _event_filter.ClearSimple();
            _scroll_item_filter.ClearSimple();
        }

        void SetUpDeleteInfo(ref ItemInfo item_info, int del_count)
        {
            if(del_count > 0)
            {
                item_info.is_deleted = false;
                item_info.is_deleted_move = true;
                item_info.prev_item_pos = item_info.pos;
                item_info.index = item_info.index - del_count;
                item_info.item.index = item_info.index;
                item_info.pos = GetItemPos(item_info.index);
            }
        }

        void CreateDataArray(int count)
        {
            data_arr = new T[count];
            size_arr = new float[count];
        }

        void AddDataArray(int addcount)
        {
            if(is_data_ready)
            {
                data_arr = ArrayTool.ResizeArray(data_arr, addcount);
                size_arr = ArrayTool.ResizeArray(size_arr, addcount);
            }
            else
            {
                Debug.LogErrorFormat("BaseChildSystem: add data without create type={0}", typeof(T));
            }
        }

        void InsDataArray(int index, int inscount)
        {
            if(is_data_ready)
            {
                data_arr = ArrayTool.InsertArray(data_arr, index, inscount);
                size_arr = ArrayTool.InsertArray(size_arr, index, inscount);
            }
            else
            {
                Debug.LogErrorFormat("BaseChildSystem: add data without create type={0}", typeof(T));
            }
        }

        void RemoveDataArray(int index)
        {
            if(is_data_ready)
            {
                float size = size_arr[index];
                data_arr = ArrayTool.RemoveFromArray(data_arr, index);
                size_arr = ArrayTool.RemoveFromArray(size_arr, index);
                total_size -= size;
            }
            else
            {
                Debug.LogErrorFormat("BaseChildSystem: add data without create type={0}", typeof(T));
            }
        }

        void SetDataArrayItem(int index, T data)
        {
            float size = getItemSize( data );
            data_arr[index] = data;
            size_arr[index] = size;
            total_size += size;
        }

        void UpdateDataArrayItem(int index, T data)
        {
            float size = getItemSize( data );
            data_arr[index] = data;
            float old_size = size_arr[index];
            size_arr[index] = size;
            total_size += size - old_size;
        }
        //array 
        
        //view's
        void CheckScrollable(int view_count)
        {
            if(view_count - delta_view_count > data_arr.Length)
            {
                is_scrollable = false;
            }
            else
            {
                is_scrollable = true;
            }
        }

        void CreateViews()
        {
            if(!is_data_ready) return;
            if(is_view_items_ready) return;

            int view_count = GetViewCount();
            CheckScrollable( view_count );
            //check data count < view_count
            if(view_count > data_arr.Length)
            {
                view_count = data_arr.Length;
            }
            item_arr = new ItemInfo[view_count];
            for(int i=0; i<view_count; i++)
            {
                AddViewInfo(i, i);
            }
            //set pos size
            SetViewsPosVal();
            item_go.SetActive(false);
            is_view_items_ready = true;
            is_view_items_check_add = true;
            is_view_items_check_create = true;
        }

        /// <summary>
        /// re-create array check
        /// </summary>
        void CheckViewsCreate()
        {
            if(!is_data_ready) return;
            if(is_view_items_check_create) return;

            int view_count = GetViewCount();
            CheckScrollable( view_count );
            int ready_view_count = item_arr.Length;
            int data_count = data_arr.Length;
            if(ready_view_count < view_count)
            {
                int resize_count = view_count - ready_view_count;
                item_arr = ArrayTool.ResizeArray(item_arr, resize_count);
                for(int i=item_arr.Length - 1; i>=item_arr.Length - resize_count; i--)
                {
                    //create empty view
                    AddViewInfo(-1, i);
                }
            }
            for(int i=0; i<view_count; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                SetViewInfoDataIndex(ref item_info, i);
            }
            //set pos size
            SetViewsPosVal();
            //set flag's
            is_view_items_check_add = true;
            is_view_items_check_create = true;
        }
        
        /// <summary>
        /// check for add or insert data !not remove!(remove only single row)
        /// </summary>
        void CheckViewsAdd()
        {
            if(!is_data_ready) return;
            if(is_view_items_check_add) return;

            int view_count = GetViewCount();
            CheckScrollable( view_count );
            int ready_view_count = item_arr.Length;
            int data_count = data_arr.Length;
            if(view_count > data_count)
                view_count = data_count;
            //кол-во готовых не достаточно, но данных больше чем виевов
            if(ready_view_count < view_count)
            {
                int resize_count = view_count - ready_view_count;
                item_arr = ArrayTool.ResizeArray(item_arr, resize_count);
                for(int i=0; i<resize_count; i++)
                {
                    int data_index = GetMaxItemIndex() + 1;
                    if(data_index >=  data_count)
                        data_index = GetMinItemIndex() - 1;
                    int view_array_index = ready_view_count + i;
                    AddViewInfo(data_index, view_array_index);
                }
            }
            SetViewsPosVal();
            is_view_items_check_add = true;
            is_view_items_check_create = true;
        }

        void AddViewInfo(int data_index, int view_array_index)
        {
            if(view_array_index >= item_arr.Length)
            {
                Debug.LogErrorFormat("BaseChildSystem: AddViewInfo view_array_index out of bounds type:{0}", typeof(T));
                return;
            }

            Type type_m = typeof(M);
            GameObject go = GameObject.Instantiate( item_go );
            go.SetActive(true);
            Transform tr = go.transform;
            tr.SetParent(parent_tr, false);
            ItemInfo item_info =  new ItemInfo();
            item_info.rec_tr = (RectTransform)tr;
            item_info.item = (IBaseItem<T>)go.GetComponent(type_m);
            if(item_info.item == null)
                Debug.LogErrorFormat("BaseChildSystem: can't get MonoBehaviour item component: {0}", type_m.ToString());
            SetViewInfoDataIndex(ref item_info, data_index);
            item_arr[view_array_index] = item_info;
        }

        bool SetViewInfoDataIndex(ref ItemInfo item_info, int data_index)
        {
            if(data_index < 0 || data_index > data_arr.Length - 1) return false;

            item_info.index = data_index;
            item_info.item.SetData(item_info.index, ref data_arr[item_info.index]);
            SetItemSize( ref item_info, GetCasheItemSize(item_info.index) );
            return true;
        }

        void SetPosViewsFromIndx(int from_index, int to_index, float new_scroll_val, ViewAction view_act)
        {
            //create start view from index
            int next_index;
            for(int i=0; i<item_arr.Length; i++)
            {
                next_index = from_index + i;
                if(next_index > data_arr.Length - 1 || next_index < 0)
                {
                    UnityEngine.Debug.LogErrorFormat("BaseChildSystem: SetPosViewsFromIndx index out of bounds array index:{0} from_index:{1}", 
                                                        next_index, from_index);
                    continue;
                }
                ref ItemInfo item_info = ref item_arr[i];
                SetViewInfoDataIndex(ref item_info, next_index);
                if(next_index == to_index)
                    item_info.item.SetAction(view_act );
            }
            //set new scroll val
            scroll_val = new_scroll_val;
            //set pos size
            SetViewsPosVal();
        }

        float GetMinCasheViewSize()
        {
            float size = 0f;
            float cur = 0f;
            for(int i=0; i<size_arr.Length; i++)
            {
                cur = size_arr[i];
                if(i == 0 || cur < size)
                    size = cur;
            }
            return size;
        }

        float GetCasheItemSize(int index)
        {
            if(index < 0) return 0;
            if(index > size_arr.Length - 1) return 0;
            if(size_arr.Length == 0) return 0;

            return size_arr[index];
        }

        int GetViewCount()
        {
            float min_size = GetMinCasheViewSize();
            if(min_size < 0.0001f) return 0;

            int view_count = Mathf.CeilToInt(content_size/(min_size + item_offset));
            delta_view_count = Mathf.CeilToInt(view_count/2f);
            if(delta_view_count < 3)
                delta_view_count = 3;

            return view_count + delta_view_count;
        }
        //view's

        float GetSizeDirection()
        {
            switch(direction)
            {
                case Direction.top:
                case Direction.right:
                    return 1f;
                case Direction.bottom:
                case Direction.left:
                    return -1f;
                default:
                    return 0;
            }
        }

        //move view's
        float GetItemPos(int index)
        {
            float dir = GetSizeDirection();
            float fist_size_half = GetCasheItemSize(0)/2f;
            float cur_size_half = 0;
            float pos = dir * (content_size_half - fist_size_half);
            for(int i = 1; i<=index; i++)
            {
                cur_size_half = GetCasheItemSize( i ) / 2f;
                pos += dir * -(fist_size_half + cur_size_half + item_offset);
                fist_size_half = cur_size_half;
            }
            return pos + scroll_val;
        }
        /// <summary>
        /// set view index by syze out of content one by one
        /// </summary>
        void SetViews()
        {
            float size;
            float max_dist;
            int last_index;
            int next_index;
            switch(direction)
            {
                case Direction.top:
                case Direction.right:
                    for(int i=0; i<item_arr.Length; i++)
                    {
                        ref ItemInfo item_info = ref item_arr[i];
                        size = GetCasheItemSize(item_info.index);
                        max_dist = move_direction * (content_size_half + size/2f);
                        if(move_direction > 0 && item_info.pos > max_dist)//up move
                        {
                            last_index = GetMaxItemIndex();
                            next_index = last_index + 1;
                            if( !SetViewInfoDataIndex(ref item_info, next_index) )
                            {
                                CorrectFromIndex(last_index, -1);
                                break;
                            }
                        }
                        else if(move_direction < 0 && item_info.pos < max_dist)
                        {
                            last_index = GetMinItemIndex();
                            next_index = last_index - 1;
                            if( !SetViewInfoDataIndex(ref item_info, next_index) )
                            {
                                CorrectFromIndex(last_index, 1);
                                break;
                            }
                        }
                    }
                break;
                case Direction.bottom:
                case Direction.left:
                    for(int i=0; i<item_arr.Length; i++)
                    {
                        ref ItemInfo item_info = ref item_arr[i];
                        size = GetCasheItemSize(item_info.index);
                        max_dist = move_direction * (content_size_half + size/2f);
                        if(move_direction < 0 && item_info.pos < max_dist)//up move
                        {
                            last_index = GetMaxItemIndex();
                            next_index = last_index + 1;
                            if( !SetViewInfoDataIndex(ref item_info, next_index) )
                            {
                                CorrectFromIndex(last_index, -1);
                                break;
                            }

                        }
                        else if(move_direction > 0 && item_info.pos > max_dist)
                        {
                            last_index = GetMinItemIndex();
                            next_index = last_index - 1;
                            SetViewInfoDataIndex(ref item_info, next_index);
                            {
                                CorrectFromIndex(last_index, 1);
                                break;
                            }
                        }
                    }
                break;
            }
        }

        /// <summary>
        /// correct view's indexes in row on bound's data array
        /// </summary>
        /// <param name="max_index"></param>
        /// <param name="dir"></param>
        void CorrectFromIndex(int max_index, int dir)
        {
            int error_count = 0;
            int next_index = max_index;
            int correct_index;
            while(true)
            {
                next_index += dir;
                ref ItemInfo item_info = ref GetItemViewByArrayIndex(next_index);
                //check error
                if(item_info.index < 0)
                {
                    error_count += 1;
                    //exit if error count > view count
                    if(error_count > item_arr.Length - 1)
                        break;
                    continue;
                }
                correct_index = next_index + error_count * -dir;
                if(item_info.index != correct_index)
                {
                    // Debug.LogFormat("BaseChildSystem: CorrectFromIndex correct_index={0}, next_index={1}, error_count={2}, dir={3}",
                    //                                                 correct_index, next_index, error_count, dir);
                    SetViewInfoDataIndex(ref item_info, correct_index);
                }
            }
        }

        void ShowItemInfo(ref ItemInfo item, bool show)
        {
            item.is_show = show;
            item.item.Show(show);
        }

        int GetMaxItemIndex()
        {
            int index = 0;
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                if(item_info.index > index)
                    index = item_info.index;
            }
            return index;
        }

        int GetMinItemIndex()
        {
            int index = data_arr.Length - 1;
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                if(item_info.index < index)
                    index = item_info.index;
            }
            return index;
        }
        int GetUpDeletedCount(int from_index)
        {
            int count = 0;
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                if(item_info.is_deleted && item_info.index < from_index)
                {
                    count += 1;
                }
            }
            return count;
        }
        /// <summary>
        /// total up delete count
        /// </summary>
        /// <returns></returns>
        int GetUpDeletedCount()
        {
            int count = 0;
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                if(item_info.is_deleted)
                {
                    count += 1;
                }
            }
            return count;
        }
        //move view's
        IBaseItem<T> GetBaseItemByArrayIndex(int index)
        {
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                if(item_info.index == index)
                    return item_info.item;
            }
            return null; 
        }

        ref ItemInfo GetItemViewByArrayIndex(int index)
        {
            for(int i=0; i<item_arr.Length; i++)
            {
                ref ItemInfo item_info = ref item_arr[i];
                if(item_info.index == index)
                    return ref item_info;
            }

            fake_item_view.index = -1;
            return ref fake_item_view;
        }
    }
}