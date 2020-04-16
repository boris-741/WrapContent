namespace WrapContent
{
    public enum DataAction
    {
        create,
        remove,
        add,
        insert,
        update
    }

    public enum Direction 
    {
        top,
        bottom,
        left,
        right
    }

    public enum EventType
    {
        begin_drag,
        end_drag,
        drag,
        point_up,
        point_down
    }

    public enum ScrollCondition
    {
        none,
        nearvisible
    }

    public enum ViewAction
    {
        none,
        after_add_view,
        before_delete_view
    }
}