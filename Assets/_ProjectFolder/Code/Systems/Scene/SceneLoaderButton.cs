namespace UnityEngine.SceneManagement
{
    using UI;

    [RequireComponent(typeof(Button))]
    public class SceneLoaderButton : SceneLoader
    {
        private void Start() => GetComponent<Button>().onClick.AddListener(SwipeScene);
    }
}