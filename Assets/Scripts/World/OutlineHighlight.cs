using System.Collections.Generic;
using UnityEngine;

namespace TrainingDemo
{
    // Lightweight URP-compatible mesh shell outline for objects created from Unity primitives.
    public class OutlineHighlight : MonoBehaviour
    {
        private readonly List<GameObject> _shells = new List<GameObject>();
        private Material _material;

        private void Awake()
        {
            var shader = Shader.Find("TrainingDemo/OutlineShell");
            if (shader == null) return;
            _material = new Material(shader) { color = new Color(1f, .68f, .08f, 1f) };
            foreach (var source in GetComponentsInChildren<MeshFilter>())
            {
                if (source.sharedMesh == null) continue;
                var shell = new GameObject("Outline shell"); shell.transform.SetParent(source.transform, false); shell.transform.localScale = Vector3.one * 1.065f;
                shell.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                shell.AddComponent<MeshRenderer>().sharedMaterial = _material;
                shell.SetActive(false); _shells.Add(shell);
            }
        }

        public void Show(bool value)
        {
            foreach (var shell in _shells) if (shell != null) shell.SetActive(value);
        }

        private void OnDestroy() { if (_material != null) Destroy(_material); }
    }
}
