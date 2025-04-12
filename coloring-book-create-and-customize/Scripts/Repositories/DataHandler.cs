using HootyBird.ColoringBook.Serialized;
using UnityEngine;

namespace HootyBird.ColoringBook.Repositories
{
    /// <summary>
    /// Repositories container.
    /// </summary>
    public class DataHandler : MonoBehaviour
    {
        public static DataHandler Instance { get; private set; }

        [SerializeField]
        private UIRepository uiRepository;
        [SerializeField]
        private AudioRepository audioRepository;
        [SerializeField]
        private Category coloringBooks;

        public UIRepository UIRepository => uiRepository;

        public AudioRepository AudioRepository => audioRepository;

        public Category DefaultColoringBooks => coloringBooks;

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
