using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Menu
{

    public class MainMenuBGScroller : MonoBehaviour
    {
        [SerializeField]
        private Vector2 scrollSpeed;

        private Graphic graphic;

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
        }

        private void Update()
        {
            graphic.material.mainTextureOffset += scrollSpeed * Time.deltaTime;
        }
    }
}
