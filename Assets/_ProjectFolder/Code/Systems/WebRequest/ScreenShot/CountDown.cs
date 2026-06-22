using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class CountDown : MonoBehaviour
{
    [SerializeField] private int _countFrom = 3;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private UnityEvent _onCompleteCount;

    private readonly WaitForSeconds _second = new(1);

    private void Awake() => _text?.SetText(string.Empty);
    public void StartCountDown() => StartCoroutine(CountDownRoutine());

    private IEnumerator CountDownRoutine()
    {
        for (int i = _countFrom; i > 0; i--)
        {
            _text?.SetText(i.ToString());
            yield return _second;
        }

        _text?.SetText(string.Empty);
        _onCompleteCount.Invoke();
    }
}