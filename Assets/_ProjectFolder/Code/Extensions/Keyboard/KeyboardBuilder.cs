namespace UnityEngine.UI
{
    [ExecuteAlways]
    public class KeyboardBuilder : MonoBehaviour
    {
        [SerializeField] private ScriptableKeyboard _config;

        public void OnEnable()
        {
            KeyboardManager manager = GetComponentInParent<KeyboardManager>();
            manager.mayus = false;

            _config?.Setup(transform, out manager.keys);
        }
    }
}