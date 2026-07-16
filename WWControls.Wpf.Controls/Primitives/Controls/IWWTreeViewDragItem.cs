namespace WWControls.Wpf.Primitives
{
    /// <summary>
    /// Optional contract a <see cref="WWTreeView"/> bound item implements to gate its own dragging.
    /// An item whose data context implements this interface is dragged only while <see cref="CanDrag"/>
    /// is <see langword="true"/>; items that do not implement it are draggable once selected (subject
    /// to <see cref="WWTreeView.AllowDragDrop"/>).
    /// </summary>
    public interface IWWTreeViewDragItem
    {
        /// <summary>Whether this item may currently be dragged.</summary>
        bool CanDrag { get; }
    }
}
