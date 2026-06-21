using System.Collections.Generic;

namespace UnityEngine.SceneManagement
{
    public class SceneController : MonoBehaviour
    {
        [SerializeField] private SceneFadeEffect _fadePrefab;

        private HashSet<string> _loadedScenes = new();
        public bool IsLoadingScene { get; private set; }

        #region Singleton Basic
        public static SceneController Instance { get; private set; }
        private void Awake() => Instance = this;
        #endregion

        private void Start()
        {
            Time.timeScale = 1f;
            Instantiate(_fadePrefab, transform);
        }

        public void AddScene(string scenePath)
        {
            if (!_loadedScenes.Add(scenePath)) return;
            SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
        }
        public void RemoveScene(string scenePath)
        {
            if (!_loadedScenes.Remove(scenePath)) return;
            SceneManager.UnloadSceneAsync(scenePath);
        }
        public void ChangeScene(string scenePath)
        {
            if (IsLoadingScene) return;

            Instantiate(_fadePrefab, transform).ScenePath = scenePath;
            IsLoadingScene = true;
        }
    }
}