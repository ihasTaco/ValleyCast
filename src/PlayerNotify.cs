using StardewValley;

namespace ValleyCast {
    public class PlayerNotify {
        /// <summary>
        /// Displays a notification message with optional responses and a callback function.
        /// <para>This method is used to prompt the player with a message and handle their response.</para>
        /// </summary>
        /// <param name="message">The main text message to display.</param>
        /// <param name="responses">A list of <see cref="Response"/> objects representing the possible player responses. Can be null.</param>
        /// <param name="callback">An action to execute after a response is selected. The selected response is passed as a string argument to this action.</param>
        /// <example>
        /// <code>
        /// PlayerNotify playerNotify = new PlayerNotify();
        /// playerNotify.Notify(
        ///     "OBS is not recording, do you want to start recording?",
        ///     new List&lt;Response&gt; {
        ///         new Response("1", "Yes"),
        ///         new Response("2", "No")
        ///     },
        ///     answer => {
        ///         if (answer == "1") {
        ///             Console.WriteLine("Recording started.");
        ///             playerNotify.function1(); // Call function1() when "Yes" is selected
        ///         } else if (answer == "2") {
        ///             Console.WriteLine("Recording not started.");
        ///         }
        ///     }
        /// );
        /// </code>
        /// </example>
        public static void Dialogue(string message, List<Response>? responses, Action<string>? callback)
        {
            // Get the player's current location
            GameLocation location = Game1.currentLocation;

            // Convert the list of responses to an array (or use an empty array if responses is null)
            Response[] responseArray = responses?.ToArray() ?? Array.Empty<Response>();

            // Create the question dialogue with the provided message and responses
            location.createQuestionDialogue(message, responseArray, delegate (Farmer _, string answer) {
                // Directly invoke the callback function with the selected answer
                callback?.Invoke(answer);
            });
        }

        /// <summary>
        /// Displays a simple notification in the bottom-left corner of the screen.
        /// </summary>
        /// <param name="message">The status message to display.</param>
        /// <param name="type">The type of popup (</param>
        public static void Popup(string message, int type) {
            Game1.addHUDMessage(new HUDMessage(message, type));
        }
    }
}