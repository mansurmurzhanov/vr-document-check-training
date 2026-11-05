using System;
using UnityEngine;

namespace TrainingDemo
{
    public enum ActionKind { Approach, Grab, Click }

    [Serializable]
    public class ScenarioStep
    {
        public string title;
        [TextArea] public string instruction;
        public ActionKind expectedAction;
        public string targetId;
    }

    [Serializable]
    public class ScenarioGroup
    {
        public string title;
        public ScenarioStep[] steps;
    }

    [CreateAssetMenu(menuName = "Training/Scenario Definition")]
    public class ScenarioDefinition : ScriptableObject
    {
        public ScenarioGroup[] groups;

        public static ScenarioDefinition CreateDemo()
        {
            var definition = CreateInstance<ScenarioDefinition>();
            definition.groups = new[]
            {
                Group("Подготовка", new[]
                {
                    Step("Подойдите к стойке", "Войдите в подсвеченную зону у стойки.", ActionKind.Approach, "zone_1"),
                    Step("Возьмите папку", "Наведитесь на папку и нажмите взаимодействие.", ActionKind.Grab, "folder_1"),
                    Step("Начните проверку", "Нажмите кнопку «Начать» на панели.", ActionKind.Click, "button_1")
                }),
                Group("Проверка документов", new[]
                {
                    Step("Подойдите к документу", "Перейдите в выделенную зону проверки.", ActionKind.Approach, "zone_2"),
                    Step("Осмотрите паспорт", "Возьмите паспорт со стола.", ActionKind.Grab, "folder_2"),
                    Step("Подтвердите данные", "Нажмите кнопку подтверждения.", ActionKind.Click, "button_2")
                }),
                Group("Подтверждение", new[]
                {
                    Step("Подойдите к терминалу", "Войдите в зону терминала.", ActionKind.Approach, "zone_3"),
                    Step("Возьмите карточку", "Возьмите карточку со стойки.", ActionKind.Grab, "folder_3"),
                    Step("Завершите сценарий", "Нажмите кнопку завершения.", ActionKind.Click, "button_3")
                })
            };
            return definition;
        }

        private static ScenarioGroup Group(string title, ScenarioStep[] steps) => new ScenarioGroup { title = title, steps = steps };
        private static ScenarioStep Step(string title, string instruction, ActionKind action, string target) =>
            new ScenarioStep { title = title, instruction = instruction, expectedAction = action, targetId = target };
    }
}
