using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TrainingDemo
{
    public class DemoSceneBuilder : MonoBehaviour
    {
        public bool isLobby;
        private Font _font;
        private Camera _camera;
        private Text _message;
        private Text _progress;
        private GameObject _resultPanel;
        private Transform _canvasRoot;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildRoom();
            BuildPlayer();
            BuildHUD();
            if (isLobby) BuildLobby(); else BuildTraining();
        }

        private void BuildRoom()
        {
            var floor = Primitive(PrimitiveType.Cube, "Floor", new Vector3(0, -0.15f, 3.5f), new Vector3(15, .3f, 18), new Color(.13f, .18f, .23f));
            var back = Primitive(PrimitiveType.Cube, "Back wall", new Vector3(0, 2.3f, 12.4f), new Vector3(15, 4.8f, .25f), new Color(.16f, .23f, .29f));
            Primitive(PrimitiveType.Cube, "Left wall", new Vector3(-7.4f, 2.3f, 3.5f), new Vector3(.25f, 4.8f, 18), new Color(.18f, .25f, .31f));
            Primitive(PrimitiveType.Cube, "Right wall", new Vector3(7.4f, 2.3f, 3.5f), new Vector3(.25f, 4.8f, 18), new Color(.18f, .25f, .31f));
            var light = new GameObject("Soft room light");
            var lamp = light.AddComponent<Light>(); lamp.type = LightType.Directional; lamp.intensity = 1.45f; light.transform.rotation = Quaternion.Euler(50, -25, 0);
            RenderSettings.ambientLight = new Color(.68f, .72f, .76f);
        }

        private void BuildPlayer()
        {
            var player = new GameObject("Player rig");
            player.transform.position = new Vector3(0, .05f, 0);
            var controller = player.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = .28f; controller.center = new Vector3(0, .9f, 0);
            player.AddComponent<VRPlayerController>();
            _camera = new GameObject("Head camera").AddComponent<Camera>();
            _camera.gameObject.AddComponent<AudioListener>();
            _camera.transform.SetParent(player.transform, false); _camera.transform.localPosition = new Vector3(0, 1.62f, 0); _camera.tag = "MainCamera";
            _camera.fieldOfView = 78; _camera.nearClipPlane = .04f;
            player.GetComponent<VRPlayerController>().SendMessage("SetCamera", _camera, SendMessageOptions.DontRequireReceiver);
            player.AddComponent<ScenarioRunner>();
        }

        private void BuildHUD()
        {
            var canvasObject = new GameObject("Training HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasRoot = canvas.transform;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900);
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null) eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (eventSystem.GetComponent<StandaloneInputModule>() == null) eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            var panel = UIBox(canvas.transform, "Info panel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(540, 176), new Color(.035f, .07f, .1f, .9f));
            _progress = UILabel(panel.transform, "Progress", new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -16), new Vector2(-36, 40), 23, new Color(.5f, .85f, 1f));
            _message = UILabel(panel.transform, "Instruction", new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -64), new Vector2(-36, 98), 22, Color.white);
            _resultPanel = UIBox(canvas.transform, "Result panel", new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(820, 600), new Color(.025f, .045f, .07f, .97f));
            _resultPanel.SetActive(false);
            var result = UILabel(_resultPanel.transform, "Results", new Vector2(0, 1), new Vector2(1, 1), new Vector2(32, -24), new Vector2(-64, 490), 23, Color.white);
            result.name = "Result text";
        }

        private void BuildLobby()
        {
            Title("Лобби", "Тренировка: проверка документов\nПройдите три группы действий. Следуйте подсказкам и обращайте внимание на подсветку.");
            var start = UIButton(_canvasRoot, "Start training button", "НАЧАТЬ ТРЕНИРОВКУ", new Vector2(.5f, .5f), new Vector2(0, -110), new Vector2(420, 72), new Color(.08f, .48f, .53f));
            start.onClick.AddListener(() => SceneManager.LoadScene("Training"));
            Station("Старт тренировки", "Начать", "lobby_start", ActionKind.Click, new Vector3(0, 1.2f, 3.2f), new Color(.08f, .48f, .53f), () => SceneManager.LoadScene("Training"));
            Label3D("Клавиатура: WASD + мышь, E — действие\nVR: стики + триггер контроллера", new Vector3(0, 2.5f, 3f), 0.045f, Color.white);
        }

        private void BuildTraining()
        {
            Title("Тренировка", "Выполните действия в указанном порядке. Ошибки фиксируются в результатах.");
            var runner = FindObjectOfType<ScenarioRunner>();
            var hud = gameObject.AddComponent<TrainingHudUpdater>(); hud.Init(_progress, _message, _resultPanel, _camera, _font, runner);

            var positions = new[] { new Vector3(-3.6f, 0, 6f), new Vector3(0, 0, 9f), new Vector3(3.6f, 0, 6f) };
            var titles = new[] { "01  Подготовка", "02  Проверка документов", "03  Подтверждение" };
            var instructions = new[] { "ПАПКА", "ПАСПОРТ", "КАРТА" };
            var buttons = new[] { "НАЧАТЬ", "ПОДТВЕРДИТЬ", "ЗАВЕРШИТЬ" };
            var colors = new[] { new Color(.18f, .48f, .55f), new Color(.34f, .39f, .62f), new Color(.26f, .53f, .38f) };
            for (var i = 0; i < positions.Length; i++)
            {
                var p = positions[i];
                var station = Primitive(PrimitiveType.Cube, titles[i], p + new Vector3(0, .55f, .9f), new Vector3(2.3f, 1.1f, .9f), new Color(.23f, .29f, .33f));
                station.transform.rotation = Quaternion.identity;
                Label3D(titles[i], p + new Vector3(0, 2.4f, .65f), .055f, Color.white);
                var zone = Primitive(PrimitiveType.Cylinder, "Подсвеченная зона " + (i + 1), p + new Vector3(0, .04f, -.25f), new Vector3(2.7f, .04f, 1.3f), new Color(.1f, .72f, .77f));
                var zoneTarget = zone.AddComponent<InteractableTarget>(); zoneTarget.targetId = "zone_" + (i + 1); zoneTarget.action = ActionKind.Approach;
                var trigger = zone.GetComponent<Collider>(); trigger.isTrigger = true; zone.GetComponent<Renderer>().enabled = true;
                zone.GetComponent<Renderer>().material.color = new Color(.06f, .38f, .42f, .5f);
                var item = Primitive(PrimitiveType.Cube, instructions[i], p + new Vector3(-.45f, 1.23f, .6f), new Vector3(.6f, .15f, .42f), colors[i]);
                item.transform.rotation = Quaternion.Euler(0, -12, 0);
                item.AddComponent<InteractableTarget>().targetId = "folder_" + (i + 1);
                item.GetComponent<InteractableTarget>().action = ActionKind.Grab;
                item.GetComponent<InteractableTarget>().hint = "Возьмите предмет";
                Station("Кнопка " + buttons[i], buttons[i], "button_" + (i + 1), ActionKind.Click, p + new Vector3(.48f, 1.23f, .55f), colors[i], null);
            }
            Station("В лобби", "В ЛОББИ", "return_lobby", ActionKind.Click, new Vector3(0, 1.05f, 11.3f), new Color(.38f, .24f, .25f), () => SceneManager.LoadScene("Lobby"));
        }

        private void Title(string title, string subtitle)
        {
            Label3D(title, new Vector3(0, 3.25f, 4.7f), .12f, new Color(.57f, .89f, 1f));
            Label3D(subtitle, new Vector3(0, 2.75f, 4.7f), .045f, Color.white, TextAnchor.UpperCenter, 6f);
        }

        private void Station(string name, string label, string id, ActionKind action, Vector3 position, Color color, System.Action onClick)
        {
            var button = Primitive(PrimitiveType.Cube, name, position, new Vector3(1.05f, .2f, .35f), color);
            var target = button.AddComponent<InteractableTarget>(); target.targetId = id; target.action = action; target.hint = label;
            Label3D(label, position + Vector3.up * .28f, .035f, Color.white);
            if (onClick != null)
            {
                target.bypassScenario = true;
                var callback = button.AddComponent<SceneButtonCallback>(); callback.Set(onClick);
            }
        }

        private GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.position = position; obj.transform.localScale = scale;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader); material.color = color; obj.GetComponent<Renderer>().material = material;
            return obj;
        }

        private TextMesh Label3D(string value, Vector3 position, float size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, float width = 4f)
        {
            var obj = new GameObject("Label: " + value, typeof(TextMesh));
            // Unity's built-in TextMesh faces local -Z, so turn it toward the camera and keep its top upright.
            obj.transform.position = position; obj.transform.rotation = Quaternion.LookRotation(_camera.transform.position - position) * Quaternion.Euler(0f, 180f, 0f);
            var text = obj.GetComponent<TextMesh>(); text.font = _font; text.text = value; text.characterSize = size; text.fontSize = 64; text.color = color;
            text.anchor = alignment; text.alignment = TextAlignment.Center; text.richText = false; text.tabSize = 4;
            return text;
        }

        private GameObject UIBox(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(anchorMin.x, anchorMax.y); rect.anchoredPosition = offset; rect.sizeDelta = size;
            obj.GetComponent<Image>().color = color; return obj;
        }

        private Button UIButton(Transform parent, string name, string label, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var obj = UIBox(parent, name, anchor, anchor, offset, size, color);
            var button = obj.AddComponent<Button>();
            var colors = button.colors; colors.normalColor = color; colors.highlightedColor = new Color(color.r + .12f, color.g + .12f, color.b + .12f); colors.pressedColor = color * .8f; button.colors = colors;
            var text = UILabel(obj.transform, "Label", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 25, Color.white);
            text.alignment = TextAnchor.MiddleCenter; text.text = label;
            return button;
        }

        private Text UILabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, Vector2 size, int fontSize, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(anchorMin.x, anchorMax.y); rect.anchoredPosition = offset; rect.sizeDelta = size;
            var text = obj.GetComponent<Text>(); text.font = _font; text.fontSize = fontSize; text.color = color; text.alignment = TextAnchor.UpperLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }

    public class SceneButtonCallback : MonoBehaviour
    {
        private System.Action _callback;
        public void Set(System.Action callback) => _callback = callback;
        private void OnMouseDown() => _callback?.Invoke();
        public void Invoke() => _callback?.Invoke();
    }

    public class TrainingHudUpdater : MonoBehaviour
    {
        private Text _progress, _message, _result;
        private GameObject _panel;
        private ScenarioRunner _runner;
        private Camera _camera;
        private Font _font;

        public void Init(Text progress, Text message, GameObject panel, Camera camera, Font font, ScenarioRunner runner)
        { _progress = progress; _message = message; _panel = panel; _camera = camera; _font = font; _runner = runner; }

        private void Update()
        {
            if (_runner == null) return;
            if (_runner.IsComplete)
            {
                if (Input.GetKeyDown(KeyCode.R)) { _runner.ResetRun(); _panel.SetActive(false); _result = null; }
                return;
            }
            _progress.text = _runner.CurrentGroup.title + "     Шаг " + (_runner.StepIndex + 1) + " из " + _runner.CurrentGroup.steps.Length;
            _message.text = _runner.LastMessage + "\n\nПодсвеченные предметы доступны для взаимодействия.";
            if (_runner.IsComplete) ShowResults();
        }

        private void LateUpdate()
        {
            if (_runner != null && _runner.IsComplete && !_panel.activeSelf) ShowResults();
        }

        private void ShowResults()
        {
            _panel.SetActive(true);
            if (_result != null) return;
            var go = new GameObject("Result text", typeof(RectTransform), typeof(Text)); go.transform.SetParent(_panel.transform, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(30, -24); rect.sizeDelta = new Vector2(-60, 490);
            _result = go.GetComponent<Text>(); _result.font = _font; _result.fontSize = 24; _result.color = Color.white; _result.alignment = TextAnchor.UpperLeft;
            var text = "ТРЕНИРОВКА ЗАВЕРШЕНА\nРезультаты по шагам\n\n";
            for (var g = 0; g < _runner.Definition.groups.Length; g++)
            {
                var group = _runner.Definition.groups[g]; text += group.title + "\n";
                for (var s = 0; s < group.steps.Length; s++)
                {
                    var status = _runner.GetStatus(g, s);
                    var mark = status == StepStatus.Success ? "✓" : status == StepStatus.Failed ? "✕ Ошибка" : "— Пропущен";
                    text += "  " + (s + 1) + ". " + group.steps[s].title + "   " + mark + "\n";
                }
                text += "\n";
            }
            text += "Выберите действие ниже.";
            _result.text = text;
            var restart = CreateButton("Повторить", new Vector2(.28f, 0), new Vector2(0, 26), new Vector2(280, 58), new Color(.08f, .48f, .53f));
            restart.onClick.AddListener(() => { _runner.ResetRun(); _panel.SetActive(false); _result = null; });
            var lobby = CreateButton("В лобби", new Vector2(.72f, 0), new Vector2(0, 26), new Vector2(280, 58), new Color(.38f, .24f, .25f));
            lobby.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby"));
        }

        private Button CreateButton(string label, Vector2 anchor, Vector2 offset, Vector2 size, Color color)
        {
            var obj = new GameObject(label + " button", typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(_panel.transform, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = offset; rect.sizeDelta = size;
            obj.GetComponent<Image>().color = color;
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text)); textObject.transform.SetParent(obj.transform, false);
            var labelRect = textObject.GetComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            var labelText = textObject.GetComponent<Text>(); labelText.font = _font; labelText.fontSize = 22; labelText.color = Color.white; labelText.alignment = TextAnchor.MiddleCenter; labelText.text = label;
            return obj.GetComponent<Button>();
        }
    }
}
