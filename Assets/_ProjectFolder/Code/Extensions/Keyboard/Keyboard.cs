using System;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [Flags] public enum ContentType
    {
        None = 0,
        Standard = 1,
        IntegerNumber = 2,
        Name = 4,
        EmailAddress = 8
    }

    public class Keyboard : MonoBehaviour
    {
        [SerializeField] private ContentType _type;
        [SerializeField] private GameObject[] _boards;
        private int _current;

        public ContentType Type => _type;

        private void OnEnable() => SetEnabled(0);

        public void SetEnabled(int index)
        {
            _current = index;
            for (int i = 0; i < _boards.Length; i++) _boards[i]?.SetActive(i == index);
        }
        public void Swipe(BaseEventData data)
        {
            _current++;
            if (_current >= _boards.Length) _current = 0;
            SetEnabled(_current);
        }
    }
}