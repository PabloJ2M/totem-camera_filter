namespace UnityEngine.SceneManagement
{
    [RequireComponent(typeof(CanvasGroup))]
    public class SceneFadeEffect : MonoBehaviour
    {
        [SerializeField, Range(0f, 5f)] private float _speed = 1f;
        [SerializeField] protected bool _unscaleTime;

        private CanvasGroup _canvasGroup;
        private sbyte _fadeDirection;
        private bool _isComplete;

        public string ScenePath { private get; set; }

        private void Awake() => _canvasGroup = GetComponent<CanvasGroup>();

        private void Start()
        {
            bool isLoadingScene = !string.IsNullOrEmpty(ScenePath);
            _canvasGroup.alpha = isLoadingScene ? 0.001f : 0.999f;
            _fadeDirection = (sbyte)(isLoadingScene ? 1 : -1);
        }
        private void LateUpdate()
        {
            if (_isComplete) return;
            float delta = _unscaleTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _canvasGroup.alpha += delta * _speed * _fadeDirection;

            switch(_canvasGroup.alpha)
            {
                case 1f: SceneManager.LoadSceneAsync(ScenePath); _isComplete = true; break;
                case 0f: Destroy(gameObject); break;
            };
        }
    }
}