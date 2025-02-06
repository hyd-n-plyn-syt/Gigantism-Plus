using System;
using System.Collections.Generic;
using System.Linq;

namespace Mods.GigantismPlus
{
    public static class Debug
    {
        private static int VerbosityOption => Options.DebugVerbosity;
        // Verbosity translates in roughly the following way:
        // 0 : Critical. Use sparingly, if at all, as they show up without the option. Move these to 1 when pushing to main.
        // 1 : Show. Initial debugging entries. Broad, general "did it happen?" style entries for basic trouble-shooting.
        // 2 : Verbose. Entries have more information in them, indicating how values are passed around and changed.
        // 3 : Very Verbose. Entries in more locations, or after fewer steps. These contribute to tracing program flow.
        // 4 : Maximally Verbose. Just like, all of it. Every step of a process, as much detail as possible.

        private static bool IncludeInMessage => Options.DebugIncludeInMessage;

        private static void Message(string text)
        {
            XRL.Messages.MessageQueue.AddPlayerMessage(text);
        }

        private static void Log(string text)
        {
            UnityEngine.Debug.LogError(text);
        }

        public static void Entry(int verbosity, string text)
        {
            if (verbosity > VerbosityOption) return;
            Log(text);
            if (IncludeInMessage) 
            {
                Message(text);
            }
        }

        public static void Entry(string text)
        {
            int verbosity = 0;
            Entry(verbosity, text);
        }

        public static void Entry(int verbosity, string label, string text)
        {
            string output = label + ": " + text;
            Entry(verbosity, output);
        }

    } //!--- public static class Debug
}