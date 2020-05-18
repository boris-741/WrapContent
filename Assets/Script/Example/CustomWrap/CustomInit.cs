using System.Collections;
using UnityEngine;
using Leopotam.EcsNew;
using WrapContent;

public class CustomInit : MonoBehaviour
{
    void Start()
    {
        //init wrap singleton
        WrapMgr.Init<CustomFilterInitSystem>();
        //init data array
        int count = 100;
        for(int i=0; i<count; i++)
        {
            WrapMgr.CreateDataComponent(count, i, DataAction.create, new CustomWrapItemData{str = string.Format("{0}", i)});
        }

        //init data 2 array
        count = 100;
        int num = 0;
        string big_text_str;
        for(int i=0; i<count; i++)
        {
            num += 1;
            if(num > 6)
                num = 0;
            big_text_str = "Big Text";
            for(int s=0; s<num; s++)
            {
                big_text_str = string.Concat(big_text_str, " ", big_text_str);
            }
            WrapMgr.CreateDataComponent(count, i, DataAction.create, 
                                        new CustomWrapItemData_2{str = big_text_str});
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
}
