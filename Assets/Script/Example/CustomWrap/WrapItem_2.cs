using UnityEngine;
using UnityEngine.UI;
using WrapContent;

public class WrapItem_2 : MonoBehaviour, IBaseItem<CustomWrapItemData_2>
{
    public int index{ set; get; } 

    Text text_num;
    Text text;
    RectTransform item_rec;
    RectTransform text_rec;
     bool init = false;

    void Init()
    {
        if(init) return;

        text_num = transform.Find("TextNum").GetComponent<Text>();
        text = transform.Find("Text").GetComponent<Text>();
        item_rec = GetComponent<RectTransform>();
        text_rec = text.GetComponent<RectTransform>();
        
        init = true;
    }

    public void SetData(int index, ref CustomWrapItemData_2 data)
    {
        Init();

        this.index = index;
        text_num.text = string.Format("{0}", index);
        text.text = data.str;
    }

    public void SetSize(float size)
    {
        Vector2 rec_size = text_rec.sizeDelta;
        rec_size.y = size - 29;
        text_rec.sizeDelta = rec_size;

        rec_size = item_rec.sizeDelta;
        rec_size.y = size;
        item_rec.sizeDelta = rec_size;
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }
    
    public void SetAction(ViewAction action){}
    public void SetAction(float tm_act, float tm_cur, ViewAction action){}
}