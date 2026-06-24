using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace UnityEngine.UI
{
    public class KeyboardManager : SimpleSingleton<KeyboardManager>
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Keyboard[] _keyboards;
        [SerializeField] private UnityEvent<string> _onPreview;
        private TMP_InputField _selected;

        public bool mayus { private get; set; }
        public IKeyText[] keys;

        #region Event Listener
        private void OnEnable() => KeyboardListener.onSelectField += FieldSelected;
        private void OnDisable() => KeyboardListener.onSelectField -= FieldSelected;
        private void FieldSelected(TMP_InputField field)
        {
            _selected = field;
            _onPreview.Invoke(_selected.text);
            ChangeVisibility(true);

            foreach (Keyboard keyboard in _keyboards)
            {
                ContentType type = (ContentType)_selected.contentType.ConvertEnum(ContentType.None);
                keyboard.gameObject.SetActive(keyboard.Type.HasFlag(type));
                keyboard.SetEnabled(0);
            }
        }

        public void ChangeVisibility(bool value)
        {
            _canvasGroup.alpha = value ? 1 : 0;
            _canvasGroup.blocksRaycasts = _canvasGroup.interactable = value;
        }
        #endregion

        #region Special Characters
        public void Space(BaseEventData data) => AddChar(" ");
        public void Mayus(BaseEventData data) { mayus = !mayus; foreach (var key in keys) key.Mayus(mayus); }
        #endregion

        #region Simple Interactions
        public void AddChar(string text)
        {
            _selected?.SetText($"{_selected.text}{text}");
            _onPreview.Invoke(_selected.text);
        }
        public void RemoveChar(BaseEventData data)
        {
            if (!_selected) return;
            int lenght = _selected.text.Length;

            if (lenght == 0) return;
            _selected?.SetTextWithoutNotify(_selected.text.Remove(lenght - 1, 1));
            _onPreview.Invoke(_selected.text);
        }
        #endregion

    }
}