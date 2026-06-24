using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public interface IKey
    {
        public void OnSelect(BaseEventData data);
    }
    public interface IKeyText : IKey
    {
        public string Text { get; set; }
        public void Mayus(bool value);
    }
}