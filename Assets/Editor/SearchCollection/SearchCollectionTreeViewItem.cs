using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search.Collections
{ 
    class SearchCollectionTreeViewItem : SearchTreeViewItem
    {        
        readonly SearchCollection m_Collection;
        public SearchCollectionTreeViewItem(SearchCollectionTreeView treeView, SearchCollection collection)
            : base(treeView)
        {
            m_Collection = collection ?? throw new ArgumentNullException(nameof(collection));

            icon = Icons.quicksearch;
            displayName = m_Collection.query.name;
            children = new List<TreeViewItem>();

            FetchItems();
        }

        public void FetchItems()
        {
            var context = SearchService.CreateContext(m_Collection.query.providerIds, m_Collection.query.text);
            foreach (var item in m_Collection.items)
                AddChild(new SearchTreeViewItem(m_TreeView, context, item));
            SearchService.Request(context, (_, items) =>
            {
                foreach (var item in items)
                {
                    if (m_Collection.items.Add(item))
                        AddChild(new SearchTreeViewItem(m_TreeView, context, item));
                }
            },
            _ =>
            {
                UpdateLabel();
                m_TreeView.UpdateCollections();
                context?.Dispose();
            });
        }

        private void UpdateLabel()
        {
            displayName = $"{m_Collection.query.name} ({children.Count})";
        }

        public override void Select()
        {
            // Do nothing
        }

        public override void Open()
        {
            SearchQuery.Open(m_Collection.query.GetInstanceID());
        }

        public override bool CanStartDrag()
        {
            return false;
        }

        public override void OpenContextualMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Refresh"), false, () => Refresh());
            menu.AddSeparator("/Edit");
            menu.AddItem(new GUIContent("Edit"), false, () => Selection.activeObject = m_Collection.query);
            menu.AddItem(new GUIContent("Open"), false, () => Open());
            menu.AddSeparator("/Remove");
            menu.AddItem(new GUIContent("Remove"), false, () => m_TreeView.Remove(this, m_Collection));

            menu.ShowAsContext();
        }

        public void Refresh()
        {
            children.Clear();
            m_Collection.items.Clear();
            FetchItems();
        }
    }
}
