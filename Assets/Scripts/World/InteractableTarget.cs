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
        private OutlineHighlight _outline;

        private void Awake()
        {
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
            if (bypassScenario) GetComponent<SceneButtonCallback>()?.Invoke();
            else ScenarioRunner.Instance?.Submit(action, targetId);
            if (action == ActionKind.Grab) transform.localPosition += Vector3.up * 0.14f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (action == ActionKind.Approach && other.GetComponentInParent<VRPlayerController>() != null)
                ScenarioRunner.Instance?.Submit(action, targetId);
        }
    }
}
