using System;
using System.Collections.Generic;

namespace UnityEditor.Search.Collections
{
    [Serializable]
    class SearchCollection
    {
        public SearchCollection(SearchQuery searchQuery)
        {
            query = searchQuery ?? throw new ArgumentNullException(nameof(searchQuery));
            objects = new List<UnityEngine.Object>();
        }

        public SearchQuery query;
        public List<UnityEngine.Object> objects;
    }
}
