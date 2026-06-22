using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SliderButton : MonoBehaviour
{
    [SerializeField] private float _value;
    [SerializeField] private Vector2 _limits;
    [SerializeField] private float _step = 0.1f;

    [SerializeReference] private Button _addButton, _substractbutton;
    [SerializeField] private UnityEvent<float> _onValueChanged;

    private void Awake()
    {
        _addButton.onClick.AddListener(AddValue);
        _substractbutton.onClick.AddListener(SubtractValue);
    }

    private void AddValue() => SetValue(_value + _step);
    private void SubtractValue() => SetValue(_value - _step);

    private void SetValue(float value)
    {
        _value = Mathf.Clamp(value, _limits.x, _limits.y);
        _onValueChanged.Invoke(_value);
    }
}