using UnityEngine;

namespace TrainingDemo
{
    public class InteractableTarget : MonoBehaviour
    {
        public string targetId;
        public ActionKind action;
        public bool bypassScenario;
        [TextArea] public string hint;
        [SerializeField] private Renderer[] renderers;
        private Color[] _baseColors;
        private bool _highlighted;
        private bool _carried;
        private Transform _originalParent;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale;
        private OutlineHighlight _outline;

        private void Awake()
        {
            _originalParent = transform.parent;
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            _originalScale = transform.localScale;
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();
            _outline = GetComponent<OutlineHighlight>();
            if (_outline == null) _outline = gameObject.AddComponent<OutlineHighlight>();
            _baseColors = new Color[renderers.Length];
            for (var i = 0; i < renderers.Length; i++) _baseColors[i] = renderers[i].material.color;
        }

        public void SetHighlighted(bool value)
        {
            if (_highlighted == value) return;
            _highlighted = value;
            _outline?.Show(value);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var material = renderers[i].material;
                material.color = value ? new Color(1f, 0.78f, 0.16f) : _baseColors[i];
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", value ? new Color(1f, 0.45f, 0.04f) : Color.black);
            }
        }

        public void Interact()
        {
            if (action == ActionKind.Approach) return;
            if (bypassScenario)
            {
                GetComponent<SceneButtonCallback>()?.Invoke();
                return;
            }

            var accepted = ScenarioRunner.Instance != null && ScenarioRunner.Instance.Submit(action, targetId);
            if (accepted && action == ActionKind.Grab) CarryDocument();
        }

        private void CarryDocument()
        {
            var camera = Camera.main;
            if (camera == null) return;

            // Keep the picked-up document visible at the player's lower-right, like an inspection hold.
            transform.SetParent(camera.transform, false);
            transform.localPosition = new Vector3(.34f, -.25f, .72f);
            transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
            transform.localScale = _originalScale * .62f;
            _carried = true;
        }

        public void ResetCarry()
        {
            if (!_carried) return;
            transform.SetParent(_originalParent, true);
            transform.SetPositionAndRotation(_originalPosition, _originalRotation);
            transform.localScale = _originalScale;
            _carried = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (action == ActionKind.Approach && other.GetComponentInParent<VRPlayerController>() != null)
                ScenarioRunner.Instance?.Submit(action, targetId);
        }
    }
}
