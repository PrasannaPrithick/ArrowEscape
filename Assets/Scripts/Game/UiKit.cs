using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HueGates.Game
{
    public sealed class UiKit
    {
        public Font font;
        public PlaceholderSprites art;

        public RectTransform Element(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public Image Image(Transform parent, Sprite sprite, Color color, string name)
        {
            RectTransform rt = Element(parent, name);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite == art.rounded || sprite == art.bar)
                image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        public Text Label(Transform parent, string text, int size, FontStyle style, TextAnchor anchor, Color color)
        {
            RectTransform rt = Element(parent, "text");
            Text label = rt.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = anchor;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        public Button Button(Transform parent, string text, Color face, Color labelColor, int fontSize, UnityAction onClick)
        {
            RectTransform rt = Element(parent, text);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = art.rounded;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = face;
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.fadeDuration = 0.05f;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            Text label = Label(rt, text, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, labelColor);
            Stretch(label.rectTransform);
            return button;
        }

        public LayoutElement Height(Component component, float height, float flexible = 0f)
        {
            LayoutElement element = component.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = flexible;
            return element;
        }

        public VerticalLayoutGroup Column(RectTransform rt, int padding, int spacing)
        {
            VerticalLayoutGroup layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public HorizontalLayoutGroup Row(RectTransform rt, int spacing)
        {
            HorizontalLayoutGroup layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public RectTransform Scroll(Transform parent, out RectTransform content)
        {
            RectTransform root = Element(parent, "scroll");
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            RectTransform viewport = Element(root, "viewport");
            Stretch(viewport);
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0.01f);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Element(viewport, "content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return root;
        }

        public void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
