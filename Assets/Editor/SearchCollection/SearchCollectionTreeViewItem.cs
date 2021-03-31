using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search.Collections
{ 
    class SearchCollectionTreeViewItem : SearchTreeViewItem
    {        
        readonly HashSet<SearchItem> m_Items;
        readonly SearchCollection m_Collection;
        readonly SearchCollectionTreeView m_TreeView;
        public SearchCollectionTreeViewItem(SearchCollectionTreeView treeView, SearchCollection collection)
        {
            m_TreeView = treeView ?? throw new ArgumentNullException(nameof(treeView));
            m_Collection = collection ?? throw new ArgumentNullException(nameof(collection));
            m_Items = new HashSet<SearchItem>();

            icon = Icons.quicksearch;
            displayName = m_Collection.query.name;
            children = new List<TreeViewItem>();

            FetchItems();
        }

        public void FetchItems()
        {
            var context = SearchService.CreateContext(m_Collection.query.providerIds, m_Collection.query.text);
            SearchService.Request(context, (_, items) =>
            {
                foreach (var item in items)
                {
                    if (m_Items.Add(item))
                        AddChild(new SearchTreeViewItem(context, item));
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

        private void Refresh()
        {
            m_Items.Clear();
            children.Clear();
            FetchItems();
        }
    }
}
