using System;
using System.Collections.Generic;
using System.Linq;
using XRL.UI;

namespace Mods.GigantismPlus
{
    public static class Options
    {
        // Per the wiki, code is taken 1:1
        private static string GetOption(string ID, string Default = "")
        {
            return XRL.UI.Options.GetOption(ID, Default: Default);
        }

        // this turns the 3 options into a 0-2 value, -1 is not found but shouldn't be possible outside of the values being changed in Options.xml and not reflected here.
        // I'll probably move the return value to a variable, and throw a message in the log to make those situations easier to hunt down.
        private static int ParseSelectPlayer(string value)
        {
            string[] Options = {
                "No one",
                "Gigantic ({{r|D}}) players",
                "Everyone" };
            return Array.FindIndex(Options, v => v == value);
        }

        // I believe these might actually need a full reload and not just a new save to take effect.
        // TODO: make them reinitialise between new games if they don't already.
        // - Rapid advancement option seems to work on the fly.

        // Checkbox settings
        public static bool EnableGiganticStartingGear => GetOption("Option_GigantismPlus_EnableGiganticStartingGear").EqualsNoCase("Yes");
        public static bool EnableGiganticStartingGear_Grenades => GetOption("Option_GigantismPlus_EnableGiganticStartingGear_Grenades").EqualsNoCase("Yes");
        public static bool EnableGigantismRapidAdvance => GetOption("Option_GigantismPlus_EnableGigantismRapidAdvance").EqualsNoCase("Yes");

        // Selector Options
        public static int SelectGiganticTinkering => ParseSelectPlayer(GetOption("Option_GigantismPlus_SelectGiganticTinkering"));
        public static int SelectGiganticDerarification => ParseSelectPlayer(GetOption("Option_GigantismPlus_SelectGiganticDerarification"));

        // Debug Settings
        private static int _DebugVerbosity;
        public static int DebugVerbosity
        {
            get
            {
                _DebugVerbosity = 0;
                foreach (KeyValuePair<string, GameOption> option in XRL.UI.Options.OptionsByID)
                {
                    string optionID = option.Key;
                    if (optionID.Contains("Option_GigantismPlus_DebugV", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!GetOption(option.Key).EqualsNoCase("Yes"))
                        {
                            break;
                        }
                        _DebugVerbosity++;
                    }
                }
                return _DebugVerbosity;
            }
            private set
            {
                _DebugVerbosity = value;
            }
        }

        public static bool DebugIncludeInMessage
        {
            get
            {
                return GetOption("Option_GigantismPlus_DebugIncludeInMessage").EqualsNoCase("Yes");
            }
            private set
            {
                DebugIncludeInMessage = value;
            }
        }
    } //!--- public static class Options
}