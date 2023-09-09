using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CornBot.Utilities
{
    public class WordDetector
    {

        public Regex PartialRE { get; private set; }
        public Regex FullRE { get; private set; }
        public string Emoji { get; private set; }

        public enum DetectionLevel
        {
            NONE,
            PARTIAL,
            FULL
        }

        public WordDetector(string word, string emoji)
        {
            word = word.ToLower();

            var partialTemplate = "+.*";
            var fullTemplate = "+([^\\w\\d]|%c)*";

            var partialRaw = new StringBuilder();
            var fullRaw = new StringBuilder();

            for (int i = 0; i < word.Length; i++)
            {
                partialRaw.Append(word[i]);
                fullRaw.Append(word[i]);
                if (i != word.Length - 1)
                {
                    partialRaw.Append(partialTemplate.Replace("%c", word[i].ToString()));
                    fullRaw.Append(fullTemplate.Replace("%c", word[i].ToString()));
                }
            }

            PartialRE = new Regex(partialRaw.ToString());
            FullRE = new Regex(fullRaw.ToString());

            Emoji = emoji;
        }

        public DetectionLevel Parse(string input)
        {
            input = input.ToLower();
            if (input.Contains(Emoji) || FullRE.IsMatch(input))
                return DetectionLevel.FULL;
            else if (PartialRE.IsMatch(input))
                return DetectionLevel.PARTIAL;
            return DetectionLevel.NONE;
        }

    }
}
