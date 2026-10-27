using UnityEngine;

namespace CarShowroom
{
    // Offline proof of concept; replace with a server-validated LLM tool call for natural language.
    public class SimpleTextCommandAdapter : MonoBehaviour
    {
        [SerializeField] private AICommandExecutor executor;

        public void Submit(string phrase)
        {
            if (!executor || string.IsNullOrWhiteSpace(phrase)) return;
            string text = phrase.Trim().ToLowerInvariant();
            string action = "", value = "";
            if (text.Contains("back seat") || text.Contains("rear seat")) { action = "go_to_seat"; value = "RearLeft"; }
            else if (text.Contains("driver seat") || text.Contains("inside the car")) { action = "go_to_seat"; value = "Driver"; }
            else if (text.Contains("exit car") || text.Contains("outside")) action = "exit_car";
            else if (text.Contains("sunroof")) { action = "set_sunroof"; value = text.Contains("close") ? "off" : "on"; }
            else if (text.Contains("music")) { action = "set_music"; value = text.Contains("stop") || text.Contains("off") ? "off" : "on"; }
            else if (text.Contains("ac") || text.Contains("air conditioning")) { action = "set_ac"; value = text.Contains("off") ? "off" : "on"; }
            else if (text.Contains("top model")) { action = "set_variant"; value = "Top"; }
            else if (text.Contains("base model")) { action = "set_variant"; value = "Base"; }
            else if (text.Contains("mid model")) { action = "set_variant"; value = "Mid"; }
            else if (text.Contains("color") || text.Contains("colour"))
            {
                action = text.Contains("seat") ? "change_seat_color" : "change_car_color";
                foreach (string color in new[] { "red", "green", "white", "black", "blue", "silver", "beige", "brown" })
                    if (text.Contains(color)) { value = color; break; }
            }
            if (!string.IsNullOrEmpty(action)) executor.Execute(new AIAction { action = action, value = value });
            else Debug.LogWarning("Command not recognized: " + phrase);
        }
    }
}
