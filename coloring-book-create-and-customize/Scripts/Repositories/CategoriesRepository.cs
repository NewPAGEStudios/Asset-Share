using HootyBird.ColoringBook.Serialized;
using System.Collections.Generic;
using UnityEngine;

namespace HootyBird.ColoringBook.Repositories
{
    [CreateAssetMenu(fileName = "CategoriesRepository", menuName = "HootyBird/Repositories/Create Categories Repository")]
    public class CategoriesRepository : ScriptableObject
    {
        [SerializeField]
        private List<Category> categories;

        public List<Category> Categories => categories;
    }
}
