namespace UnityEngine.SceneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField, Scene] private string _path;

        public void AddScene() => SceneController.Instance.AddScene(_path);
        public void SwipeScene() => SceneController.Instance.ChangeScene(_path);
        public void RemoveScene() => SceneController.Instance.RemoveScene(_path);
    }
}