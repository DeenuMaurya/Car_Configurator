using UnityEngine;

namespace CarShowroom
{
    // Offline proof of concept; replace with a server-validated LLM tool call for natural language.
    public class SimpleTextCommandAdapter : MonoBehaviour
    {
        [SerializeField] private AICommandExecutor executor;
        [SerializeField] private CarConfiguratorFeatureActions featureActions;

        public void Submit(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return;
            string text = phrase.Trim().ToLowerInvariant();
            if (featureActions && TryFeaturePhrase(text)) return;
            if (!executor) { Debug.LogWarning("Assign either a feature controller or AICommandExecutor."); return; }
            string action = "", value = "";
            if (text.Contains("back seat") || text.Contains("rear seat")) { action = "go_to_seat"; value = "RearLeft"; }
            else if (text.Contains("driver seat") || text.Contains("inside the car")) { action = "go_to_seat"; value = "Driver"; }
            else if (text.Contains("exit car") || text.Contains("outside")) action = "exit_car";
            else if (text.Contains("sunroof")) { action = "set_sunroof"; value = text.Contains("close") ? "off" : "on"; }
            else if (text.Contains("music")) { action = "set_music"; value = text.Contains("stop") || text.Contains("off") ? "off" : "on"; }
            else if (text.Contains(" air conditioning") || text.StartsWith("ac ") || text.Contains(" ac ")) { action = "set_ac"; value = text.Contains("off") ? "off" : "on"; }
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

        // Offline grammar for text input or transcribed speech. No microphone / cloud AI is assumed.
        private bool TryFeaturePhrase(string text)
        {
            if (text.Contains("color") || text.Contains("colour"))
            {
                string color = FindNamedColor(text);
                if (color != null)
                {
                    string area = text.Contains("rear") || text.Contains("back seat") ? "rear_seat_color" :
                        text.Contains("seat") ? "seat_color" : text.Contains("dashboard") ? "dashboard_color" :
                        text.Contains("ambient") ? "ambient_color" : text.Contains("door") ? "door_color" : "body_color";
                    return featureActions.ExecuteFeature(area, color);
                }
            }

            if (text.Contains("inside the car") || text.Contains("driver seat")) return featureActions.ExecuteFeature("seat", "Driver");
            if (text.Contains("back seat") || text.Contains("rear seat")) return featureActions.ExecuteFeature("seat", "RearLeft");
            if (text.Contains("exit car") || text.Contains("outside the car")) return featureActions.ExecuteFeature("exit_car", "");

            if (text.Contains("door") && (text.Contains("open") || text.Contains("close")))
            {
                if (text.Contains("all")) return featureActions.ExecuteFeature(text.Contains("open") ? "all_doors_open" : "all_doors_close", "");
                string door = text.Contains("rear") || text.Contains("back") ? "rear_" : "front_";
                door += text.Contains("right") || text.Contains("passenger") ? "right_door" : "left_door";
                return featureActions.ExecuteFeature(text.Contains("open") ? "door_open" : "door_close", door);
            }
            if (text.Contains("window") && (text.Contains("open") || text.Contains("close")))
            {
                string window = text.Contains("rear") || text.Contains("back") ? "rear_" : "front_";
                window += text.Contains("right") || text.Contains("passenger") ? "right_window" : "left_window";
                return featureActions.ExecuteFeature(text.Contains("open") ? "window_open" : "window_close", window);
            }
            if (text.Contains("metallic") || text.Contains("matte") || text.Contains("glossy"))
                return featureActions.ExecuteFeature("paint_finish", text.Contains("metallic") ? "Metallic" : text.Contains("matte") ? "Matte" : "Glossy");
            if (text.Contains("top model") || text.Contains("mid model") || text.Contains("base model"))
                return featureActions.ExecuteFeature("variant", text.Contains("top") ? "Top" : text.Contains("mid") ? "Mid" : "Base");
            if (text.Contains("headlight")) return featureActions.ExecuteFeature("set_headlights", text.Contains("off") ? "off" : "on");
            if (text.Contains("horn") || text.Contains("honk")) return featureActions.ExecuteFeature("honk", "");
            if (text.Contains("sunroof")) return featureActions.ExecuteFeature("set_sunroof", text.Contains("close") || text.Contains("off") ? "off" : "on");
            if (text.Contains("music")) return featureActions.ExecuteFeature("set_music", text.Contains("stop") || text.Contains("off") ? "off" : "on");
            if (text.Contains("air conditioning") || text.StartsWith("ac ") || text.Contains(" ac ")) return featureActions.ExecuteFeature("set_ac", text.Contains("off") ? "off" : "on");
            if (text.Contains("night mode") || text.Contains("day mode")) return featureActions.ExecuteFeature("day_night", "");
            if (text.Contains("premium sound")) return featureActions.ExecuteFeature("premium_audio", "");
            if (text.Contains("wireless charging")) return featureActions.ExecuteFeature("wireless_charging", "");
            if (text.Contains("digital cluster")) return featureActions.ExecuteFeature("digital_cluster", "");
            if (text.Contains("360") || text.Contains("rotate car")) return featureActions.ExecuteFeature("orbit_auto", "");
            if (text.Contains("save")) return featureActions.ExecuteFeature("save", "");
            if (text.Contains("reset")) return featureActions.ExecuteFeature("reset", "");
            if (text.Contains("share")) return featureActions.ExecuteFeature("share", "");
            return false;
        }

        private static string FindNamedColor(string text)
        {
            foreach (string name in new[] { "white", "black", "red", "green", "blue", "silver", "orange",
                     "yellow", "beige", "brown", "pink", "purple", "tan" })
                if (text.Contains(name)) return name;
            return null;
        }
    }
}
