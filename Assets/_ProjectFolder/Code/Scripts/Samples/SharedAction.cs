using UnityEngine;
using UnityEngine.Events;

public class SharedAction : MonoBehaviour
{
    [SerializeField] private UnityEvent _onActionTriggered;

    public void TriggerAction() => _onActionTriggered?.Invoke();
}