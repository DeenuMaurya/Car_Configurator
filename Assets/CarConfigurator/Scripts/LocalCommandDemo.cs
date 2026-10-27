using UnityEngine;

namespace CarShowroom
{
    public class LocalCommandDemo : MonoBehaviour
    {
        [SerializeField] private AICommandExecutor executor;
        [SerializeField] private string json = "{\"action\":\"change_car_color\",\"value\":\"red\"}";

        [ContextMenu("Execute Sample Command")]
        public void ExecuteSample() => Debug.Log("Command succeeded: " + (executor && executor.ExecuteJson(json)));

        public void SubmitJson(string commandJson)
        {
            if (executor) executor.ExecuteJson(commandJson);
        }
    }
}
