using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Leopotam.EcsNew;
using WrapContent;

public class WrapItem_3 : MonoBehaviour, IBaseItem<CustomWrapItemData_3>
{
    public int index{set; get;} 

    Text text;
    Coroutine add_cor;
    ViewAction last_action;
    bool init = false;

    void Init()
    {
        if(init) return;

        Button btn = transform.Find("Button").GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnRemove);
        
        text = transform.Find("Text").GetComponent<Text>();
        init = true;
    }

    public void SetData(int index, ref CustomWrapItemData_3 data)
    {
        Init();
        StopLastAction();

        this.index = index;
        text.text = data.str;
    }

    public void SetSize(float size){}
    public void SetAction(float tm_act, float tm_cur, ViewAction action)
    {
        Init();
        //Debug.LogFormat("WrapItem SetAction action={0}, tm_act={1}, tm_cur={2}", action, tm_act, tm_cur);
        if(Mathf.Approximately(tm_cur, 0)) return;

        switch(action)
        {
            case ViewAction.before_delete_view:
                Vector2 scale = Vector2.Lerp(Vector2.one, new Vector3(0, 0, 1), tm_cur/tm_act);
                transform.localScale = scale;
            break;
        }
        last_action = action;
    }

    public void SetAction(ViewAction action)
    {
        Init();
        Debug.LogFormat("WrapItem: SetAction index:{0} act:{1}", index, action);
        switch(action)
        {
            case ViewAction.after_add_view:
                add_cor = StartCoroutine(AddCor());
            break;
        }
        last_action = action;
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }

    void OnRemove()
    {
        WrapMgr.CreateDataComponent(1, index, DataAction.remove, new CustomWrapItemData_3());
    }

    void StopLastAction()
    {
        switch(last_action)
        {
            case ViewAction.after_add_view:
                if(add_cor != null)
                {
                    //Debug.LogFormat("WrapItem: SetData stop coroutine index:{0}", index);
                    StopCoroutine(add_cor);
                    transform.localScale = Vector3.one;
                    add_cor = null;
                }
            break;
            case ViewAction.before_delete_view:
                transform.localScale = Vector3.one;
            break;
        }
        last_action = ViewAction.none;
    }

    IEnumerator AddCor()
    {
        float tm_show = 0.3f;
        float cur_time = 0;
        transform.localScale = new Vector3(0, 0, 1);
        Vector2 scale;
        while(tm_show > cur_time)
        {
            cur_time += Time.deltaTime;
            scale = Vector2.Lerp(new Vector3(0, 0, 1), Vector2.one, cur_time/tm_show);
            transform.localScale = scale;
            yield return null;
        }
        transform.localScale = Vector3.one;
        add_cor = null;
    }

}
