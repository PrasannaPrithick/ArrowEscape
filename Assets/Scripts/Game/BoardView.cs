using System;
using System.Collections;
using System.Collections.Generic;
using HueGates.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace HueGates.Game
{
    public sealed class BoardView : MonoBehaviour
    {
        public event Action<int> Tapped;

        PlaceholderSprites art;
        Level level;
        float pitch;
        int buildId;
        bool locked;
        int pulseGen;
        bool showSymbols;
        int gateCleared;

        const float GlowFit = 1.38f;

        static Shader emissionShader;

        RectTransform[] arrows;
        Vector2[] homes;
        Vector2[] steps;
        Image[] arrowImages;
        Material[] glowMats;
        bool[] ready;
        float[] glowNow;
        float[] glowKick;
        float[] glowPulse;
        Image[] cells;
        int[] arrowCell;
        Sprite cellDefault;
        Sprite cellLit;
        Material cellHotMat;
        static Shader cellShader;
        Image[] symbols;
        Button[] buttons;
        bool[] busy;

        Image[] top;
        Image[] bottom;
        Image[] left;
        Image[] right;
        Image[] topSymbol;
        Image[] bottomSymbol;
        Image[] leftSymbol;
        Image[] rightSymbol;

        public bool IsBusy(int index)
        {
            return busy != null && index >= 0 && index < busy.Length && busy[index];
        }

        void OnDestroy()
        {
            ReleaseGlow();
            if (cellHotMat != null) Destroy(cellHotMat);
        }

        public void Build(Level level, RectTransform host, PlaceholderSprites art, UiKit kit)
        {
            StopAllCoroutines();
            ReleaseGlow();
            buildId++;
            pulseGen++;
            this.level = level;
            this.art = art;
            locked = false;
            kit.Clear(host);

            int n = level.size;
            float width = host.rect.width;
            float height = host.rect.height;
            if (width < 32f) width = 1000f;
            if (height < 32f) height = 1100f;
            pitch = Mathf.Min(width, height) / (n + 2.2f);

            float gate = pitch * 1.05f;
            float gap = pitch * 0.05f;
            float grid = pitch * n;
            float total = gate + gap + grid + gap + gate;
            float tile = pitch * 0.9f;

            RectTransform root = kit.Element(host, "board");
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(total, total);

            int count = level.arrows.Length;
            arrows = new RectTransform[count];
            homes = new Vector2[count];
            steps = new Vector2[count];
            arrowImages = new Image[count];
            glowMats = new Material[count];
            ready = new bool[count];
            glowNow = new float[count];
            glowKick = new float[count];
            glowPulse = new float[count];
            symbols = new Image[count];
            buttons = new Button[count];
            busy = new bool[count];

            cells = new Image[n * n];
            arrowCell = new int[count];
            cellDefault = PlaceholderSprites.Prefer("Art/Board/cell_bg", art.cell);
            cellLit = PlaceholderSprites.Prefer("Art/Board/cell_lit", cellDefault);
            EnsureCellMaterials();
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    Vector2 center = CellCenter(r, c, n, gate, gap);
                    Image cell = kit.Image(root, cellDefault, Color.white, "cell");
                    cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    cell.rectTransform.sizeDelta = new Vector2(tile, tile);
                    cell.rectTransform.anchoredPosition = center;
                    cells[r * n + c] = cell;
                }
            }

            top = new Image[n];
            bottom = new Image[n];
            left = new Image[n];
            right = new Image[n];
            topSymbol = new Image[n];
            bottomSymbol = new Image[n];
            leftSymbol = new Image[n];
            rightSymbol = new Image[n];
            Sprite gateSprite = PlaceholderSprites.Prefer("Art/Board/gate", art.bar);
            Vector2 icon = new Vector2(tile * 0.98f, gate * 0.96f);
            for (int i = 0; i < n; i++)
            {
                top[i] = Gate(kit, root, gateSprite, new Vector2(CellCenter(0, i, n, gate, gap).x, total * 0.5f - gate * 0.5f), icon, 180f);
                bottom[i] = Gate(kit, root, gateSprite, new Vector2(CellCenter(n - 1, i, n, gate, gap).x, -total * 0.5f + gate * 0.5f), icon, 0f);
                left[i] = Gate(kit, root, gateSprite, new Vector2(-total * 0.5f + gate * 0.5f, CellCenter(i, 0, n, gate, gap).y), icon, -90f);
                right[i] = Gate(kit, root, gateSprite, new Vector2(total * 0.5f - gate * 0.5f, CellCenter(i, 0, n, gate, gap).y), icon, 90f);
                topSymbol[i] = SymbolOn(kit, top[i].rectTransform, pitch * 0.22f, icon.y * 0.28f, -180f);
                bottomSymbol[i] = SymbolOn(kit, bottom[i].rectTransform, pitch * 0.22f, icon.y * 0.28f, 0f);
                leftSymbol[i] = SymbolOn(kit, left[i].rectTransform, pitch * 0.22f, icon.y * 0.28f, 90f);
                rightSymbol[i] = SymbolOn(kit, right[i].rectTransform, pitch * 0.22f, icon.y * 0.28f, -90f);
            }

            var occupied = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
            {
                Arrow arrow = level.arrows[i];
                occupied[arrow.r * n + arrow.c] = i;
            }

            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    int key = r * n + c;
                    if (!occupied.ContainsKey(key)) continue;
                    int index = occupied[key];
                    arrowCell[index] = key;
                    Arrow arrow = level.arrows[index];
                    Vector2 center = CellCenter(r, c, n, gate, gap);
                    RectTransform hit = kit.Element(root, "hit");
                    hit.anchorMin = hit.anchorMax = new Vector2(0.5f, 0.5f);
                    hit.sizeDelta = new Vector2(pitch, pitch);
                    hit.anchoredPosition = center;
                    Image hitImage = hit.gameObject.AddComponent<Image>();
                    hitImage.color = new Color(1f, 1f, 1f, 0f);
                    Button cellButton = hit.gameObject.AddComponent<Button>();
                    cellButton.targetGraphic = hitImage;
                    int captured = index;
                    cellButton.onClick.AddListener(() => { if (Tapped != null) Tapped(captured); });
                    buttons[index] = cellButton;

                    RectTransform arrowRoot = kit.Element(root, "arrow");
                    arrowRoot.anchorMin = arrowRoot.anchorMax = new Vector2(0.5f, 0.5f);
                    arrowRoot.sizeDelta = new Vector2(tile * 0.92f, tile * 0.92f);
                    arrowRoot.anchoredPosition = center;
                    float angle = Angle(arrow.dir);
                    arrowRoot.localEulerAngles = new Vector3(0f, 0f, angle);

                    string paletteName = level.palette[arrow.color];
                    Color tint = HueTheme.Palette(paletteName);

                    Sprite colored = PlaceholderSprites.Prefer("Art/Board/arrow_" + paletteName, null);
                    Sprite glossy = PlaceholderSprites.Prefer("Art/Board/arrow", art.arrow);
                    Image body = kit.Image(arrowRoot, colored != null ? colored : glossy, colored != null ? Color.white : tint, "body");
                    kit.Stretch(body.rectTransform);

                    Image mark = kit.Image(arrowRoot, SymbolSprite(paletteName), Color.white, "mark");
                    mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    mark.rectTransform.sizeDelta = new Vector2(pitch * 0.22f, pitch * 0.22f);
                    mark.rectTransform.anchoredPosition = new Vector2(0f, -tile * 0.06f);
                    mark.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
                    mark.enabled = false;

                    arrows[index] = arrowRoot;
                    homes[index] = center;
                    steps[index] = Step(arrow.dir) * pitch;
                    arrowImages[index] = body;
                    symbols[index] = mark;
                }
            }
        }

        public void Sync(GameState state, bool assist, bool colorblind)
        {
            StopAllCoroutines();
            buildId++;
            locked = false;
            for (int i = 0; i < arrows.Length; i++)
            {
                busy[i] = false;
                ready[i] = false;
                glowKick[i] = 0f;
                glowPulse[i] = 0f;
                glowNow[i] = 0.22f;
                arrows[i].anchoredPosition = homes[i];
                arrows[i].localScale = Vector3.one;
                arrows[i].gameObject.SetActive(state.remaining[i]);
                buttons[i].interactable = state.remaining[i];
            }
            RestoreCells();
            ApplyGates(state.cleared, false, -1);
            ApplyAssist(state, assist, colorblind);
        }

        public void SetLocked(bool value)
        {
            locked = value;
            RefreshButtons(null);
        }

        public void ApplyAssist(GameState state, bool assist, bool colorblind)
        {
            showSymbols = colorblind;
            for (int i = 0; i < arrows.Length; i++)
            {
                ready[i] = state.remaining[i] && assist && Engine.GateMatches(state, i);
                symbols[i].enabled = colorblind;
            }
            RefreshGateSymbols();
        }

        public void ApplyGates(int cleared, bool animate, int pulseArrow)
        {
            if (!animate)
            {
                PaintGates(cleared);
                return;
            }
            StartCoroutine(FadeGates(cleared, pulseArrow, buildId));
        }

        public Coroutine AnimateClear(int index)
        {
            return StartCoroutine(ClearRoutine(index, buildId));
        }

        public Coroutine AnimateBlocked(int index, int blocker)
        {
            return StartCoroutine(BlockedRoutine(index, blocker, buildId));
        }

        public Coroutine AnimateDenied(int index)
        {
            return StartCoroutine(DeniedRoutine(index, buildId));
        }

        public void PulseLoop(int index)
        {
            pulseGen++;
            StartCoroutine(PulseRoutine(index, pulseGen, true, 0f));
        }

        public void PulseOnce(int index, float seconds)
        {
            pulseGen++;
            StartCoroutine(PulseRoutine(index, pulseGen, false, seconds));
        }

        public void StopPulse()
        {
            pulseGen++;
            if (arrows == null) return;
            for (int i = 0; i < arrows.Length; i++)
            {
                if (glowPulse != null) glowPulse[i] = 0f;
                if (arrows[i] != null) arrows[i].localScale = Vector3.one;
            }
        }

        IEnumerator ClearRoutine(int index, int build)
        {
            busy[index] = true;
            buttons[index].interactable = false;
            ShowHotCell(index);
            Vector2 start = homes[index];
            int path = Engine.PathCells(level.size, level.arrows[index]).Count;
            Vector2 end = start + steps[index].normalized * pitch * (path + 1.15f);
            float duration = 0.28f;
            float t = 0f;
            while (t < duration)
            {
                if (build != buildId) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float eased = k * k * k;
                float ignite = Mathf.Sin(Mathf.Clamp01(k * 2.4f) * Mathf.PI);
                arrows[index].anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
                arrows[index].localScale = Vector3.one * (1f + 0.07f * ignite);
                DriveHotCell(Mathf.Lerp(1.2f, 2.3f, ignite));
                yield return null;
            }
            if (build != buildId) yield break;
            arrows[index].gameObject.SetActive(false);
            RestoreCells();
            busy[index] = false;
        }

        IEnumerator BlockedRoutine(int index, int blocker, int build)
        {
            busy[index] = true;
            buttons[index].interactable = false;
            ShowHotCell(index);
            Vector2 start = homes[index];
            Vector2 nudge = steps[index].normalized * pitch * 0.25f;
            float duration = 0.24f;
            float t = 0f;
            while (t < duration)
            {
                if (build != buildId) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float wave = Mathf.Sin(k * Mathf.PI);
                float hit = wave * wave;
                arrows[index].anchoredPosition = start + nudge * wave;
                DriveHotCell(0.85f + hit * 1.55f);
                if (blocker >= 0 && arrows[blocker] != null)
                {
                    arrows[blocker].localScale = Vector3.one * (1f + 0.1f * hit);
                }
                yield return null;
            }
            if (build != buildId) yield break;
            arrows[index].anchoredPosition = start;
            if (blocker >= 0 && arrows[blocker] != null) arrows[blocker].localScale = Vector3.one;
            RestoreCells();
            busy[index] = false;
            buttons[index].interactable = !locked;
        }

        IEnumerator DeniedRoutine(int index, int build)
        {
            busy[index] = true;
            buttons[index].interactable = false;
            ShowHotCell(index);
            Vector2 start = homes[index];
            int path = Engine.PathCells(level.size, level.arrows[index]).Count;
            Vector2 end = start + steps[index].normalized * (path * pitch + pitch * 0.35f);
            Image gate = ExitImage(level.arrows[index]);
            Color gateColor = gate.color;
            float t = 0f;
            const float total = 0.44f;
            while (t < total)
            {
                if (build != buildId) yield break;
                t += Time.deltaTime;
                float heat;
                if (t < 0.2f)
                {
                    float k = Mathf.Clamp01(t / 0.2f);
                    float eased = k * k;
                    heat = eased;
                    arrows[index].anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
                }
                else if (t < 0.26f)
                {
                    heat = 1f;
                    arrows[index].anchoredPosition = end;
                    gate.color = Color.Lerp(gateColor, new Color(0.15f, 0.16f, 0.24f, 1f), (t - 0.2f) / 0.06f);
                }
                else
                {
                    float k = Mathf.Clamp01((t - 0.26f) / 0.18f);
                    float eased = 1f - (1f - k) * (1f - k);
                    heat = 1f - eased;
                    arrows[index].anchoredPosition = Vector2.LerpUnclamped(end, start, eased);
                    gate.color = Color.Lerp(new Color(0.15f, 0.16f, 0.24f, 1f), gateColor, eased);
                }
                DriveHotCell(0.9f + heat * 1.5f);
                yield return null;
            }
            if (build != buildId) yield break;
            arrows[index].anchoredPosition = start;
            gate.color = gateColor;
            RestoreCells();
            busy[index] = false;
            buttons[index].interactable = !locked;
        }

        IEnumerator FadeGates(int cleared, int pulseArrow, int build)
        {
            int n = level.size;
            Color[] fromT = Colors(top);
            Color[] fromB = Colors(bottom);
            Color[] fromL = Colors(left);
            Color[] fromR = Colors(right);
            Color[] toT = Target(level.gates.T, cleared);
            Color[] toB = Target(level.gates.B, cleared);
            Color[] toL = Target(level.gates.L, cleared);
            Color[] toR = Target(level.gates.R, cleared);
            GateSide pulseSide = GateSide.T;
            int pulseIndex = -1;
            if (pulseArrow >= 0)
                Engine.ExitGate(level.size, level.arrows[pulseArrow], out pulseSide, out pulseIndex);

            float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                if (build != buildId) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                for (int i = 0; i < n; i++)
                {
                    top[i].color = Blend(fromT[i], toT[i], k, pulseSide == GateSide.T && pulseIndex == i);
                    bottom[i].color = Blend(fromB[i], toB[i], k, pulseSide == GateSide.B && pulseIndex == i);
                    left[i].color = Blend(fromL[i], toL[i], k, pulseSide == GateSide.L && pulseIndex == i);
                    right[i].color = Blend(fromR[i], toR[i], k, pulseSide == GateSide.R && pulseIndex == i);
                }
                yield return null;
            }
            if (build != buildId) yield break;
            PaintGates(cleared);
        }

        IEnumerator PulseRoutine(int index, int gen, bool loop, float seconds)
        {
            float elapsed = 0f;
            while (gen == pulseGen && arrows[index] != null && arrows[index].gameObject.activeSelf)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * 5.2f);
                float bloom = wave * wave;
                arrows[index].localScale = Vector3.one * (1f + 0.035f * bloom);
                if (!loop)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed >= seconds) break;
                }
                yield return null;
            }
            if (gen == pulseGen && arrows != null && arrows[index] != null)
            {
                arrows[index].localScale = Vector3.one;
                if (glowPulse != null) glowPulse[index] = 0f;
            }
        }

        void PaintGates(int cleared)
        {
            gateCleared = cleared;
            PaintRow(top, level.gates.T, cleared);
            PaintRow(bottom, level.gates.B, cleared);
            PaintRow(left, level.gates.L, cleared);
            PaintRow(right, level.gates.R, cleared);
            RefreshGateSymbols();
        }

        void RefreshGateSymbols()
        {
            if (topSymbol == null) return;
            MarkRow(topSymbol, level.gates.T);
            MarkRow(bottomSymbol, level.gates.B);
            MarkRow(leftSymbol, level.gates.L);
            MarkRow(rightSymbol, level.gates.R);
        }

        void MarkRow(Image[] images, int[] bases)
        {
            for (int i = 0; i < images.Length; i++)
            {
                images[i].enabled = showSymbols;
                if (!showSymbols) continue;
                int colorIndex = (bases[i] + gateCleared) % level.colors;
                images[i].sprite = SymbolSprite(level.palette[colorIndex]);
                images[i].color = Color.white;
            }
        }

        void PaintRow(Image[] images, int[] bases, int cleared)
        {
            for (int i = 0; i < images.Length; i++)
            {
                int colorIndex = (bases[i] + cleared) % level.colors;
                images[i].color = HueTheme.Palette(level.palette[colorIndex]);
            }
        }

        Sprite SymbolSprite(string name)
        {
            switch (name)
            {
                case "coral": return PlaceholderSprites.Prefer("Art/Board/symbol_circle", art.symbolCircle);
                case "sun": return PlaceholderSprites.Prefer("Art/Board/symbol_triangle", art.symbolTriangle);
                case "mint": return PlaceholderSprites.Prefer("Art/Board/symbol_square", art.symbolSquare);
                case "sky": return PlaceholderSprites.Prefer("Art/Board/symbol_diamond", art.symbolDiamond);
                case "violet": return PlaceholderSprites.Prefer("Art/Board/symbol_star", art.symbolStar);
                default: return art.symbolCircle;
            }
        }

        Image ExitImage(Arrow arrow)
        {
            GateSide side;
            int index;
            Engine.ExitGate(level.size, arrow, out side, out index);
            switch (side)
            {
                case GateSide.T: return top[index];
                case GateSide.B: return bottom[index];
                case GateSide.L: return left[index];
                default: return right[index];
            }
        }

        void RefreshButtons(GameState state)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool alive = state == null || state.remaining[i];
                buttons[i].interactable = alive && !locked && !busy[i];
            }
        }

        void EnsureCellMaterials()
        {
            if (cellShader == null)
                cellShader = Resources.Load<Shader>("Shaders/CellCorner");
            if (cellShader == null) return;
            if (cellHotMat == null)
            {
                cellHotMat = new Material(cellShader);
                cellHotMat.SetFloat("_Glow", 1.8f);
            }
        }

        void ShowHotCell(int index)
        {
            if (cells == null || arrowCell == null || index < 0 || index >= arrowCell.Length) return;
            Image cell = cells[arrowCell[index]];
            if (cell == null) return;
            if (cellLit != null) cell.sprite = cellLit;
            if (cellHotMat != null) cell.material = cellHotMat;
        }

        void DriveHotCell(float glow)
        {
            if (cellHotMat != null) cellHotMat.SetFloat("_Glow", glow);
        }

        void RestoreCells()
        {
            if (cells == null || cellDefault == null) return;
            for (int i = 0; i < cells.Length; i++)
            {
                Image cell = cells[i];
                if (cell == null) continue;
                cell.sprite = cellDefault;
                cell.material = null;
            }
        }

        static Shader EmissionShader()
        {
            if (emissionShader == null)
                emissionShader = Resources.Load<Shader>("Shaders/ArrowEmission");
            return emissionShader;
        }

        static Material AttachEmission(Image body, Color glowColor)
        {
            Shader shader = EmissionShader();
            if (shader == null) return null;
            Material mat = new Material(shader);
            mat.SetFloat("_Glow", 1.25f);
            mat.SetFloat("_Fit", GlowFit);
            mat.SetColor("_GlowColor", glowColor);
            body.material = mat;
            return mat;
        }

        void ReleaseGlow()
        {
            if (glowMats == null) return;
            for (int i = 0; i < glowMats.Length; i++)
            {
                if (glowMats[i] != null) Destroy(glowMats[i]);
            }
            glowMats = null;
        }

        static Image Gate(UiKit kit, Transform parent, Sprite sprite, Vector2 position, Vector2 size, float angle)
        {
            Image image = kit.Image(parent, sprite, Color.white, "gate");
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
            return image;
        }

        static Image SymbolOn(UiKit kit, RectTransform parent, float size, float trayOffset, float upright)
        {
            Image image = kit.Image(parent, kit.art.symbolCircle, Color.white, "symbol");
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.sizeDelta = new Vector2(size, size);
            image.rectTransform.anchoredPosition = new Vector2(0f, trayOffset);
            image.rectTransform.localEulerAngles = new Vector3(0f, 0f, upright);
            image.enabled = false;
            return image;
        }

        Vector2 CellCenter(int r, int c, int n, float gate, float gap)
        {
            float grid = pitch * n;
            float total = gate + gap + grid + gap + gate;
            float x = -total * 0.5f + gate + gap + pitch * (c + 0.5f);
            float y = total * 0.5f - gate - gap - pitch * (r + 0.5f);
            return new Vector2(x, y);
        }

        static Vector2 Step(Dir dir)
        {
            switch (dir)
            {
                case Dir.U: return Vector2.up;
                case Dir.D: return Vector2.down;
                case Dir.L: return Vector2.left;
                default: return Vector2.right;
            }
        }

        static float Angle(Dir dir)
        {
            switch (dir)
            {
                case Dir.U: return 0f;
                case Dir.D: return 180f;
                case Dir.L: return 90f;
                default: return -90f;
            }
        }

        static Color[] Colors(Image[] images)
        {
            var colors = new Color[images.Length];
            for (int i = 0; i < images.Length; i++) colors[i] = images[i].color;
            return colors;
        }

        Color[] Target(int[] bases, int cleared)
        {
            var colors = new Color[bases.Length];
            for (int i = 0; i < bases.Length; i++)
            {
                int colorIndex = (bases[i] + cleared) % level.colors;
                colors[i] = HueTheme.Palette(level.palette[colorIndex]);
            }
            return colors;
        }

        static Color Blend(Color from, Color to, float t, bool pulse)
        {
            if (!pulse) return Color.Lerp(from, to, t);
            if (t < 0.45f) return Color.Lerp(from, Color.white, t / 0.45f);
            return Color.Lerp(Color.white, to, (t - 0.45f) / 0.55f);
        }
    }
}
