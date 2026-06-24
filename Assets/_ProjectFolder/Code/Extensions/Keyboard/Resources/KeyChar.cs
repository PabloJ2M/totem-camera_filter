using UnityEngine.EventSystems;
using TMPro;

namespace UnityEngine.UI
{
    public class KeyChar : MonoBehaviour, IKeyText
    {
        [SerializeField] private TextMeshProUGUI _text;

        public string Text { get => _text.text; set => _text?.SetText(value); }
        public void Mayus(bool value) => Text = value ? Text.ToUpper() : Text.ToLower();

        public void OnSelect(BaseEventData data) => KeyboardManager.Instance.AddChar(Text);
    }
}