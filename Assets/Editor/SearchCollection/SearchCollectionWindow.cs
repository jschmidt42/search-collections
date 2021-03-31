// TODO:
// 1- Add a new flags to saved search query to mark them as collection.
//   a. Only load search query asset marked as collections.
// 2- Add support to create search query asset with a custom list of search items.

// PICKER ISSUES:
// - Hide toolbar/search field/button
// - Allow to toggle panels
// - Do not always center the picker view
// - Allow to completely override the picker title (do not keep Select ...)
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search.Collections
{
    public class SearchCollectionWindow : EditorWindow, ISearchView
    {
        static class InnerStyles
        {
            public static GUIContent createContent = EditorGUIUtility.IconContent("CreateAddNew");
            public static GUIStyle toolbarCreateAddNewDropDown = new GUIStyle(EditorStyles.toolbarCreateAddNewDropDown)
            {
                fixedWidth = 28f,
                fixedHeight = 14f,
                padding = new RectOffset(0, 0, 0, 0)
            };
            public static GUIStyle toolbar = new GUIStyle(Styles.toolbar)
            {
                padding = new RectOffset(4, 4, 4, 4)
            };
        }

        static int s_GlobalWindowCounter = 1;

        SearchCollectionTreeView m_TreeView;

        [SerializeField] int m_WindowCounter;
        [SerializeField] string m_SearchText;
        [SerializeField] bool m_FocusSearchField = true;
        [SerializeField] TreeViewState m_TreeViewState;
        [SerializeField] List<SearchCollection> m_Collections;

        public ISearchList results => throw new NotSupportedException();
        public SearchContext context => throw new NotSupportedException();

        public DisplayMode displayMode => DisplayMode.List;
        public float itemIconSize { get => 0f; set => throw new NotSupportedException(); }
        public bool multiselect { get => true; set => throw new NotSupportedException(); }

        public Action<SearchItem, bool> selectCallback => throw new NotSupportedException();
        public Func<SearchItem, bool> filterCallback => throw new NotSupportedException();
        public Action<SearchItem> trackingCallback => throw new NotSupportedException();

        public SearchSelection selection
        {
            get
            {
                return new SearchSelection(m_TreeView.GetSelection()
                    .Select(idx => m_TreeView.GetRows()[idx] as SearchTreeViewItem)
                    .Where(e => e != null)
                    .Select(e => e.item));
            }
        }

        void OnEnable()
        {
            if (m_WindowCounter == 0)
                m_WindowCounter = s_GlobalWindowCounter++;
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            if (m_Collections == null)
                m_Collections = LoadCollections(m_WindowCounter);

            m_TreeView = new SearchCollectionTreeView(m_TreeViewState, m_Collections);
        }

        void OnDisable()
        {
            s_GlobalWindowCounter--;
            SaveWindowCollections(m_WindowCounter);
        }

        private List<SearchCollection> LoadCollections(int windowId)
        {
            var collectionPaths = EditorPrefs.GetString($"SearchCollection.{windowId}", "")
                .Split(new [] { ";;;" }, StringSplitOptions.RemoveEmptyEntries);
            var collection = collectionPaths
                .Select(p => AssetDatabase.LoadAssetAtPath<SearchQuery>(p))
                .Where(p => p)
                .Select(sq => new SearchCollection(sq));
            return new List<SearchCollection>(collection);
        }

        private void SaveWindowCollections(int windowId)
        {
            var collectionPaths = string.Join(";;;", m_Collections.Select(c => AssetDatabase.GetAssetPath(c.query)));
            EditorPrefs.SetString($"SearchCollection.{windowId}", collectionPaths);
        }

        void OnGUI()
        {
            var evt = Event.current;
            HandleShortcuts(evt);
            using (new EditorGUILayout.VerticalScope(GUIStyle.none, GUILayout.ExpandHeight(true)))
            {
                using (new GUILayout.HorizontalScope(InnerStyles.toolbar))
                {
                    if (DrawSearchField())
                        UpdateView();
                    DrawButtons();
                }

                DrawTreeView();
            }
        }

        void HandleShortcuts(Event evt)
        {
            if (evt.type == EventType.KeyUp && evt.keyCode == KeyCode.F5)
            {
                m_TreeView.Reload();
                evt.Use();
            }
            else if (!docked && evt.type == EventType.KeyUp && evt.keyCode == KeyCode.Escape)
            {
                evt.Use();
                Close();
            }
            else
            {
                FocusSearchField();
            }

            if (evt.type == EventType.Used)
                Repaint();
        }

        void DrawTreeView()
        {
            var treeViewRect = EditorGUILayout.GetControlRect(false, -1, GUIStyle.none, GUILayout.ExpandHeight(true));
            m_TreeView.OnGUI(treeViewRect);
        }

        public void DrawButtons()
        {
            Rect rect = GUILayoutUtility.GetRect(InnerStyles.createContent, InnerStyles.toolbarCreateAddNewDropDown);
            bool mouseOver = rect.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
                InnerStyles.toolbarCreateAddNewDropDown.Draw(rect, InnerStyles.createContent, mouseOver, false, false, false);
            else if (Event.current.type == EventType.MouseDown && mouseOver)
            {
                GUIUtility.hotControl = 0;

                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Load collection..."), false, LoadCollection);

                menu.DropDown(rect);
                Event.current.Use();
            }
        }

        private void LoadCollection()
        {
            var context = SearchService.CreateContext("asset", $"t={nameof(SearchQuery)}");
            SearchService.ShowPicker(context, SelectCollection, 
                trackingHandler: _ => { }, 
                title: "search collection",
                defaultWidth: 300, defaultHeight: 500, itemSize: 0);
        }

        private void SelectCollection(SearchItem selectedItem, bool canceled)
        {
            if (canceled)
                return;

            var searchQuery = selectedItem.ToObject<SearchQuery>();
            if (!searchQuery)
                return;
            
            m_TreeView.Add(new SearchCollection(searchQuery));
        }

        bool DrawSearchField()
        {
            var hashForSearchField = "CollectionsSearchField".GetHashCode();
            var searchTextRect = GUILayoutUtility.GetRect(-1, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            int searchFieldControlID = GUIUtility.GetControlID(hashForSearchField, FocusType.Passive, searchTextRect);

            if (Event.current.type != EventType.KeyDown || Event.current.keyCode != KeyCode.None || Event.current.character != '\r')
            {
                var previousSearchText = m_SearchText;
                m_SearchText = EditorGUI.ToolbarSearchField(
                    searchFieldControlID,
                    searchTextRect,
                    m_SearchText,
                    EditorStyles.toolbarSearchField,
                    string.IsNullOrEmpty(m_SearchText) ? GUIStyle.none : EditorStyles.toolbarSearchFieldCancelButton);
                if (!string.Equals(previousSearchText, m_SearchText, StringComparison.Ordinal))
                    return true;
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

        public void SetSelection(params int[] selection)
        {
            throw new NotImplementedException();
        }

        public void AddSelection(params int[] selection)
        {
            throw new NotImplementedException();
        }

        public void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.MoveLineEnd)
        {
            SetSearchText(searchText, moveCursor, -1);
        }

        public void SetSearchText(string searchText, TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            m_SearchText = searchText;
            UpdateView();
        }

        public void Refresh(RefreshFlags reason = RefreshFlags.Default)
        {
            m_TreeView.Reload();
            Repaint();
        }

        public void ExecuteAction(SearchAction action, SearchItem[] items, bool endSearch = true)
        {
            throw new NotImplementedException();
        }

        public void ShowItemContextualMenu(SearchItem item, Rect contextualActionPosition)
        {
            throw new NotImplementedException();
        }

        public void SelectSearch()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
