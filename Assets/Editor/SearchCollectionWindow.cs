using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using SearchField = UnityEditor.Search.SearchField;
using System.Linq;

public class SearchCollectionWindow : EditorWindow
{
    SearchCollectionTreeView m_TreeView;

    [SerializeField] string m_SearchText;
    [SerializeField] bool m_FocusSearchField = true;
    [SerializeField] TreeViewState m_TreeViewState;

    void OnEnable()
    {
        if (m_TreeViewState == null)
            m_TreeViewState = new TreeViewState();

        m_TreeView = new SearchCollectionTreeView(m_TreeViewState);
    }

    void OnGUI()
    {
        FocusSearchField();
        using (new EditorGUILayout.VerticalScope(GUIStyle.none, GUILayout.ExpandHeight(true)))
        {
            using (new GUILayout.HorizontalScope(Styles.toolbar))
            {
                if (DrawSearchField())
                    UpdateView();
            }

            DrawTreeView();
        }
    }

    void DrawTreeView()
    {
        var treeViewRect = EditorGUILayout.GetControlRect(false, -1, GUIStyle.none, GUILayout.ExpandHeight(true));
        m_TreeView.OnGUI(treeViewRect);
    }

    bool DrawSearchField()
    {
        var searchFieldText = m_SearchText;
        var searchTextRect = SearchField.GetRect(searchFieldText, position.width, (Styles.toolbarButton.fixedWidth + Styles.toolbarButton.margin.left) + Styles.toolbarButton.margin.right);
        var searchClearButtonRect = Styles.searchFieldBtn.margin.Remove(searchTextRect);
        searchClearButtonRect.xMin = searchClearButtonRect.xMax - SearchField.s_CancelButtonWidth;

        if (Event.current.type == EventType.MouseUp && searchClearButtonRect.Contains(Event.current.mousePosition))
        {
            ClearSearch();
            return true;
        }

        var previousSearchText = m_SearchText;
        if (Event.current.type != EventType.KeyDown || Event.current.keyCode != KeyCode.None || Event.current.character != '\r')
        {
            m_SearchText = SearchField.Draw(searchTextRect, m_SearchText, Styles.searchField);
            if (!string.Equals(previousSearchText, m_SearchText, StringComparison.Ordinal))
                return true;
        }

        if (!string.IsNullOrEmpty(m_SearchText))
        {
            EditorGUIUtility.AddCursorRect(searchClearButtonRect, MouseCursor.Arrow);
            if (GUI.Button(searchClearButtonRect, Icons.clear, Styles.searchFieldBtn))
            {
                ClearSearch();
                return true;
            }
        }
        return false;
    }

    void Update()
    {
        if (focusedWindow == this && SearchField.UpdateBlinkCursorState(EditorApplication.timeSinceStartup))
            Repaint();
    }

    void ClearSearch()
    {
        m_SearchText = "";
        m_FocusSearchField = true;
        UpdateView();
        GUI.changed = true;
        GUI.FocusControl(null);
        GUIUtility.ExitGUI();
    }

    void UpdateView()
    {
        m_TreeView.searchString = m_SearchText;
    }

    void FocusSearchField()
    {
        if (Event.current.type != EventType.Repaint)
            return;
        if (m_FocusSearchField)
        {
            SearchField.Focus();
            m_FocusSearchField = false;
        }
    }

    [MenuItem("Window/Search/Collections")]
    public static void ShowWindow()
    {
        SearchCollectionWindow wnd = GetWindow<SearchCollectionWindow>();
        wnd.titleContent = new GUIContent("Collections");
    }
}

class SearchCollectionTreeView : TreeView
{
    class CollectionTreeViewItem : SearchTreeViewItem
    {        
        readonly SearchQuery m_SearchQueryAsset;
        readonly SearchCollectionTreeView m_TreeView;
        public CollectionTreeViewItem(SearchCollectionTreeView treeView, SearchContext context, SearchItem item)
            : base(context, item)
        {
            m_TreeView = treeView ?? throw new ArgumentNullException(nameof(treeView));

            if (!GlobalObjectId.TryParse(item.id, out var gid))
                throw new ArgumentException($"Invalid search query item {item.id}", nameof(item));

            m_SearchQueryAsset = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as SearchQuery;
            if (m_SearchQueryAsset == null)
                throw new ArgumentException($"Cannot find search query asset {gid}", nameof(item));

            displayName = m_SearchQueryAsset.name;
            icon = item.GetThumbnail(context, cacheThumbnail: false);
            children = new List<TreeViewItem>();

            FetchItems();
        }

        public void FetchItems()
        {
            var context = SearchService.CreateContext(m_SearchQueryAsset.providerIds, m_SearchQueryAsset.text);
            SearchService.Request(context, (context, items) =>
            {
                foreach (var item in items)
                    AddChild(new SearchTreeViewItem(context, item));
            },
            context =>
            {
                UpdateLabel();
                m_TreeView.UpdateCollections();
                context?.Dispose();
            });
        }

        private void UpdateLabel()
        {
            displayName = $"{m_SearchQueryAsset.name} ({children.Count})";
        }

        public override void Select()
        {
            Utils.SelectAssetFromPath(AssetDatabase.GetAssetPath(m_SearchQueryAsset), true);
        }

        public override bool CanStartDrag()
        {
            return false;
        }
    }

    class SearchTreeViewItem : TreeViewItem
    {
        static int s_NextId = 10000;

        SearchItem m_SearchItem;
        public SearchTreeViewItem(SearchContext context, SearchItem item)
            : base(s_NextId++, 1, item.GetLabel(context))
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
            var defaultAction = m_SearchItem.provider?.actions.FirstOrDefault();
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

    public SearchCollectionTreeView(TreeViewState treeViewState)
        : base(treeViewState)
    {
        Reload();
    }

    protected override TreeViewItem BuildRoot()
    {
        var root = new TreeViewItem { id = int.MinValue, depth = -1, displayName = "Root", children = new List<TreeViewItem>() };
        FetchSearchQueries(root);
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

    void FetchSearchQueries(TreeViewItem root)
    {
        SearchService.Request("p:t=SearchQuery", (context, items) => OnIncomingQueries(context, items, root), _ => UpdateCollections());
    }

    void OnIncomingQueries(SearchContext context, IEnumerable<SearchItem> items, TreeViewItem root)
    {
        foreach (var item in items)
            root.AddChild(new CollectionTreeViewItem(this, context, item));
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
    }
}
