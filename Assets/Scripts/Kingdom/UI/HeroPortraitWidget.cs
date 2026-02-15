using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.App
{
    /// <summary>
    /// Hero portrait widget used by GameView HUD.
    /// Built as a separate prefab so art can be swapped without touching GameView.
    /// </summary>
    public class HeroPortraitWidget : MonoBehaviour
    {
        [SerializeField] private Image backdropImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image portraitImage;

        public Image PortraitImage => portraitImage;

        private void Awake()
        {
            EnsureRuntimeDefaults();
        }

        public void EnsureRuntimeDefaults()
        {
            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            if (backdropImage == null)
            {
                backdropImage = EnsureImageChild(root, "Backdrop", new Color(0f, 0f, 0f, 0.42f));
                Stretch(backdropImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            if (portraitImage == null)
            {
                portraitImage = EnsureImageChild(root, "Portrait", new Color(1f, 1f, 1f, 0.18f));
                portraitImage.preserveAspect = true;
                Stretch(portraitImage.rectTransform, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
            }

            if (frameImage == null)
            {
                frameImage = EnsureImageChild(root, "Frame", new Color(1f, 1f, 1f, 0.22f));
                frameImage.raycastTarget = false;
                Stretch(frameImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        public void SetPortrait(Sprite portrait)
        {
            EnsureRuntimeDefaults();
            if (portraitImage == null)
            {
                return;
            }

            portraitImage.sprite = portrait;
            portraitImage.color = portrait != null
                ? Color.white
                : new Color(1f, 1f, 1f, 0.18f);
        }

        public static HeroPortraitWidget CreateFallback(Transform parent)
        {
            var go = new GameObject("HeroPortraitWidget", typeof(RectTransform), typeof(HeroPortraitWidget));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(84f, 84f);
            var widget = go.GetComponent<HeroPortraitWidget>();
            widget.EnsureRuntimeDefaults();
            return widget;
        }

        private static Image EnsureImageChild(RectTransform parent, string name, Color color)
        {
            Transform existing = parent.Find(name);
            Image image;
            if (existing != null)
            {
                image = existing.GetComponent<Image>();
                if (image == null)
                {
                    image = existing.gameObject.AddComponent<Image>();
                }
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
