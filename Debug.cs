using System;

namespace Mods.GigantismPlus
{
    public static class Debug
    {
        public static bool send => Options.EnableGigantismPlusDebug;

        // XRL.Messages.MessageQueue.AddPlayerMessage("Hello world!")
        public static void Message(string text)
        {
            if (send)
            {
                string output = text;
                XRL.Messages.MessageQueue.AddPlayerMessage(output);
            }
        }

        public static void Message(string label, string text)
        {
            string output = label + ": " + text;
            Message(output);
        }

        // UnityEngine.Debug.LogError("Hello world!")
        public static void Log(string text)
        {
            if (send)
            {
                string output = text;
                UnityEngine.Debug.LogError(output);
            }
        }

        public static void Log(string label, string text)
        {
            string output = label + ": " + text;
            Log(output);
        }
    }
}