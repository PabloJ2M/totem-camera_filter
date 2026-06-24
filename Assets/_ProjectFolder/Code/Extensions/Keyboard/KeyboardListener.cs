using System;
using System.Collections;
using UnityEngine.Events;
using TMPro;

namespace UnityEngine.EventSystems
{
    [RequireComponent(typeof(EventSystem))]
    public class KeyboardListener : MonoBehaviour
    {
        public static event Action<TMP_InputField> onSelectField;
        [SerializeField] private UnityEvent _onSelect;

        private EventSystem _system;
        private GameObject _current;
        private WaitUntil _fieldSelected;

        private void Awake() 
        {
            _system = GetComponent<EventSystem>();
            _fieldSelected = new WaitUntil(() => _current != _system.currentSelectedGameObject);
        }
        private IEnumerator Start()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer) yield break;

            yield return _fieldSelected;
            _current = _system.currentSelectedGameObject;

            if (_current)
            {
                if (_current.TryGetComponent(out TMP_InputField t))
                {
                    onSelectField?.Invoke(t);
                    _onSelect.Invoke();
                }
            }

            StartCoroutine(Start());
        }
    }
}