using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace ValleyCast
{
    public class PlayerNotify
    {
        private string statusMessage = null!;
        private Texture2D statusImage = null!;
        private int statusTimeLeft;
        private bool showStatusPopup;
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
        public static void Notify(string message, List<Response>? responses, Action<string>? callback)
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
        /// Displays a status popup with an image or a message in the bottom-right corner of the screen.
        /// </summary>
        /// <param name="message">The status message to display.</param>
        /// <param name="image">The image to display (e.g., OBS logo with a check or X).</param>
        /// <param name="duration">The duration in ticks for which the popup should be visible (60 ticks = 1 second).</param>
        public void ShowStatusPopup(string? message, Texture2D? image, int duration)
        {
            statusMessage = message!;
            statusImage = image!;
            statusTimeLeft = duration;
            showStatusPopup = true;

            // Hook into the SMAPI rendering event
            Helper.Events.Display.RenderedHud += OnRenderedHud;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (showStatusPopup)
            {
                statusTimeLeft--;

                if (statusTimeLeft <= 0)
                {
                    showStatusPopup = false;

                    // Unhook the events after the popup disappears
                    Helper.Events.Display.RenderedHud -= OnRenderedHud;
                    Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
                }
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (showStatusPopup)
            {
                SpriteBatch spriteBatch = Game1.spriteBatch;
                Vector2 position = new Vector2(Game1.graphics.GraphicsDevice.Viewport.Width - 150, Game1.graphics.GraphicsDevice.Viewport.Height - 100); // Bottom-right corner

                // Draw the image
                spriteBatch.Draw(statusImage, new Rectangle((int)position.X, (int)position.Y, 64, 64), Color.White);

                // Draw the text
                spriteBatch.DrawString(Game1.dialogueFont, statusMessage, new Vector2(position.X + 70, position.Y + 20), Color.White);
            }
        }
    }
}