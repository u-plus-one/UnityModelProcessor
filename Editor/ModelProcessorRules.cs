namespace ModelProcessor.Editor
{
    [System.Serializable]
    public class ModelProcessorRules
    {
        [System.Serializable]
        public class Rule
        {
            public enum ConditionType
            {
                Always = 0,
                NameStartsWith = 1,
                NameEndsWith = 2,
                NameContains = 3,
            }

            public enum ActionType
            {
                None = 0,
                SetGameObjectInactive = 1,
                DestroyGameObject = 2,
                RemoveRenderer = 3,
                RemoveCollider = 4,
                AddHelperComponent = 99
            }

            public ConditionType condition = ConditionType.Always;
            public string conditionString = "";
            public ActionType action = ActionType.None;
        }
    }
}