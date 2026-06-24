using System.Text.RegularExpressions;

namespace UnityEngine.UI
{
    [CreateAssetMenu(fileName = "keyboard", menuName = "system/keyboard")]
    public class ScriptableKeyboard : ScriptableObject
    {
        [SerializeField, TextArea] private string chars;

        public void Setup(Transform parent, out IKeyText[] keys)
        {
            //remove white spaces
            string values = Regex.Replace(chars, @"\r\n?|\n", string.Empty);

            keys = parent.GetComponentsInChildren<IKeyText>();
            for (int i = 0; i < keys.Length; i++) keys[i].Text = values[i].ToString();
        }
    }
}