using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search.Collections
{
    class SearchCollectionColumnHeader : MultiColumnHeader
    {
        readonly ISearchCollectionView searchView;

        public SearchCollectionColumnHeader(ISearchCollectionView searchView)
            : base(new MultiColumnHeaderState(CreateColumns().ToArray()))
        {
            canSort = true;
            this.searchView = searchView;
            allowDraggingColumnsToReorder = true;
        }

        private static IEnumerable<MultiColumnHeaderState.Column> CreateColumns()
        {
            yield return CreateColumn("", 200f, autoResize: false);
            yield return CreateColumn("value", 50f);
        }

        static MultiColumnHeaderState.Column CreateColumn(string label, float width = 32f, bool autoResize = true)
        {
            return new MultiColumnHeaderState.Column()
            {
                width = width,
                headerContent = new GUIContent(label),
                autoResize = autoResize,
                canSort = true,
                sortedAscending = true,
                allowToggleVisibility = false,
                headerTextAlignment = TextAlignment.Left,
                sortingArrowAlignment = TextAlignment.Right,
                minWidth = 32f,
                maxWidth = 1000000f,
                contextMenuText = null
            };
        }
    }
}