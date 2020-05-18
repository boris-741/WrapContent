# Wrap Content

Example for creating GUI elements using LeoECS and Unity3d

thanks leo [GitHub Pages](https://github.com/Leopotam/ecs)

![screenshot_0](/Img/wrap_content.gif)
![screenshot_1](/Img/wrap_content.png)
![screenshot_2](/Img/wrap_scroll_hierarchy.png)
 
1. create data structure.
example:

```
public struct CustomWrapItemData
{
    public string str;
}

```

2. create an inheritor class from MonoBehaviour and override interface IBaseItem functions
this is for scroll item element
````
public class WrapItem : MonoBehaviour, IBaseItem<CustomWrapItemData>
{
    public int index{set; get;} 
    public void SetData(int index, ref T data){}
    public void SetSize(float size){}
    public void SetAction(ViewAction action){}
    public void SetAction(float tm_action, float tm_cur, ViewAction action){}
    public void Show(bool show){}
}
```

3. create an inheritor class from BaseWrapContent - this is scroll class
GetItemSize - return size of scroll item
```
public class CustomWrapContent:  BaseWrapContent<CustomWrapItemData, WrapItem>
{
    protected override float GetItemSize(CustomWrapItemData data)
    {
        return 60f;
    }
}
```

see scene example