// Adds every character the interface can show to the dynamic font atlas once, while the main screen is still loading.
// Without it TextMeshPro builds a glyph the first time it is drawn, which is a small hitch mid-game.

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

namespace PackItPerfect
{
    /// <summary>One-shot font atlas preparation. Presentation only; safe to skip.</summary>
    public static class FontWarmup
    {
        const int LAST_BASIC_CHARACTER = 0x2100; // letters, digits and punctuation; emoji come from the sprite asset

        static bool done;

        /// <summary>Collects the characters of every string constant in the given types plus any extra text, and adds them to the default font.</summary>
        public static void Run(IEnumerable<string> extraText, params System.Type[] stringTables)
        {
            if (done) return;
            done = true;
            var font = TMP_Settings.defaultFontAsset;
            if (font == null) return;
            var seen = new HashSet<char>();
            var characters = new StringBuilder();
            void Add(string text)
            {
                if (string.IsNullOrEmpty(text)) return;
                foreach (char c in text)
                {
                    if (c < ' ' || c >= LAST_BASIC_CHARACTER || char.IsSurrogate(c) || c == '️') continue;
                    if (seen.Add(c)) characters.Append(c);
                }
            }
            Add("0123456789 %+-–·:/.,!?'\"()…xX");
            foreach (var table in stringTables)
            {
                foreach (var field in table.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.IsLiteral && field.FieldType == typeof(string)) Add((string)field.GetRawConstantValue());
                }
            }
            if (extraText != null)
            {
                foreach (var text in extraText) Add(text);
            }
            font.TryAddCharacters(characters.ToString(), out _);
        }
    }
}
