using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search.Collections
{
    class SearchTreeViewItem : TreeViewItem
    {
        static int s_NextId = 10000;

        SearchItem m_SearchItem;
        public SearchItem item => m_SearchItem;

        public SearchTreeViewItem()
            : base(s_NextId++, 0)
        {
            m_SearchItem = null;
        }

        public SearchTreeViewItem(SearchContext context, SearchItem item)
            : base(s_NextId++, 0, item.GetLabel(context))
        {
            m_SearchItem = item;
            icon = item.GetThumbnail(context, cacheThumbnail: false);
        }

        public virtual void Select()
        {
            m_SearchItem.provider?.trackSelection?.Invoke(m_SearchItem, m_SearchItem.context);
        }

        public virtual void Open()
        {
            var currentSelection = new[] { m_SearchItem };
            var defaultAction = m_SearchItem.provider?.actions.FirstOrDefault(a => a.enabled?.Invoke(currentSelection) ?? true);
            ExecuteAction(defaultAction, new [] { m_SearchItem });
        }

        public virtual void OpenContextualMenu()
        {
            var menu = new GenericMenu();
            var currentSelection = new[] { m_SearchItem };
            foreach (var action in m_SearchItem.provider.actions.Where(a => a.enabled(currentSelection)))
            {
                var itemName = !string.IsNullOrWhiteSpace(action.content.text) ? action.content.text : action.content.tooltip;
                menu.AddItem(new GUIContent(itemName, action.content.image), false, () => ExecuteAction(action, currentSelection));
            }

            menu.ShowAsContext();
        }

        private void ExecuteAction(SearchAction action, SearchItem[] currentSelection)
        {
            if (action == null)
                return;
            if (action.handler != null)
                action.handler(m_SearchItem);
            else if (action.execute != null)
                action.execute(currentSelection);
        }

        public virtual bool CanStartDrag()
        {
            return m_SearchItem.provider?.startDrag != null;
        }

        public UnityEngine.Object GetObject()
        {
            return m_SearchItem.provider?.toObject(m_SearchItem, typeof(UnityEngine.Object));
        }
    }
}
