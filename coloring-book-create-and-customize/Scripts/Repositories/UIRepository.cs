using HootyBird.ColoringBook.Menu;
using System.Collections.Generic;
using UnityEngine;

namespace HootyBird.ColoringBook.Repositories
{
    [CreateAssetMenu(fileName = "UIAssetsRepository", menuName = "HootyBird/Repositories/Create UI Assets Repository")]
    public class UIRepository : ScriptableObject
    {
        [SerializeField]
        private List<MenuOverlay> dynamicOverlays;

        public T GetOverlay<T>() where T : MenuOverlay
        {
            foreach (MenuOverlay overlay in dynamicOverlays)
            {
                MenuOverlay prefab = overlay.GetComponent<T>();

                if (prefab)
                {
                    return prefab as T;
                }
            }

            return null;
        }
    }
}
