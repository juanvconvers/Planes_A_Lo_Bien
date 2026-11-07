using System.Collections;
using System.Threading.Tasks;
using Tesela.Core;
using Tesela.Monetization;
using Tesela.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tesela.UI
{
    /// <summary>
    /// Raíz de composición. Se crea sola al arrancar cualquier escena (Boot.unity está vacía), registra servicios,
    /// genera el arte en un hilo aparte y abre el flujo: primera vez → partida-tutorial; luego → Inicio.
    /// Sin red ni IDs todo funciona con las implementaciones falsas y los valores por defecto locales.
    /// </summary>
    public sealed class TeselaApp : MonoBehaviour
    {
        private static TeselaApp _instance;
        private AppContext _ctx;
        private Navigator _nav;
        private RectTransform _screens;
        private double _sessionStart;
        private bool _reminderShown;

        public static AppContext Context => _instance != null ? _instance._ctx : null;
        public static Navigator Navigator => _instance != null ? _instance._nav : null;
        public static bool Ready { get; private set; }

        /// <summary>Carpeta de guardado alternativa (tests PlayMode: no tocan el progreso real del editor).</summary>
        public static string SaveDirectoryOverride;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("Tesela");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<TeselaApp>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Input.multiTouchEnabled = false;
        }

        private IEnumerator Start()
        {
            var art = Task.Run(ArtLibrary.RenderProcedural);
            BuildCanvas(out var loading);
            _ctx = CreateContext();
            ApplySettings(_ctx);
            while (!art.IsCompleted) yield return null;
            Ui.Art = ArtLibrary.Build(Theme.Instance, art.Result);
            Ui.Audio = _ctx.Audio;
            Destroy(loading);
            BuildBackground();
            _nav = new Navigator(_ctx, _screens);
            StartServices();
            Ready = true;
            OpenFirstScreen();
        }

        private AppContext CreateContext()
        {
            var ids = ExternalIds.Instance;
            var store = new FileStore(SaveDirectoryOverride);
            var saves = new SaveSystem(store);
            var data = GameData.Load();
            var profile = saves.LoadProfile();
            var ctx = new AppContext
            {
                Data = data,
                Saves = saves,
                Settings = saves.LoadSettings(),
                AdState = saves.LoadAds(),
                Clock = new Tesela.Core.SystemClock(),
                PreviousActiveDay = profile.LastActiveDay
            };
            ctx.Meta = new Tesela.Meta.MetaService(data.Economy, profile);

            string defaults = GameData.Text("RemoteDefaults");
#if TESELA_FIREBASE
            var analytics = new FirebaseAnalyticsService();
            analytics.Initialize(null);
            ctx.Analytics = analytics;
            ctx.Remote = new FirebaseRemoteConfigService(defaults);
#else
            ctx.Analytics = new NullAnalytics();
            ctx.Remote = new LocalRemoteConfig(defaults);
#endif
            ctx.AdConfig = AdConfig.FromJson(ctx.Remote.Get("ads"));
            ctx.Offers = OfferEngine.FromJson(ctx.Remote.Get("offers"));

            IAdService ads = new FakeAdService();
#if TESELA_LEVELPLAY
            if (ids.AdsConfigured) ads = new LevelPlayAdService(ids);
#endif
            ctx.Ads = new SafeAdService(ads, () => ctx.AdConfig.SdkTimeoutSeconds);

            ctx.Store = new FakeStoreService();
#if TESELA_IAP
            ctx.Store = new UnityIapStoreService();
#endif
            ctx.Consent = new FakeConsentService();
#if TESELA_UMP
            ctx.Consent = new UmpConsentService();
#endif
            ctx.Games = new OfflineGames();
#if TESELA_PGS
            if (ids.PlayGamesConfigured) ctx.Games = new PlayGamesService(ids);
#endif
            ctx.Notifications = new AndroidLocalNotifications();
            ctx.Review = new PlayReviewService(this);

            var audio = gameObject.AddComponent<AudioService>();
            audio.Initialize(ctx.Settings);
            ctx.Audio = audio;

            ServiceLocator.Reset();
            ServiceLocator.Register(ctx.Analytics);
            ServiceLocator.Register(ctx.Remote);
            ServiceLocator.Register<IAdService>(ctx.Ads);
            ServiceLocator.Register(ctx.Store);
            ServiceLocator.Register(ctx.Games);
            ServiceLocator.Register<IClock>(ctx.Clock);
            return ctx;
        }

        private void StartServices()
        {
            var ctx = _ctx;
            ctx.Meta.StartSession(ctx.UtcNow);
            ctx.AdState.StartSession(Time.realtimeSinceStartup);
            ctx.Offers.StartSession();
            _sessionStart = Time.realtimeSinceStartup;
            ctx.Store.DeferredPurchase += id => Shop.Deliver(ctx, id);
            ctx.Store.Initialize(ctx.Data.Products.Products, ok =>
            {
                if (ok) Shop.Restore(ctx, null);
            });
            ctx.Remote.Fetch(ok =>
            {
                if (!ok) return;
                ctx.AdConfig = AdConfig.FromJson(ctx.Remote.Get("ads"));
                var offers = OfferEngine.FromJson(ctx.Remote.Get("offers"));
                offers.StartSession();
                ctx.Offers = offers;
                ctx.Analytics.SetUserProperty("ab_variant", ctx.Remote.Variant);
            });
            // Los anuncios solo se inicializan después de pedir el consentimiento.
            if (ctx.Profile.ConsentAsked) ctx.Ads.Initialize(ctx.Consent.PersonalizedAdsAllowed);
            ctx.Games.SignIn(null);
            ctx.Review.CheckForUpdate();
            ctx.Analytics.SetUserProperty("segment", Segmentation.Key(ctx.Segment));
            ctx.SaveAll();
        }

        private void OpenFirstScreen()
        {
            if (!_ctx.Profile.TutorialDone)
            {
                var game = new GameController(_ctx);
                game.StartClassic(tutorial: true);
                _nav.Reset(new HomeScreen());
                _nav.Push(new GameScreen(game));
                return;
            }
            _nav.Reset(new HomeScreen());
            if (_ctx.Profile.ConsentAsked)
            {
                var resume = new GameController(_ctx);
                if (resume.TryResume()) _nav.Push(new GameScreen(resume));
                else if (_ctx.Segment == Segment.AtRisk)
                {
                    var rule = _ctx.Offers.Pick(OfferMoment.ReturnAfterAbsence, Segment.AtRisk, _ctx.Profile, 0, _ctx.Today, null);
                    if (rule != null) { _ctx.Offers.MarkShown(); _nav.Push(new OfferDialog(rule)); }
                }
            }
        }

        private void BuildCanvas(out GameObject loading)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            float aspect = Screen.height / (float)Mathf.Max(1, Screen.width);
            // Teléfonos altos: ajustar por ancho. Tabletas y pantallas anchas: por alto (todo cabe en vertical).
            scaler.matchWidthOrHeight = aspect >= 1920f / 1080f ? 0f : 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = Ui.Rect(canvasGo.transform, "background").Stretch();
            bg.gameObject.AddComponent<Image>().color = Palette.Sand.ToColor();
            loading = bg.gameObject;

            var safe = Ui.Rect(canvasGo.transform, "safe").Stretch();
            safe.gameObject.AddComponent<SafeArea>();
            _screens = safe;

            // Tamaño lógico calculado con la misma fórmula del CanvasScaler (el lienzo aún no se ha redimensionado en este frame).
            float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            float scale = scaler.matchWidthOrHeight < 0.5f ? sw / 1080f : sh / 1920f;
            Rect sa = Screen.safeArea;
            Layout.Width = sa.width / scale;
            Layout.Height = sa.height / scale;
            if (Layout.Width < 100 || Layout.Height < 100) { Layout.Width = 1080; Layout.Height = 1920; }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.transform.SetParent(transform, false);
#if TESELA_INPUTSYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        private void BuildBackground()
        {
            var canvas = _screens.parent;
            var bg = Ui.Rect(canvas, "background").Stretch();
            bg.SetAsFirstSibling();
            bg.gameObject.AddComponent<Image>().color = Palette.Sand.ToColor();
            if (Theme.Instance.useTextures && Theme.Instance.background != null)
            {
                var raw = Ui.Rect(bg, "art").Stretch().gameObject.AddComponent<RawImage>();
                raw.texture = Theme.Instance.background;
            }
            Ui.Grain(bg);
        }

        public static void ApplySettings(AppContext ctx)
        {
            var s = ctx.Settings;
            Haptics.Enabled = s.Vibration;
            Motion.ReduceMotion = s.ReduceMotion;
            UiText.GlobalScale = s.TextScale;
            ctx.Audio?.ApplySettings(s);
            string lang = string.IsNullOrEmpty(s.Language) ? Loc.FromSystem(SystemLanguageIso(Application.systemLanguage)) : s.Language;
            foreach (string l in Loc.Languages)
                if (Loc.Table(l).Count == 0) Loc.Load(l, GameData.Text("Loc/" + l));
            Loc.SetLanguage(lang);
        }

        private static string SystemLanguageIso(SystemLanguage l)
        {
            switch (l)
            {
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Portuguese: return "pt";
                default: return "en";
            }
        }

        /// <summary>Tras borrar datos: reconstruye todo desde cero.</summary>
        public static void Restart()
        {
            if (_instance == null) return;
            Ready = false;
            Destroy(_instance.gameObject);
            _instance = null;
            Boot();
        }

        private void Update()
        {
            if (!Ready) return;
            _ctx.Ads.Tick();
            if (BackPressed()) _nav.Back();
            if (_ctx.Settings.SessionReminder && !_reminderShown)
            {
                int minutes = _ctx.Remote.Get("features")["sessionReminderMinutes"].AsInt(45);
                if (Time.realtimeSinceStartup - _sessionStart > minutes * 60 && !(_nav.Top is GameScreen))
                {
                    _reminderShown = true;
                    _nav.Push(new MessageDialog("reminder.title", Loc.T("reminder.body", minutes)));
                }
            }
        }

        private static bool BackPressed()
        {
#if TESELA_INPUTSYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused || _ctx == null) return;
            _ctx.SaveAll();
            ScheduleNotification();
        }

        private void OnApplicationQuit()
        {
            if (_ctx == null) return;
            _ctx.SaveAll();
            ScheduleNotification();
        }

        /// <summary>Máximo una notificación al día, solo con permiso, a las 19:00 hora local.</summary>
        private void ScheduleNotification()
        {
            string key = _ctx.Meta.PlanNotification(_ctx.Today);
            if (key == null) return;
            var when = System.DateTime.Now.Date.AddDays(1).AddHours(19);
            _ctx.Notifications.ScheduleAt(when, Loc.T("app.name"), Loc.T(key));
            _ctx.Meta.MarkNotificationPlanned(_ctx.Today + 1);
            _ctx.Saves.SaveProfile(_ctx.Profile);
        }
    }
}
