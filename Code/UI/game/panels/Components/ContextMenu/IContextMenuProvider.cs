namespace Seekers;

public interface IContextMenuProvider
{
	List<ContextMenu.Entry> GetContextMenuItems(string path, bool isFolder);
}
