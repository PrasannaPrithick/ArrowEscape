using System.Collections.Generic;
using UnityEngine;

namespace HueGates.Game
{
    public sealed class PlaceholderSprites
    {
        public Sprite rounded;
        public Sprite circle;
        public Sprite soft;
        public Sprite arrow;
        public Sprite heart;
        public Sprite star;
        public Sprite bar;
        public Sprite cell;
        public Sprite symbolCircle;
        public Sprite symbolTriangle;
        public Sprite symbolSquare;
        public Sprite symbolDiamond;
        public Sprite symbolStar;
        public Sprite shard;
        public Sprite square;

        public static PlaceholderSprites Build()
        {
            var art = new PlaceholderSprites();
            art.rounded = Slice(Rounded(256, 64, true), 64);
            art.circle = Simple(Circle(128, 0.42f, 0.08f));
            art.soft = Simple(SoftCircle(128));
            art.arrow = Simple(Arrow(256));
            art.heart = Simple(Heart(128));
            art.star = Simple(Star(128, 5, 0.46f, 0.2f));
            art.bar = Slice(Rounded(128, 36, true), 36);
            art.cell = Simple(CellTile(128));
            art.symbolCircle = Simple(Circle(64, 0.32f, 0.02f));
            art.symbolTriangle = Simple(Polygon(64, 3, 0.4f));
            art.symbolSquare = Simple(Polygon(64, 4, 0.34f));
            art.symbolDiamond = Simple(Paint(64, (u, v) => InsidePolygon(u, v, 4, 0.42f, Mathf.PI * 0.25f) ? 1f : 0f));
            art.symbolStar = Simple(Star(64, 5, 0.42f, 0.18f));
            art.shard = Simple(Rounded(64, 18, false));
            art.square = Simple(WhiteSquare(16));
            return art;
        }

        static readonly Dictionary<string, Sprite> loaded = new Dictionary<string, Sprite>();

        public static Sprite Prefer(string resourcePath, Sprite fallback)
        {
            Sprite cached;
            if (loaded.TryGetValue(resourcePath, out cached) && cached != null) return cached;
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            if (sprite != null) loaded[resourcePath] = sprite;
            return sprite != null ? sprite : fallback;
        }

        static Sprite Simple(Texture2D texture)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite Slice(Texture2D texture, float border)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Texture2D WhiteSquare(int size)
        {
            Texture2D texture = NewTex(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Texture2D NewTex(int w, int h)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        static Texture2D Paint(int size, System.Func<float, float, float> alpha)
        {
            var texture = NewTex(size, size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = ((x + 0.5f) / size - 0.5f) * 2f;
                    float v = ((y + 0.5f) / size - 0.5f) * 2f;
                    float a = Mathf.Clamp01(alpha(u, v));
                    float light = Mathf.Clamp01(0.85f + ( -u + v) * 0.12f);
                    pixels[y * size + x] = new Color(light, light, light, a);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Texture2D Rounded(int width, int radius, bool highlight)
        {
            var texture = NewTex(width, radius * 2);
            int height = texture.height;
            var pixels = new Color[width * height];
            float r = radius - 1.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x > width - radius) dx = x - (width - radius);
                    float dy = 0f;
                    if (y < radius) dy = radius - y;
                    else if (y > height - radius) dy = y - (height - radius);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - dist + 0.5f);
                    float light = highlight ? 0.9f + (y / (float)height) * 0.15f : 1f;
                    pixels[y * width + x] = new Color(light, light, light, a);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Texture2D Circle(int size, float radius, float edge)
        {
            return Paint(size, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                return (radius - d) / Mathf.Max(0.001f, edge);
            });
        }

        static Texture2D SoftCircle(int size)
        {
            return Paint(size, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                return Mathf.Clamp01(1f - d) * 0.85f;
            });
        }

        static Texture2D CellTile(int size)
        {
            var texture = NewTex(size, size);
            var pixels = new Color[size * size];
            Color baseColor = HueTheme.Panel;
            Color hi = new Color(0.22f, 0.26f, 0.42f, 1f);
            float r = size * 0.22f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f;
                    if (x < r) dx = r - x;
                    else if (x > size - r) dx = x - (size - r);
                    float dy = 0f;
                    if (y < r) dy = r - y;
                    else if (y > size - r) dy = y - (size - r);
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
                    float t = y / (float)size;
                    Color color = Color.Lerp(baseColor, hi, t * 0.35f);
                    color.a = a;
                    pixels[y * size + x] = color;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Texture2D Arrow(int size)
        {
            return Paint(size, (u, v) =>
            {
                bool shaft = v >= -0.72f && v <= 0.16f && Mathf.Abs(u) <= 0.2f;
                bool head = false;
                if (v >= 0.02f && v <= 0.86f)
                {
                    float t = (v - 0.02f) / 0.84f;
                    head = Mathf.Abs(u) <= 0.62f * (1f - t);
                }
                if (!shaft && !head) return 0f;
                return 1f;
            });
        }

        static Texture2D Heart(int size)
        {
            return Paint(size, (u, v) =>
            {
                float x = u * 0.95f;
                float y = v * 0.95f + 0.05f;
                float a = x * x + y * y - 0.55f;
                float f = a * a * a - x * x * y * y * y;
                return f < 0f ? 1f : 0f;
            });
        }

        static Texture2D Polygon(int size, int sides, float radius)
        {
            return Paint(size, (u, v) => InsidePolygon(u, v, sides, radius, 0f) ? 1f : 0f);
        }

        static Texture2D Star(int size, int points, float outer, float inner)
        {
            return Paint(size, (u, v) =>
            {
                float ang = Mathf.Atan2(v, u);
                float dist = Mathf.Sqrt(u * u + v * v);
                float turn = Mathf.PI * 2f / points;
                float local = (ang + Mathf.PI * 0.5f) % turn;
                if (local < 0f) local += turn;
                float half = turn * 0.5f;
                float t = Mathf.Abs(local - half) / half;
                float limit = Mathf.Lerp(inner, outer, t);
                return limit - dist > 0f ? 1f : 0f;
            });
        }

        static bool InsidePolygon(float u, float v, int sides, float radius, float rotation)
        {
            float ang = Mathf.Atan2(v, u) - rotation;
            float dist = Mathf.Sqrt(u * u + v * v);
            float sector = Mathf.PI * 2f / sides;
            float local = (ang + sector * 0.5f) % sector;
            if (local < 0f) local += sector;
            local -= sector * 0.5f;
            float limit = radius * Mathf.Cos(Mathf.PI / sides) / Mathf.Cos(local);
            return dist <= limit;
        }
    }
}
