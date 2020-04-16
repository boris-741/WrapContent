namespace WrapContent
{
    /// <summary>
    /// wrap content - scroll to item component
    /// </summary>
    public struct BaseWrapScrollComponent
    {
        /// <summary>
        /// component index
        /// </summary>
        public int index;
        /// <summary>
        /// time scroll (0 - immidiantly)
        /// </summary>
        public float tm_scroll;
        /// <summary>
        /// scroll condition
        /// </summary>
        public ScrollCondition condition;
        /// <summary>
        /// view action after scroll
        /// </summary>
        public ViewAction view_action;
    }
}