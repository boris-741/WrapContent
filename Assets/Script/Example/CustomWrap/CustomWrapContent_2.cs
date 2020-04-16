using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WrapContent;

public class CustomWrapContent_2:  BaseWrapContent<CustomWrapItemData_2, WrapItem_2>
{
    TextGenerator generator = null;
    TextGenerationSettings textsett;

    void Start()
    {
        StartCoroutine(CustomInitCor());
    }

    IEnumerator CustomInitCor()
    {
        yield return new WaitForSeconds(0.1f);
        Init();
    }

    void CreateTextGenerator()
    {
        generator = new TextGenerator();
        Text text = transform.Find("Content/Item/Text").GetComponent<Text>();
        textsett = text.GetGenerationSettings(text.rectTransform.rect.size);
    }

    
    protected override float GetItemSize(CustomWrapItemData_2 data)
    {
        if(generator == null)
            CreateTextGenerator();
            
        generator.GetPreferredHeight(data.str, textsett);
        int line_count = generator.lineCount;    
        return Mathf.CeilToInt(line_count * 14f) + Mathf.CeilToInt((line_count + 2) * 0.8f) + 30;
    }
}