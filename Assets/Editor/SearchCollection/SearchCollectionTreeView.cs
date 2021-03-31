using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search.Collections
{
    class SearchCollectionTreeView : TreeView
    {
        readonly ISearchCollectionView searchView;
        public ICollection<SearchCollection> collections => searchView.collections;

        public SearchCollectionTreeView(TreeViewState treeViewState, ISearchCollectionView searchView)
            : base(treeViewState, new SearchCollectionColumnHeader(searchView))
        {
            this.searchView = searchView ?? throw new ArgumentNullException(nameof(searchView));
            showAlternatingRowBackgrounds = true;

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = int.MinValue, depth = -1, displayName = "Root", children = new List<TreeViewItem>() };
            foreach (var coll in collections)
                root.AddChild(new SearchCollectionTreeViewItem(this, coll));
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem rowItem)
        {
            EditorApplication.tick -= DelayedUpdateCollections;
            return base.BuildRows(rowItem);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is SearchTreeViewItem tvi && tvi.item != null)
            {
                for (int i = 0, end = args.GetNumVisibleColumns(); i < end; ++i)
                {
                    var cellRect = args.GetCellRect(i);
                    if (i == 0)
                    {
                        var mainArgs = args;
                        mainArgs.rowRect = cellRect;
                        base.RowGUI(mainArgs);
                    }
                    else
                    {
                        var v = tvi.item.GetValue();
                        if (v != null)
                            GUI.Label(cellRect, v.ToString());
                    }
                }
            }
            else
            { 
                base.RowGUI(args); 
            }
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

        protected override void ContextClicked()
        {
            OpenContextualMenu(() => searchView.OpenContextualMenu());
        }

        protected override void ContextClickedItem(int id)
        {
            if (FindItem(id, rootItem) is SearchTreeViewItem stvi)
                OpenContextualMenu(() => stvi.OpenContextualMenu());
        }

        bool m_InContextualMenu = false;
        private bool OpenContextualMenu(Action handler)
        {
            if (m_InContextualMenu)
                return false;
            handler();
            m_InContextualMenu = true;
            EditorApplication.delayCall += () => m_InContextualMenu = false;
            Repaint();
            return true;
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
            collections.Add(newCollection);
            rootItem.AddChild(new SearchCollectionTreeViewItem(this, newCollection));
            BuildRows(rootItem);
        }

        public void Remove(SearchCollectionTreeViewItem collectionItem, SearchCollection collection)
        {
            collections.Remove(collection);
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