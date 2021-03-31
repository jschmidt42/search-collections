using System.Collections.Generic;

namespace UnityEditor.Search.Collections
{
    interface ISearchCollectionView : ISearchView
    {
        ISet<string> fieldNames { get; }
        ICollection<SearchCollection> collections { get; }

        void OpenContextualMenu();
    }
}