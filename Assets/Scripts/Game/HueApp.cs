using System;
using System.Collections;
using System.Collections.Generic;
using HueGates.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HueGates.Game
{
    public sealed class HueApp : MonoBehaviour
    {
        public static HueApp Instance { get; private set; }

        SaveData save;
        LevelSet catalog;
        Picture[] pictures = new Picture[0];
        FakeAds ads;
        FakeStore store;
        HueAudio audio;
        UiKit kit;
        PlaceholderSprites sprites;

        RectTransform homeScreen;
        RectTransform levelScreen;
        RectTransform galleryScreen;
        RectTransform detailScreen;
        RectTransform shopScreen;
        RectTransform settingsScreen;
        RectTransform playScreen;
        RectTransform modal;

        Text homeProgress;
        Text playTitle;
        Text playScore;
        Text playCombo;
        Text playTip;
        Text undoLabel;
        Text hintLabel;
        Image[] hearts;
        RectTransform boardHost;
        BoardView board;
        RectTransform stuckCard;
        RectTransform failCard;
        RectTransform winCard;
        Text failReviveLabel;
        Text winHeading;
        Text winCopy;
        RectTransform winPixels;
        Image[] winStars;
        Text modalTitle;
        Text modalBody;
        Text modalOkLabel;
        Text modalCancelLabel;
        Action modalOk;
        Action modalCancel;

        Level level;
        GameState state;
        bool reviveUsed;
        int combo = 1;
        int score;
        bool won;
        bool failed;
        bool interstitialArmed;
        int playToken;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<HueApp>() != null) return;
            var go = new GameObject("HueApp");
            go.AddComponent<HueApp>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            sprites = PlaceholderSprites.Build();
            kit = new UiKit { font = LoadFont(), art = sprites };
            save = SaveFile.Load();
            store = new FakeStore();
            ads = new FakeAds(PresentAd);
            ads.Removed = SaveFile.Owns(save, "remove_ads") || SaveFile.Owns(save, "starter_bundle");
            audio = gameObject.AddComponent<HueAudio>();
            audio.Boot(save.settings);
            LoadContent();
            EnsureEventSystem();
            BuildCanvas();
            ShowHome();
        }

        void LoadContent()
        {
            TextAsset levelsAsset = Resources.Load<TextAsset>("Data/levels_1_25");
            if (levelsAsset == null)
            {
                catalog = new LevelSet { levels = new Level[0], game = "Arrow Escape" };
            }
            else
            {
                catalog = LevelCatalog.Parse(levelsAsset.text);
            }

            TextAsset[] files = Resources.LoadAll<TextAsset>("Gallery");
            var loaded = new List<Picture>();
            for (int i = 0; i < files.Length; i++)
            {
                try { loaded.Add(LevelCatalog.ParsePicture(files[i].text)); }
                catch (Exception exception) { Debug.LogWarning(exception.Message); }
            }
            loaded.Sort((a, b) => a.order.CompareTo(b.order));
            pictures = loaded.ToArray();
        }

        static Font LoadFont()
        {
            Font font = Resources.Load<Font>("Fonts/Fredoka-Medium");
            if (font != null) return font;
            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (Exception) { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        void BuildCanvas()
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = HueTheme.Background;
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform root = canvasGo.GetComponent<RectTransform>();
            Image background = kit.Image(root, sprites.rounded, HueTheme.Background, "bg");
            kit.Stretch(background.rectTransform);
            background.raycastTarget = true;

            homeScreen = FullScreen(root, "home");
            levelScreen = FullScreen(root, "levels");
            galleryScreen = FullScreen(root, "gallery");
            detailScreen = FullScreen(root, "detail");
            shopScreen = FullScreen(root, "shop");
            settingsScreen = FullScreen(root, "settings");
            playScreen = FullScreen(root, "play");
            BuildPlay(playScreen);
            BuildModal(root);
            board = gameObject.AddComponent<BoardView>();
            board.Tapped += OnArrow;
        }

        RectTransform FullScreen(Transform parent, string name)
        {
            RectTransform screen = kit.Element(parent, name);
            kit.Stretch(screen);
            return screen;
        }

        void ShowOnly(RectTransform screen)
        {
            homeScreen.gameObject.SetActive(screen == homeScreen);
            levelScreen.gameObject.SetActive(screen == levelScreen);
            galleryScreen.gameObject.SetActive(screen == galleryScreen);
            detailScreen.gameObject.SetActive(screen == detailScreen);
            shopScreen.gameObject.SetActive(screen == shopScreen);
            settingsScreen.gameObject.SetActive(screen == settingsScreen);
            playScreen.gameObject.SetActive(screen == playScreen);
            modal.gameObject.SetActive(false);
        }

        public void ShowHome()
        {
            ShowOnly(homeScreen);
            kit.Clear(homeScreen);
            kit.Column(homeScreen, 48, 24);
            LayoutElement spacer = kit.Height(kit.Element(homeScreen, "spacer"), 40f, 1f);
            spacer.minHeight = 20f;
            Text title = kit.Label(homeScreen, "Arrow Escape", 84, FontStyle.Bold, TextAnchor.MiddleCenter, HueTheme.Ink);
            kit.Height(title, 100f);
            Text sub = kit.Label(homeScreen, "Slide each arrow out through a gate of its own color.", 32, FontStyle.Normal, TextAnchor.MiddleCenter, HueTheme.Muted);
            kit.Height(sub, 90f);
            homeProgress = kit.Label(homeScreen, ProgressLabel(), 30, FontStyle.Bold, TextAnchor.MiddleCenter, HueTheme.Mint);
            kit.Height(homeProgress, 48f);
            Button play = kit.Button(homeScreen, "Play  " + Frontier(), HueTheme.Mint, HueTheme.Navy, 40, () => OpenLevel(Frontier()));
            kit.Height(play, 120f);
            Button levels = kit.Button(homeScreen, "Levels", HueTheme.Panel, HueTheme.Ink, 34, ShowLevels);
            kit.Height(levels, 96f);
            RectTransform row = kit.Element(homeScreen, "row");
            kit.Height(row, 96f);
            kit.Row(row, 16);
            kit.Button(row, "Gallery", HueTheme.Panel, HueTheme.Ink, 28, ShowGallery);
            kit.Button(row, "Shop", HueTheme.Panel, HueTheme.Ink, 28, ShowShop);
            kit.Button(row, "Settings", HueTheme.Panel, HueTheme.Ink, 28, ShowSettings);
            kit.Height(kit.Element(homeScreen, "spacer2"), 20f, 1f);
            if (catalog.levels.Length == 0)
            {
                Text error = kit.Label(homeScreen, "Level data did not load.", 28, FontStyle.Bold, TextAnchor.MiddleCenter, HueTheme.Coral);
                kit.Height(error, 60f);
            }
        }

        void ShowLevels()
        {
            ShowOnly(levelScreen);
            kit.Clear(levelScreen);
            kit.Column(levelScreen, 32, 16);
            Header(levelScreen, "Levels", ShowHome);
            RectTransform scroll = kit.Scroll(levelScreen, out RectTransform content);
            kit.Height(scroll, 200f, 1f);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180f, 180f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.padding = new RectOffset(8, 8, 8, 24);
            int frontier = Frontier();
            for (int i = 0; i < catalog.levels.Length; i++)
            {
                Level entry = catalog.levels[i];
                bool open = entry.id <= frontier;
                int stars = SaveFile.StarsFor(save, entry.id);
                string label = open ? entry.id + "\n" + StarText(stars) : entry.id + "\nlocked";
                Color face = open ? HueTheme.Panel : new Color(0.12f, 0.13f, 0.2f, 1f);
                int id = entry.id;
                Button button = kit.Button(content, label, face, open ? HueTheme.Ink : HueTheme.Muted, 32, () => { if (open) OpenLevel(id); });
                button.interactable = open;
            }
        }

        void ShowGallery()
        {
            ShowOnly(galleryScreen);
            kit.Clear(galleryScreen);
            kit.Column(galleryScreen, 32, 16);
            Header(galleryScreen, "Gallery", ShowHome);
            RectTransform scroll = kit.Scroll(galleryScreen, out RectTransform content);
            kit.Height(scroll, 200f, 1f);
            VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 20f;
            column.padding = new RectOffset(8, 8, 8, 40);
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            bool pack = SaveFile.Owns(save, "picture_pack_1");
            for (int i = 0; i < pictures.Length; i++)
            {
                Picture picture = pictures[i];
                if (picture.order >= 3 && !pack) continue;
                int revealed = RevealedCount(picture, pack);
                string caption = Pretty(picture.name) + "    " + revealed + "/" + picture.pieces.Length;
                Button button = kit.Button(content, caption, HueTheme.Panel, HueTheme.Ink, 30, () => ShowDetail(picture));
                kit.Height(button, 88f);
            }
            if (!pack)
            {
                Button locked = kit.Button(content, "20 more pictures  ·  Shop", HueTheme.Panel, HueTheme.Muted, 28, ShowShop);
                kit.Height(locked, 88f);
            }
        }

        void ShowDetail(Picture picture)
        {
            ShowOnly(detailScreen);
            kit.Clear(detailScreen);
            kit.Column(detailScreen, 32, 16);
            Header(detailScreen, Pretty(picture.name), ShowGallery);
            bool pack = picture.order >= 3 && SaveFile.Owns(save, "picture_pack_1");
            int revealed = RevealedCount(picture, pack || picture.order >= 3 && SaveFile.Owns(save, "picture_pack_1"));
            Text count = kit.Label(detailScreen, revealed + " of " + picture.pieces.Length + " pieces", 28, FontStyle.Normal, TextAnchor.MiddleCenter, HueTheme.Muted);
            kit.Height(count, 40f);
            RectTransform host = kit.Element(detailScreen, "pixels");
            kit.Height(host, 860f);
            Canvas.ForceUpdateCanvases();
            PaintPicture(host, picture, save.highestLevelCompleted, picture.order >= 3, -1, null);
        }

        void ShowShop()
        {
            ShowOnly(shopScreen);
            kit.Clear(shopScreen);
            kit.Column(shopScreen, 32, 16);
            Header(shopScreen, "Shop", ShowHome);
            Text note = kit.Label(shopScreen, "Prices are placeholders. Purchases in this build are instant so the game can be played before the store SDKs are connected.", 26, FontStyle.Normal, TextAnchor.MiddleCenter, HueTheme.Muted);
            kit.Height(note, 120f);
            RectTransform scroll = kit.Scroll(shopScreen, out RectTransform content);
            kit.Height(scroll, 200f, 1f);
            VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 16f;
            column.padding = new RectOffset(8, 8, 8, 40);
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            for (int i = 0; i < ShopCatalog.Products.Length; i++)
            {
                ProductInfo product = ShopCatalog.Products[i];
                bool owned = !product.consumable && (SaveFile.Owns(save, product.id) || (product.id != "starter_bundle" && CoveredByBundle(product.id)));
                string label = owned ? product.title + "\nOwned" : product.title + "    " + product.price + "\n" + product.detail;
                string id = product.id;
                Button button = kit.Button(content, label, owned ? HueTheme.Panel : HueTheme.Mint, owned ? HueTheme.Muted : HueTheme.Navy, 28, () => Buy(id));
                kit.Height(button, 130f);
                button.interactable = !owned;
            }
        }

        void ShowSettings()
        {
            ShowOnly(settingsScreen);
            kit.Clear(settingsScreen);
            kit.Column(settingsScreen, 32, 16);
            Header(settingsScreen, "Settings", ShowHome);
            AddToggle(settingsScreen, "Sound    " + OnOff(save.settings.sound), () => { save.settings.sound = !save.settings.sound; Persist(); ShowSettings(); });
            AddToggle(settingsScreen, "Music    " + OnOff(save.settings.music), () => { save.settings.music = !save.settings.music; audio.Apply(); Persist(); ShowSettings(); });
            AddToggle(settingsScreen, "Haptics    " + OnOff(save.settings.haptics), () => { save.settings.haptics = !save.settings.haptics; Persist(); ShowSettings(); });
            AddToggle(settingsScreen, GlowLabel(), ToggleGlow);
            AddToggle(settingsScreen, "Color-blind symbols    " + OnOff(save.settings.colorblindSymbols), () => { save.settings.colorblindSymbols = !save.settings.colorblindSymbols; Persist(); ShowSettings(); });
            Text help = kit.Label(settingsScreen, "Ready glow marks an arrow whose gate currently matches its color. It does not check whether another arrow is in the way. It is on for levels 1–15 until you set it here.", 26, FontStyle.Normal, TextAnchor.UpperLeft, HueTheme.Muted);
            kit.Height(help, 180f);
        }

        void BuildPlay(RectTransform screen)
        {
            RectTransform column = kit.Element(screen, "column");
            kit.Stretch(column);
            kit.Column(column, 28, 12);
            RectTransform top = kit.Element(column, "top");
            kit.Height(top, 88f);
            kit.Row(top, 12);
            kit.Button(top, "Home", HueTheme.Panel, HueTheme.Ink, 26, LeaveLevel);
            playTitle = kit.Label(top, "Level", 36, FontStyle.Bold, TextAnchor.MiddleCenter, HueTheme.Ink);
            playScore = kit.Label(top, "0", 28, FontStyle.Bold, TextAnchor.MiddleRight, HueTheme.Sun);

            RectTransform heartRow = kit.Element(column, "hearts");
            kit.Height(heartRow, 64f);
            HorizontalLayoutGroup heartsLayout = heartRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            heartsLayout.childAlignment = TextAnchor.MiddleCenter;
            heartsLayout.spacing = 8f;
            heartsLayout.childControlWidth = false;
            heartsLayout.childControlHeight = false;
            heartsLayout.childForceExpandWidth = false;
            heartsLayout.childForceExpandHeight = false;
            hearts = new Image[3];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = kit.Image(heartRow, sprites.heart, HueTheme.Coral, "heart");
                hearts[i].rectTransform.sizeDelta = new Vector2(56f, 56f);
                LayoutElement box = hearts[i].gameObject.AddComponent<LayoutElement>();
                box.preferredWidth = 56f;
                box.preferredHeight = 56f;
            }
            playCombo = kit.Label(heartRow, "", 28, FontStyle.Bold, TextAnchor.MiddleLeft, HueTheme.Mint);

            playTip = kit.Label(column, "", 28, FontStyle.Normal, TextAnchor.MiddleCenter, HueTheme.Ink);
            kit.Height(playTip, 120f);

            boardHost = kit.Element(column, "board");
            LayoutElement boardSpace = boardHost.gameObject.AddComponent<LayoutElement>();
            boardSpace.flexibleHeight = 1f;
            boardSpace.minHeight = 640f;

            stuckCard = kit.Element(column, "stuck");
            kit.Height(stuckCard, 150f);
            Image stuckBg = stuckCard.gameObject.AddComponent<Image>();
            stuckBg.sprite = sprites.rounded;
            stuckBg.type = Image.Type.Sliced;
            stuckBg.color = HueTheme.Panel;
            kit.Column(stuckCard, 12, 8);
            Text stuckText = kit.Label(stuckCard, "No moves left", 30, FontStyle.Bold, TextAnchor.MiddleCenter, HueTheme.Ink);
            kit.Height(stuckText, 40f);
            RectTransform stuckRow = kit.Element(stuckCard, "row");
            kit.Height(stuckRow, 64f);
            kit.Row(stuckRow, 12);
            kit.Button(stuckRow, "Undo", HueTheme.Sun, HueTheme.Navy, 26, OnUndo);
            kit.Button(stuckRow, "Restart", HueTheme.Violet, HueTheme.Ink, 26, OnRestart);

            RectTransform actions = kit.Element(column, "actions");
            kit.Height(actions, 110f);
            kit.Row(actions, 12);
            Button undo = kit.Button(actions, "Undo", HueTheme.Sun, HueTheme.Navy, 28, OnUndo);
            undoLabel = undo.GetComponentInChildren<Text>();
            Button hint = kit.Button(actions, "Hint", HueTheme.Sky, HueTheme.Navy, 28, OnHint);
            hintLabel = hint.GetComponentInChildren<Text>();
            kit.Button(actions, "Restart", HueTheme.Violet, HueTheme.Ink, 28, OnRestart);

            RectTransform failBody;
            failCard = ModalLayer(screen, out failBody, out failReviveLabel, "Out of hearts", "Revive", OnRevive, "Restart", OnRestart);
            failBody.sizeDelta = new Vector2(920f, 420f);
            RectTransform winBody;
            winCard = ModalLayer(screen, out winBody, out winHeading, "Cleared", "Next", OnNext, "Home", () => LeaveLevel());
            winCopy = kit.Label(winBody, "", 26, FontStyle.Normal, TextAnchor.MiddleCenter, HueTheme.Muted);
            winCopy.transform.SetSiblingIndex(1);
            kit.Height(winCopy, 70f);
            RectTransform starRow = kit.Element(winBody, "stars");
            starRow.SetSiblingIndex(2);
            kit.Height(starRow, 80f);
            HorizontalLayoutGroup starLayout = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            starLayout.childAlignment = TextAnchor.MiddleCenter;
            starLayout.spacing = 8f;
            starLayout.childControlWidth = false;
            starLayout.childControlHeight = false;
            starLayout.childForceExpandWidth = false;
            winStars = new Image[3];
            for (int i = 0; i < winStars.Length; i++)
            {
                winStars[i] = kit.Image(starRow, sprites.star, HueTheme.Sun, "star");
                winStars[i].rectTransform.sizeDelta = new Vector2(72f, 72f);
            }
            winPixels = kit.Element(winBody, "reveal");
            winPixels.SetSiblingIndex(3);
            kit.Height(winPixels, 280f);
            stuckCard.gameObject.SetActive(false);
            failCard.gameObject.SetActive(false);
            winCard.gameObject.SetActive(false);
        }

        RectTransform ModalLayer(Transform parent, out RectTransform card, out Text heading, string title, string ok, UnityEngine.Events.UnityAction onOk, string cancel, UnityEngine.Events.UnityAction onCancel)
        {
            RectTransform root = kit.Element(parent, title);
            kit.Stretch(root);
            Image dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0.03f, 0.04f, 0.08f, 0.72f);
            card = kit.Element(root, "card");
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(920f, 1180f);
            Image panel = card.gameObject.AddComponent<Image>();
            panel.sprite = sprites.rounded;
            panel.type = Image.Type.Sliced;
            panel.color = HueTheme.Panel;
            kit.Column(card, 24, 10);
            heading = kit.Label(card, title, 40, FontStyle.Bold, TextAnchor.MiddleCenter, HueTheme.Ink);
            kit.Height(heading, 56f);
            RectTransform row = kit.Element(card, "row");
            kit.Height(row, 84f);
            kit.Row(row, 12);
            kit.Button(row, ok, HueTheme.Mint, HueTheme.Navy, 28, onOk);
            kit.Button(row, cancel, HueTheme.Panel, HueTheme.Ink, 28, onCancel);
            return root;
        }

        void BuildModal(RectTransform root)
        {
            modal = FullScreen(root, "modal");
            Image dim = modal.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            RectTransform card = kit.Element(modal, "card");
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(860f, 520f);
            Image panel = card.gameObject.AddComponent<Image>();
            panel.sprite = sprites.rounded;
            panel.type = Image.Type.Sliced;
            panel.color = HueTheme.Panel;
            kit.Column(card, 28, 16);
            modalTitle = kit.Label(card, "Ad", 40, FontStyle.Bold, TextAnchor.MiddleCenter, HueTheme.Ink);
            kit.Height(modalTitle, 56f);
            modalBody = kit.Label(card, "", 28, FontStyle.Normal, TextAnchor.MiddleCenter, HueTheme.Muted);
            kit.Height(modalBody, 160f);
            RectTransform row = kit.Element(card, "row");
            kit.Height(row, 90f);
            kit.Row(row, 12);
            Button ok = kit.Button(row, "Continue", HueTheme.Mint, HueTheme.Navy, 28, () => { Action action = modalOk; HideModal(); if (action != null) action(); });
            modalOkLabel = ok.GetComponentInChildren<Text>();
            Button cancel = kit.Button(row, "Close", HueTheme.Panel, HueTheme.Ink, 28, () => { Action action = modalCancel; HideModal(); if (action != null) action(); });
            modalCancelLabel = cancel.GetComponentInChildren<Text>();
            modal.gameObject.SetActive(false);
        }

        void OpenLevel(int id)
        {
            if (catalog.levels.Length == 0) return;
            StopAllCoroutines();
            playToken++;
            level = LevelById(id);
            state = new GameState(level);
            reviveUsed = false;
            combo = 1;
            score = 0;
            won = false;
            failed = false;
            interstitialArmed = false;
            ShowOnly(playScreen);
            stuckCard.gameObject.SetActive(false);
            failCard.gameObject.SetActive(false);
            winCard.gameObject.SetActive(false);
            playTitle.text = "Level " + level.id;
            playTip.text = Tip(level);
            LayoutElement tipSpace = playTip.GetComponent<LayoutElement>();
            float tipHeight = string.IsNullOrEmpty(playTip.text) ? 12f : 130f;
            tipSpace.minHeight = tipHeight;
            tipSpace.preferredHeight = tipHeight;
            RefreshHud();
            int token = playToken;
            StartCoroutine(MountLevel(token));
        }

        IEnumerator MountLevel(int token)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (boardHost.rect.height < 32f)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
            }
            if (token != playToken) yield break;
            board.Build(level, boardHost, sprites, kit);
            board.Sync(state, AssistOn(), save.settings.colorblindSymbols);
            if (level.id == 1)
            {
                List<int> moves = Engine.ValidMoves(state);
                if (moves.Count > 0) board.PulseLoop(moves[0]);
            }
        }

        void OnArrow(int index)
        {
            if (won || failed || level == null || board.IsBusy(index)) return;
            if (!state.remaining[index]) return;
            board.StopPulse();
            Outcome outcome = Engine.ApplyTap(state, index);
            int token = playToken;
            if (outcome == Outcome.Cleared)
            {
                int gained = 100 * combo;
                score += gained;
                int usedCombo = combo;
                combo = Math.Min(5, combo + 1);
                audio.Tick(usedCombo);
                PulseHaptic();
                RefreshHud();
                board.ApplyAssist(state, AssistOn(), save.settings.colorblindSymbols);
                board.ApplyGates(state.cleared, true, index);
                StartCoroutine(AfterClear(index, token));
            }
            else
            {
                combo = 1;
                if (outcome == Outcome.Blocked) audio.Thud();
                else audio.Buzz();
                PulseHaptic();
                int brokenHeart = state.hearts;
                RefreshHud();
                PunchHeart(brokenHeart);
                if (Engine.IsFailed(state))
                {
                    failed = true;
                    board.SetLocked(true);
                }
                int blocker = outcome == Outcome.Blocked ? Engine.NearestBlocker(state, index) : -1;
                StartCoroutine(AfterWrong(index, outcome, blocker, token));
            }
        }

        IEnumerator AfterClear(int index, int token)
        {
            yield return board.AnimateClear(index);
            if (token != playToken) yield break;
            board.ApplyAssist(state, AssistOn(), save.settings.colorblindSymbols);
            if (Engine.IsWon(state)) ShowWin();
            else if (Engine.IsStuck(state)) ShowStuck();
        }

        IEnumerator AfterWrong(int index, Outcome outcome, int blocker, int token)
        {
            if (outcome == Outcome.Blocked) yield return board.AnimateBlocked(index, blocker);
            else yield return board.AnimateDenied(index);
            if (token != playToken) yield break;
            if (failed) ShowFail();
        }

        void ShowStuck()
        {
            if (won || failed) return;
            stuckCard.gameObject.SetActive(true);
        }

        void ShowFail()
        {
            stuckCard.gameObject.SetActive(false);
            failCard.gameObject.SetActive(true);
            failCard.SetAsLastSibling();
            failReviveLabel.text = "Out of hearts";
            Button[] buttons = failCard.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Text label = buttons[i].GetComponentInChildren<Text>();
                if (label == null) continue;
                if (label.text != "Revive" && label.text != "Used") continue;
                label.text = reviveUsed ? "Used" : "Revive";
                buttons[i].interactable = !reviveUsed;
            }
        }

        void ShowWin()
        {
            won = true;
            board.SetLocked(true);
            stuckCard.gameObject.SetActive(false);
            int stars = Stars.FromHearts(state.hearts);
            bool first = level.id > save.highestLevelCompleted;
            int previous = save.highestLevelCompleted;
            SaveFile.SetStars(save, level.id, stars);
            if (first) save.highestLevelCompleted = level.id;
            save.currentLevel = Frontier();
            Persist();
            audio.Win();
            interstitialArmed = first && ShouldInterstitial(level.id);
            winHeading.text = "Level " + level.id + " clear";
            for (int i = 0; i < winStars.Length; i++)
                winStars[i].color = i < stars ? HueTheme.Sun : new Color(1f, 1f, 1f, 0.18f);
            Picture picture = FindPicture(GalleryRules.PictureNumber(level.id));
            if (picture == null)
            {
                winCopy.text = StarText(stars) + "     score " + score;
            }
            else if (first)
            {
                winCopy.text = "Picture " + picture.order + "  ·  piece " + GalleryRules.PieceNumber(level.id);
            }
            else
            {
                winCopy.text = "Already in the gallery.  " + StarText(stars);
            }
            winCard.gameObject.SetActive(true);
            winCard.SetAsLastSibling();
            if (picture != null)
            {
                var fading = new List<Image>();
                Canvas.ForceUpdateCanvases();
                PaintPicture(winPixels, picture, previous, false, first ? GalleryRules.PieceNumber(level.id) : -1, fading);
                if (fading.Count > 0) StartCoroutine(FadePieces(fading));
            }
            StartCoroutine(Confetti(winCard));
        }

        void OnUndo()
        {
            if (won || level == null || state.history.Count == 0) return;
            if (save.undos <= 0)
            {
                ads.ShowRewarded("undo", ok =>
                {
                    if (!ok) return;
                    save.undos += 1;
                    CommitUndo();
                });
                return;
            }
            CommitUndo();
        }

        void CommitUndo()
        {
            if (save.undos <= 0 || !Engine.Undo(state)) return;
            playToken++;
            save.undos -= 1;
            failed = false;
            failCard.gameObject.SetActive(false);
            board.SetLocked(false);
            board.Sync(state, AssistOn(), save.settings.colorblindSymbols);
            stuckCard.gameObject.SetActive(Engine.IsStuck(state));
            Persist();
            RefreshHud();
        }

        void OnHint()
        {
            if (won || failed || level == null) return;
            if (save.hints <= 0)
            {
                ads.ShowRewarded("hint", ok =>
                {
                    if (!ok) return;
                    save.hints += 1;
                    CommitHint();
                });
                return;
            }
            CommitHint();
        }

        void CommitHint()
        {
            if (save.hints <= 0) return;
            if (Engine.IsStuck(state))
            {
                ShowStuck();
                return;
            }
            int arrow = -1;
            int[] solution = Solver.Solve(state);
            if (solution != null && solution.Length > 0) arrow = solution[0];
            else
            {
                List<int> moves = Engine.ValidMoves(state);
                if (moves.Count > 0) arrow = moves[0];
            }
            if (arrow < 0)
            {
                ShowStuck();
                return;
            }
            save.hints -= 1;
            Persist();
            RefreshHud();
            board.PulseOnce(arrow, 1.2f);
        }

        void OnRestart()
        {
            if (level == null) return;
            OpenLevel(level.id);
        }

        void OnRevive()
        {
            if (!failed || reviveUsed) return;
            ads.ShowRewarded("revive", ok =>
            {
                if (!ok) return;
                if (!Engine.TryRevive(state, ref reviveUsed)) return;
                failed = false;
                board.SetLocked(false);
                failCard.gameObject.SetActive(false);
                RefreshHud();
            });
        }

        void OnNext()
        {
            int finished = level.id;
            Action go = () =>
            {
                if (LevelById(finished + 1).id == finished) ShowHome();
                else OpenLevel(finished + 1);
            };
            if (!HasLevel(finished + 1)) go = ShowHome;
            if (interstitialArmed)
            {
                interstitialArmed = false;
                ads.ShowInterstitial(go);
                return;
            }
            go();
        }

        void LeaveLevel()
        {
            Action go = ShowHome;
            if (interstitialArmed)
            {
                interstitialArmed = false;
                ads.ShowInterstitial(go);
                return;
            }
            go();
        }

        void Buy(string productId)
        {
            ProductInfo info = ShopCatalog.Find(productId);
            if (info == null) return;
            if (!info.consumable && (SaveFile.Owns(save, productId) || CoveredByBundle(productId))) return;
            store.Purchase(productId, ok =>
            {
                if (!ok) return;
                Grant(productId);
                Persist();
                ShowShop();
            });
        }

        void Grant(string productId)
        {
            if (productId == "hint_pack_10") save.hints += 10;
            else if (productId == "undo_pack_10") save.undos += 10;
            else if (productId == "remove_ads") SaveFile.Own(save, "remove_ads");
            else if (productId == "picture_pack_1") SaveFile.Own(save, "picture_pack_1");
            else if (productId == "starter_bundle")
            {
                SaveFile.Own(save, "starter_bundle");
                SaveFile.Own(save, "remove_ads");
                SaveFile.Own(save, "picture_pack_1");
                save.hints += 20;
                save.undos += 20;
            }
            ads.Removed = SaveFile.Owns(save, "remove_ads");
        }

        void PresentAd(bool rewarded, string placement, Action<bool> completed)
        {
            modal.gameObject.SetActive(true);
            modal.SetAsLastSibling();
            if (rewarded)
            {
                modalTitle.text = "Rewarded ad";
                modalBody.text = RewardCopy(placement) + "\n\nThis is a placeholder. A store build plays the real ad.";
                modalOkLabel.text = "Collect";
                modalCancelLabel.text = "No thanks";
                modalCancelLabel.transform.parent.gameObject.SetActive(true);
                modalOk = () => completed(true);
                modalCancel = () => completed(false);
            }
            else
            {
                modalTitle.text = "Advertisement";
                modalBody.text = "A short break between levels.\n\nSkipped when Remove ads is owned.";
                modalOkLabel.text = "Continue";
                modalCancelLabel.transform.parent.gameObject.SetActive(false);
                modalOk = () => completed(true);
                modalCancel = null;
            }
        }

        void HideModal()
        {
            modal.gameObject.SetActive(false);
        }

        void PaintPicture(RectTransform host, Picture picture, int highest, bool revealAll, int fadePiece, List<Image> fading)
        {
            kit.Clear(host);
            GridLayoutGroup grid = host.gameObject.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = host.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = picture.size;
            grid.spacing = new Vector2(2f, 2f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            float side = Mathf.Min(host.rect.width, host.rect.height);
            if (side < 32f) side = 280f;
            float cell = (side - 2f * (picture.size - 1)) / picture.size;
            grid.cellSize = new Vector2(cell, cell);
            var fade = new HashSet<int>();
            if (fadePiece > 0 && fadePiece <= picture.pieces.Length)
            {
                int[] piece = picture.pieces[fadePiece - 1];
                for (int i = 0; i < piece.Length; i++) fade.Add(piece[i]);
            }
            for (int y = 0; y < picture.size; y++)
            {
                for (int x = 0; x < picture.size; x++)
                {
                    int index = y * picture.size + x;
                    bool shown = revealAll || fade.Contains(index) || PixelRevealed(picture, index, highest);
                    Image pixel = kit.Image(host, sprites.square, shown ? HueTheme.Hex(picture.pixels[y][x]) : HueTheme.Panel, "px");
                    if (fade.Contains(index))
                    {
                        Color color = pixel.color;
                        color.a = 0f;
                        pixel.color = color;
                        if (fading != null) fading.Add(pixel);
                    }
                }
            }
        }

        IEnumerator FadePieces(List<Image> pixels)
        {
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.45f);
                for (int i = 0; i < pixels.Count; i++)
                {
                    if (pixels[i] == null) continue;
                    Color color = pixels[i].color;
                    color.a = k;
                    if (k < 0.35f) color = Color.Lerp(Color.white, color, k / 0.35f);
                    color.a = k;
                    pixels[i].color = color;
                }
                yield return null;
            }
        }

        IEnumerator Confetti(RectTransform parent)
        {
            Color[] colors = { HueTheme.Coral, HueTheme.Sun, HueTheme.Mint, HueTheme.Sky, HueTheme.Violet };
            var bits = new List<RectTransform>();
            for (int i = 0; i < 28; i++)
            {
                Image bit = kit.Image(parent, sprites.shard, colors[i % colors.Length], "bit");
                bit.rectTransform.anchorMin = bit.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                bit.rectTransform.sizeDelta = new Vector2(UnityEngine.Random.Range(14f, 28f), UnityEngine.Random.Range(18f, 36f));
                bit.rectTransform.anchoredPosition = new Vector2(UnityEngine.Random.Range(-360f, 360f), UnityEngine.Random.Range(80f, 220f));
                bits.Add(bit.rectTransform);
            }
            float t = 0f;
            while (t < 1.1f)
            {
                t += Time.deltaTime;
                for (int i = 0; i < bits.Count; i++)
                {
                    if (bits[i] == null) continue;
                    bits[i].anchoredPosition += new Vector2(Mathf.Sin(t * 6f + i) * 0.6f, -220f * Time.deltaTime);
                    bits[i].localEulerAngles += new Vector3(0f, 0f, 90f * Time.deltaTime);
                }
                yield return null;
            }
            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i] != null) Destroy(bits[i].gameObject);
            }
        }

        void RefreshHud()
        {
            if (playScore != null) playScore.text = score.ToString();
            if (playCombo != null) playCombo.text = combo > 1 ? "x" + combo : "";
            if (undoLabel != null) undoLabel.text = "Undo  " + save.undos;
            if (hintLabel != null) hintLabel.text = "Hint  " + save.hints;
            if (hearts == null || state == null) return;
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].color = i < state.hearts ? HueTheme.Coral : new Color(1f, 1f, 1f, 0.16f);
        }

        void PunchHeart(int remainingHearts)
        {
            if (remainingHearts < 0 || remainingHearts >= hearts.Length) return;
            StartCoroutine(HeartPop(hearts[remainingHearts]));
        }

        IEnumerator HeartPop(Image heart)
        {
            float t = 0f;
            while (t < 0.25f && heart != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI);
                heart.rectTransform.localScale = Vector3.one * (1f + 0.35f * k);
                yield return null;
            }
            if (heart != null) heart.rectTransform.localScale = Vector3.one;
        }

        void Header(RectTransform screen, string title, UnityEngine.Events.UnityAction back)
        {
            RectTransform row = kit.Element(screen, "header");
            kit.Height(row, 90f);
            kit.Row(row, 12);
            Button backButton = kit.Button(row, "Back", HueTheme.Panel, HueTheme.Ink, 26, back);
            LayoutElement backWidth = backButton.gameObject.AddComponent<LayoutElement>();
            backWidth.preferredWidth = 180f;
            backWidth.flexibleWidth = 0f;
            Text label = kit.Label(row, title, 40, FontStyle.Bold, TextAnchor.MiddleLeft, HueTheme.Ink);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        void AddToggle(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = kit.Button(parent, label, HueTheme.Panel, HueTheme.Ink, 30, action);
            kit.Height(button, 96f);
        }

        void ToggleGlow()
        {
            bool current = save.settings.assistGlowSet ? save.settings.assistGlow : true;
            save.settings.assistGlowSet = true;
            save.settings.assistGlow = !current;
            Persist();
            ShowSettings();
        }

        string GlowLabel()
        {
            if (!save.settings.assistGlowSet) return "Ready glow    Auto";
            return "Ready glow    " + OnOff(save.settings.assistGlow);
        }

        bool AssistOn()
        {
            if (level == null) return false;
            if (save.settings.assistGlowSet) return save.settings.assistGlow;
            return level.id <= 15;
        }

        void Persist()
        {
            ads.Removed = SaveFile.Owns(save, "remove_ads");
            SaveFile.Write(save);
        }

        void PulseHaptic()
        {
            if (!save.settings.haptics) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        int Frontier()
        {
            int next = save.highestLevelCompleted + 1;
            if (next < 1) next = 1;
            if (catalog.levels.Length == 0) return 1;
            if (next > catalog.levels.Length) return catalog.levels.Length;
            return next;
        }

        string ProgressLabel()
        {
            return save.highestLevelCompleted + " / " + catalog.levels.Length + " cleared";
        }

        Level LevelById(int id)
        {
            for (int i = 0; i < catalog.levels.Length; i++)
            {
                if (catalog.levels[i].id == id) return catalog.levels[i];
            }
            return catalog.levels[0];
        }

        bool HasLevel(int id)
        {
            for (int i = 0; i < catalog.levels.Length; i++)
            {
                if (catalog.levels[i].id == id) return true;
            }
            return false;
        }

        Picture FindPicture(int order)
        {
            for (int i = 0; i < pictures.Length; i++)
            {
                if (pictures[i].order == order) return pictures[i];
            }
            return null;
        }

        int RevealedCount(Picture picture, bool all)
        {
            if (all || (picture.order >= 3 && SaveFile.Owns(save, "picture_pack_1"))) return picture.pieces.Length;
            int count = 0;
            for (int i = 1; i <= picture.pieces.Length; i++)
            {
                if (GalleryRules.IsPieceRevealed(picture.order, i, save.highestLevelCompleted)) count++;
            }
            return count;
        }

        bool PixelRevealed(Picture picture, int pixel, int highest)
        {
            for (int piece = 0; piece < picture.pieces.Length; piece++)
            {
                if (!GalleryRules.IsPieceRevealed(picture.order, piece + 1, highest)) continue;
                int[] cells = picture.pieces[piece];
                for (int i = 0; i < cells.Length; i++)
                {
                    if (cells[i] == pixel) return true;
                }
            }
            return false;
        }

        bool CoveredByBundle(string productId)
        {
            if (!SaveFile.Owns(save, "starter_bundle")) return false;
            return productId == "remove_ads" || productId == "picture_pack_1";
        }

        bool ShouldInterstitial(int levelId)
        {
            if (SaveFile.Owns(save, "remove_ads")) return false;
            return levelId >= 7 && (levelId - 7) % 3 == 0;
        }

        static string Tip(Level entry)
        {
            switch (entry.id)
            {
                case 1: return "Tap an arrow to send it off the board.";
                case 2: return "Arrows can't pass other arrows. Clear the blocker first.";
                case 3: return "Work back from the arrows that are free.";
                case 4: return "Gates change color after every clear. An arrow leaves only through a gate of its own color.";
                case 8:
                case 13:
                case 19: return string.IsNullOrEmpty(entry.note) ? "A new color joins the wheel." : entry.note;
                default: return "";
            }
        }

        static string StarText(int stars)
        {
            if (stars <= 0) return "";
            if (stars == 1) return "1 star";
            return stars + " stars";
        }

        static string OnOff(bool value)
        {
            return value ? "On" : "Off";
        }

        static string Pretty(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Picture";
            string spaced = name.Replace('_', ' ');
            return char.ToUpper(spaced[0]) + spaced.Substring(1);
        }

        static string RewardCopy(string placement)
        {
            switch (placement)
            {
                case "undo": return "Watch a short ad for one undo.";
                case "hint": return "Watch a short ad for one hint.";
                case "revive": return "Watch a short ad to keep playing with one heart.";
                default: return "Watch a short ad.";
            }
        }
    }
}
