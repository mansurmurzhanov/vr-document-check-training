using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainingDemo
{
    public enum StepStatus { Pending, Success, Failed, Skipped }

    public class ScenarioRunner : MonoBehaviour
    {
        public static ScenarioRunner Instance { get; private set; }
        public ScenarioDefinition Definition { get; private set; }
        public int GroupIndex { get; private set; }
        public int StepIndex { get; private set; }
        public bool IsComplete { get; private set; }
        public string LastMessage { get; private set; }
        public event Action Changed;
        private readonly List<StepStatus[]> _statuses = new List<StepStatus[]>();
        private AudioSource _audio;
        private AudioClip _okTone;
        private AudioClip _errorTone;

        public StepStatus GetStatus(int group, int step) => _statuses[group][step];
        public ScenarioGroup CurrentGroup => Definition.groups[GroupIndex];
        public ScenarioStep CurrentStep => CurrentGroup.steps[StepIndex];

        private void Awake()
        {
            Instance = this;
            Definition = ScenarioDefinition.CreateDemo();
            foreach (var group in Definition.groups) _statuses.Add(new StepStatus[group.steps.Length]);
            _audio = gameObject.AddComponent<AudioSource>();
            _okTone = Tone(880f, 0.12f);
            _errorTone = Tone(220f, 0.24f);
            LastMessage = CurrentStep.instruction;
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Definition != null) Destroy(Definition);
            if (_okTone != null) Destroy(_okTone);
            if (_errorTone != null) Destroy(_errorTone);
        }

        public bool Submit(ActionKind action, string targetId)
        {
            if (IsComplete) return false;
            var expected = CurrentStep;
            if (expected.expectedAction == action && expected.targetId == targetId)
            {
                _statuses[GroupIndex][StepIndex] = StepStatus.Success;
                Play(_okTone);
                LastMessage = "Верно: " + expected.title;
                Advance();
                return true;
            }

            Play(_errorTone);
            LastMessage = "Ошибка: " + (targetId == expected.targetId ? "действие выполнено не в тот момент." : "выбран неверный объект или зона.");
            if (IsLaterStep(action, targetId))
            {
                // Record the action that broke the order as a failure; later steps were never attempted.
                _statuses[GroupIndex][StepIndex] = StepStatus.Failed;
                for (var i = StepIndex + 1; i < CurrentGroup.steps.Length; i++)
                    if (_statuses[GroupIndex][i] == StepStatus.Pending) _statuses[GroupIndex][i] = StepStatus.Skipped;
                LastMessage = "Нарушен порядок действий. Группа закрыта, незавершённые шаги пропущены.";
                AdvanceGroup();
            }
            else
            {
                _statuses[GroupIndex][StepIndex] = StepStatus.Failed;
                Advance();
            }
            return false;
        }

        public void ResetRun()
        {
            for (var g = 0; g < _statuses.Count; g++)
                for (var s = 0; s < _statuses[g].Length; s++) _statuses[g][s] = StepStatus.Pending;
            foreach (var target in FindObjectsOfType<InteractableTarget>()) target.ResetCarry();
            GroupIndex = 0;
            StepIndex = 0;
            IsComplete = false;
            LastMessage = CurrentStep.instruction;
            Changed?.Invoke();
        }

        private bool IsLaterStep(ActionKind action, string targetId)
        {
            for (var g = GroupIndex; g < Definition.groups.Length; g++)
            {
                var first = g == GroupIndex ? StepIndex + 1 : 0;
                for (var s = first; s < Definition.groups[g].steps.Length; s++)
                    if (Definition.groups[g].steps[s].expectedAction == action && Definition.groups[g].steps[s].targetId == targetId) return true;
            }
            return false;
        }

        private void Advance()
        {
            StepIndex++;
            if (StepIndex >= CurrentGroup.steps.Length) AdvanceGroup();
            else { LastMessage = CurrentStep.instruction; Changed?.Invoke(); }
        }

        private void AdvanceGroup()
        {
            GroupIndex++;
            StepIndex = 0;
            if (GroupIndex >= Definition.groups.Length)
            {
                IsComplete = true;
                LastMessage = "Сценарий завершён. Посмотрите результаты.";
            }
            else LastMessage = "Группа: " + CurrentGroup.title + ". " + CurrentStep.instruction;
            Changed?.Invoke();
        }

        private void Play(AudioClip clip) { if (clip != null) _audio.PlayOneShot(clip); }
        private static AudioClip Tone(float frequency, float duration)
        {
            const int rate = 22050;
            var count = Mathf.CeilToInt(rate * duration);
            var samples = new float[count];
            for (var i = 0; i < count; i++) samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / rate) * (1f - (float)i / count) * 0.25f;
            var clip = AudioClip.Create("Training signal", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
