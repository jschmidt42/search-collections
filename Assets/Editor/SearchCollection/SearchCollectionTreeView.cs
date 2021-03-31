using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor.Search.Collections
{
    class SearchCollectionTreeView : TreeView
    {
        private List<SearchCollection> m_Collections;

        public SearchCollectionTreeView(TreeViewState treeViewState, List<SearchCollection> collections)
            : base(treeViewState)
        {
            m_Collections = collections ?? throw new ArgumentNullException(nameof(collections));
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = int.MinValue, depth = -1, displayName = "Root", children = new List<TreeViewItem>() };
            foreach (var coll in m_Collections)
                root.AddChild(new SearchCollectionTreeViewItem(this, coll));
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem rowItem)
        {
            EditorApplication.tick -= DelayedUpdateCollections;
            return base.BuildRows(rowItem);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            if (selectedIds.Count == 0)
                return;

            if (FindItem(selectedIds.Last(), rootItem) is SearchTreeViewItem stvi)
                stvi.Select();
        }

        protected override void DoubleClickedItem(int id)
        {
            if (FindItem(id, rootItem) is SearchTreeViewItem stvi)
                stvi.Open();
        }

        protected override void ContextClickedItem(int id)
        {
            if (FindItem(id, rootItem) is SearchTreeViewItem stvi)
                stvi.OpenContextualMenu();
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            if (args.draggedItem is SearchTreeViewItem stvi)
                return stvi.CanStartDrag();
            return false;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var items = args.draggedItemIDs.Select(id => FindItem(id, rootItem) as SearchTreeViewItem).Where(i => i != null);
            var selectedObjects = items.Select(e => e.GetObject()).Where(o => o).ToArray();
            if (selectedObjects.Length == 0)
                return;
            var paths = selectedObjects.Select(i => AssetDatabase.GetAssetPath(i)).ToArray();
            Utils.StartDrag(selectedObjects, paths, string.Join(", ", items.Select(e => e.displayName)));
        }

        public void Add(SearchCollection newCollection)
        {
            m_Collections.Add(newCollection);
            rootItem.AddChild(new SearchCollectionTreeViewItem(this, newCollection));
            BuildRows(rootItem);
        }

        public void Remove(SearchCollectionTreeViewItem collectionItem, SearchCollection collection)
        {
            m_Collections.Remove(collection);
            rootItem.children.Remove(collectionItem);
            BuildRows(rootItem);
        }

        public void UpdateCollections()
        {
            EditorApplication.tick -= DelayedUpdateCollections;
            EditorApplication.tick += DelayedUpdateCollections;
        }

        private void DelayedUpdateCollections()
        {
            EditorApplication.tick -= DelayedUpdateCollections;
            BuildRows(rootItem);
            Repaint();
        }
    }
}