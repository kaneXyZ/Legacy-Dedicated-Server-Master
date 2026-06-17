using Legacy.DedicatedServer.Auth;
using UnityEditor;
using UnityEngine;

namespace Legacy.Editor
{
    [CustomEditor(typeof(MasterTicketAuthValidator))]
    public class MasterTicketAuthValidatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            MasterTicketAuthValidator validator = (MasterTicketAuthValidator)target;

            GUILayout.Space(10);

            if (validator.currentEnvironment == EnvironmentType.Production)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("ENTORNO ACTUAL: PRODUCCION", GUILayout.Height(40)))
                {
                    validator.currentEnvironment = EnvironmentType.Development;
                    EditorUtility.SetDirty(validator);
                }
            }
            else
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); // Rojo claro
                if (GUILayout.Button("ENTORNO ACTUAL: DESARROLLO", GUILayout.Height(40)))
                {
                    validator.currentEnvironment = EnvironmentType.Production;
                    EditorUtility.SetDirty(validator);
                }
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);

            DrawDefaultInspector();
        }
    }
}
