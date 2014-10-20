﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
#if DEBUG
using System.Diagnostics;

#endif

namespace XkPassword
{
    /// <summary>
    ///     A password generator inspired by a well-known web comic.
    /// </summary>
    public class XkPasswd
    {
        /// <summary>
        ///     A random number generator.
        /// </summary>
        private static readonly Random RandomNumberGenerator = new Random();

        private int _maxWordLength;

        private int _minWordLength;

        private int _padCharsAfter;

        private int _padCharsBefore;

        private int _padDigitsAfter;

        private int _padDigitsBefore;

        private int _wordCount;

        /// <summary>
        ///     Initializes a new instance of the <see cref="XkPasswd" /> class.
        /// </summary>
        public XkPasswd()
        {
            this.WordListPath = "?en.gz";
            this.SymbolAlphabet = new HashSet<char>
                                  {
                                      '!',
                                      '@',
                                      '$',
                                      '%',
                                      '^',
                                      '&',
                                      '*',
                                      '-',
                                      '_',
                                      '+',
                                      '=',
                                      ':',
                                      '|',
                                      '~',
                                      '?'
                                  };
            this.SeparatorAlphabet = null;
            this.MinWordLength = 4;
            this.MaxWordLength = 8;
            this.WordCount = 4;
            this.SeparatorCharacter = null;
            this.PaddingDigitsAfter = 2;
            this.PaddingDigitsBefore = 2;
            this.PaddingType = Padding.Fixed;
            this.PaddingCharacter = null;
            this.PaddingCharactersAfter = 2;
            this.PaddingCharactersBefore = 2;
            this.PadToLength = 0;
            this.CaseTransform = CaseTransformation.Capitalize;
            this.CharacterSubstitutions = new Dictionary<char, char>();
        }

        /// <summary>
        ///     Gets or sets the path to the word list used by this instance of the password generator.
        /// </summary>
        /// <value>
        ///     A string representing the path to the word list file.  If the path starts with "?", it is
        ///     assumed to refer to a gzipped embedded resource.
        /// </value>
        public string WordListPath { get; set; }

        /// <summary>
        ///     Gets or sets the set of symbols that may appear in passwords generated by this instance of the password generator.
        /// </summary>
        /// <value>
        ///     A <see cref="HashSet{T}" /> of <see cref="char" />s representing the set of symbols that may appear
        ///     in generated passwords.
        /// </value>
        public HashSet<char> SymbolAlphabet { get; set; }

        /// <summary>
        ///     Gets or sets the set of symbols that may appear in as separators in passwords generated by this instance of the
        ///     password generator.
        ///     If null or empty, the value of <see cref="SymbolAlphabet" /> will be used.
        /// </summary>
        /// <value>
        ///     A <see cref="HashSet{T}" /> of <see cref="char" />s representing the set of symbols that may appear as separators
        ///     in generated passwords, or null or an empty <see cref="HashSet{T}" /> of <see cref="char" /> to indicate that
        ///     <see cref="SymbolAlphabet" /> should be used.
        /// </value>
        public HashSet<char> SeparatorAlphabet { get; set; }

        /// <summary>
        ///     Gets or sets the minimum length of words that may appear in generated passwords.
        /// </summary>
        /// <value>
        ///     An integer.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to set value to less than or equal to 0.</exception>
        public int MinWordLength
        {
            get { return (this._minWordLength < this._maxWordLength) ? this._minWordLength : this._maxWordLength; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "Minimum word length cannot be less than 1");
                }

                this._minWordLength = value;
            }
        }

        /// <summary>
        ///     Gets or sets the maximum length of words that may appear in generated passwords.
        /// </summary>
        /// <value>
        ///     An integer.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to set value to less than or equal to 0.</exception>
        public int MaxWordLength
        {
            get { return (this._maxWordLength > this._minWordLength) ? this._maxWordLength : this._minWordLength; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "Maximum word length cannot be less than 1");
                }

                this._maxWordLength = value;
            }
        }

        /// <summary>
        ///     Gets or sets the number of words that will appear in generated passwords.
        /// </summary>
        /// <value>
        ///     A positive integer greater than or equal to 1.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to set value to less than or equal to 0.</exception>
        public int WordCount
        {
            get { return this._wordCount; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "Word count cannot be less than 1");
                }
                this._wordCount = value;
            }
        }

        /// <summary>
        ///     Gets or sets the specific character to use as a separator in generated passwords.
        ///     If ASCII NUL ('\0'), no separator will be used.
        ///     If null, a random separator will be chosen from <see cref="SeparatorAlphabet" /> if set, or
        ///     <see cref="SymbolAlphabet" />.
        /// </summary>
        /// <value>
        ///     A <see cref="Nullable{T}" /> of <see cref="char" />.  ASCII NUL ('\0') means no separator; a null value means
        ///     choose randomly
        ///     from <see cref="SeparatorAlphabet" /> if it is set, or <see cref="SymbolAlphabet" /> otherwise.
        /// </value>
        public char? SeparatorCharacter { get; set; }

        /// <summary>
        ///     Gets or sets the number of padding digits that will appear before the words in a generated password.
        /// </summary>
        /// <value>
        ///     A positive, nonzero integer.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to set value to less than 0.</exception>
        public int PaddingDigitsBefore
        {
            get { return this._padDigitsBefore; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "Cannot have negative number of padding digits");
                }

                this._padDigitsBefore = value;
            }
        }

        /// <summary>
        ///     Gets or sets the number of padding digits that will appear after the words in a generated password.
        /// </summary>
        /// <value>
        ///     The padding digits after.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to set value to less than 0.</exception>
        public int PaddingDigitsAfter
        {
            get { return this._padDigitsAfter; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "Cannot have negative number of padding digits");
                }

                this._padDigitsAfter = value;
            }
        }

        /// <summary>
        ///     Gets or sets the type of padding to use in generated passwords.
        /// </summary>
        /// <value>
        ///     A member of the <see cref="PaddingType" /> enumeration.
        /// </value>
        public Padding PaddingType { get; set; }

        /// <summary>
        ///     Gets or sets the character that will be used as padding in generated passwords.
        ///     If ASCII NUL ('\0') or null, a random padding character will be chosen from <see cref="SymbolAlphabet" />.
        /// </summary>
        /// <value>
        ///     A <see cref="Nullable{T}" /> of <see cref="char" />.  ASCII NUL ('\0') or null value means choose randomly from
        ///     <see cref="SeparatorAlphabet" />.
        /// </value>
        public char? PaddingCharacter { get; set; }

        /// <summary>
        ///     Gets or sets the number of padding characters that will appear before the initial padding digits in generated
        ///     passwords.
        /// </summary>
        /// <value>
        ///     An integer.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to set value to less than 0.</exception>
        public int PaddingCharactersBefore
        {
            get { return this._padCharsBefore; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "Cannot have negative number of padding characters");
                }

                this._padCharsBefore = value;
            }
        }

        /// <summary>
        ///     Gets or sets the number of padding characters that will appear after the terminal padding digits in generated
        ///     passwords.
        /// </summary>
        /// <value>
        ///     An integer greater than or equal to 0.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to set value to less than 0.</exception>
        public int PaddingCharactersAfter
        {
            get { return this._padCharsAfter; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "Cannot have negative number of padding characters");
                }

                _padCharsAfter = value;
            }
        }

        /// <summary>
        ///     Gets or sets the length to which to pad generated passwords.  Has no effect if <see cref="PaddingType" /> is not
        ///     <see cref="Padding.Adaptive" />.
        /// </summary>
        /// <value>
        ///     A positive, non-zero integer.
        /// </value>
        public int PadToLength { get; set; }

        /// <summary>
        ///     Gets or sets the case transformation that will be applied to words appearing in generated passwords.
        /// </summary>
        /// <value>
        ///     A member of the <see cref="CaseTransformation" /> enumeration.
        /// </value>
        public CaseTransformation CaseTransform { get; set; }

        /// <summary>
        ///     Gets or sets the dictionary of character substitutions to apply to words appearing in generated passwords.
        /// </summary>
        /// <value>
        ///     A <see cref="Dictionary{T,T}" /> of <see cref="char" />,<see cref="char" /> with the keys being the characters to
        ///     replace, and the values being
        ///     their replacements.
        /// </value>
        public Dictionary<char, char> CharacterSubstitutions { get; set; }

        /// <summary>
        ///     Randomly selects words between <see cref="MinWordLength" /> characters and <see cref="MaxWordLength" /> characters
        ///     in length, inclusive,
        ///     from the word list.
        /// </summary>
        /// <param name="reader">A <see cref="StreamReader" /> for the word list.</param>
        /// <returns>A number of random words meeting the word length limitations specified by the configuration.</returns>
        private IEnumerable<string> GetRandomWords(StreamReader reader)
        {
#if DEBUG
            Debug.WriteLine(string.Format("Processing wordlist to get words between {0} and {1} chars",
                                          this.MinWordLength,
                                          this.MaxWordLength));
            Stopwatch sw = Stopwatch.StartNew();
#endif //DEBUG

            IEnumerable<string> suitableWords =
                reader.ReadAllLines()
                      .Where(w => (w.Length > this.MinWordLength) && (w.Length < this.MaxWordLength))
                      .ToList();

#if DEBUG
            sw.Stop();
            Debug.WriteLine(string.Format("Done in {0}", sw.Elapsed));
#endif //DEBUG

            for (int i = 0; i < this.WordCount; i++)
            {
                yield return suitableWords.ElementAt(RandomNumberGenerator.Next(0, suitableWords.Count()));
            }
        }

        /// <summary>
        ///     Transforms the case of a given <see cref="IEnumerable{T}" /> of <see cref="string" /> of words
        ///     according to the <see cref="CaseTransformation" /> configured on the generator.
        /// </summary>
        /// <param name="words">The words whose cases are to be transformed.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="string" /> of words with their cases transformed.</returns>
        private IEnumerable<string> TransformCase(IEnumerable<string> words)
        {
            switch (this.CaseTransform)
            {
                case CaseTransformation.None:
                    return words;
                case CaseTransformation.UpperCase:
                    return words.Select(w => w.ToUpperInvariant());
                case CaseTransformation.LowerCase:
                    return words.Select(w => w.ToLowerInvariant());
                case CaseTransformation.Capitalize:
                    return words.Select(w => w.Substring(0, 1).ToUpperInvariant() + w.Substring(1));
                case CaseTransformation.Invert:
                    return words.Select(w => w.Substring(0, 1).ToLowerInvariant() + w.Substring(1).ToUpperInvariant());
                case CaseTransformation.Alternate:
                    return words.Select(w =>
                        {
                            char[] wChars = w.ToLowerInvariant().ToCharArray();
                            bool startCaps = RandomNumberGenerator.CoinFlip();
                            for (int i = 0; i < wChars.Length; i++)
                            {
                                if (startCaps)
                                {
                                    if ((i % 2) == 0)
                                    {
                                        wChars[i] -= ' ';
                                    }
                                }
                                else
                                {
                                    if ((i % 2) != 0)
                                    {
                                        wChars[i] -= ' ';
                                    }
                                }
                            }
                            return new string(wChars);
                        }
                        );
                case CaseTransformation.Random:
                    return
                        words.Select(
                                     w =>
                                     new string(
                                         w.ToUpperInvariant().Select(c => (char)(c + (RandomNumberGenerator.CoinFlip() ? ' ' : '\0')))
                                          .ToArray()));
                default:
                    return words;
            }
        }

        /// <summary>
        ///     Gets random digits between 0 and 9 inclusive.
        /// </summary>
        /// <param name="num">The number of random digits to return.</param>
        /// <returns>The number of random digits specified by <paramref name="num" />.</returns>
        private static IEnumerable<char> GetRandomDigits(int num)
        {
            for (int i = 0; i < num; i++)
            {
                yield return (char)(RandomNumberGenerator.Next(48, 58));
            }
        }

        /// <summary>
        ///     Gets the separator character as a string.
        /// </summary>
        /// <returns>
        ///     The separator character to be used in passwords, as a <see cref="string" />.  If <see cref="SeparatorCharacter" />
        ///     is set,
        ///     that value is used as follows: no separator (the empty string) if <see cref="SeparatorCharacter" /> is ASCII NUL
        ///     ('\0'),
        ///     otherwise the character in <see cref="SeparatorCharacter" /> is returned.  If that is not set and
        ///     <see cref="SeparatorAlphabet" />
        ///     is set, a character is randomly selected from that.  If <see cref="XkPasswd.SeparatorAlphabet" /> is not set, a
        ///     character is randomly
        ///     selected from <see cref="SymbolAlphabet" />.
        /// </returns>
        private string GetSeparatorCharacter()
        {
            return this.SeparatorCharacter.HasValue
                       ? (this.SeparatorCharacter.Value == '\0'
                              ? ""
                              : this.SeparatorCharacter.Value.ToString(CultureInfo.InvariantCulture))
                       : (this.SeparatorAlphabet == null
                              ? this.SymbolAlphabet.ElementAt(RandomNumberGenerator.Next(0, this.SymbolAlphabet.Count))
                                    .ToString(CultureInfo.InvariantCulture)
                              : this.SeparatorAlphabet.ElementAt(RandomNumberGenerator.Next(0, this.SeparatorAlphabet.Count))
                                    .ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Performs character substitutions on an <see cref="IEnumerable{T}" /> of <see cref="string" /> according to the
        ///     substitutions provided in <see cref="CharacterSubstitutions" />.
        /// </summary>
        /// <param name="words">The words to perform substitutions on.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="string" /> of words with characters substituted.</returns>
        private IEnumerable<string> SubstituteCharacters(IEnumerable<string> words)
        {
            if (this.CharacterSubstitutions == null)
            {
                foreach (string w in words)
                {
                    yield return w;
                }
            }
            else
            {
                foreach (string word in words)
                {
                    string subWord = word;
                    foreach (var sub in this.CharacterSubstitutions)
                    {
                        subWord = word.Replace(sub.Key, sub.Value);
                    }
                    yield return subWord;
                }
            }
        }

        /// <summary>
        ///     Generates a password according to the rules configured in this instance.
        /// </summary>
        /// <returns>A password matching the configuration options of this instance.</returns>
        public string Generate()
        {
            bool hadPaddingChar = this.PaddingCharacter.HasValue && (this.PaddingCharacter.Value != '\0');
            var passwordBuilder = new StringBuilder();

            using (var reader = new StreamReader(
                this.WordListPath.StartsWith("?")
                    ? (Stream)new GZipStream(Assembly.GetExecutingAssembly()
                                                     .GetManifestResourceStream("XkPassword."
                                                                                + this.WordListPath.Substring(1)),
                                             CompressionMode.Decompress)
                    : File.Open(this.WordListPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                string separator = this.GetSeparatorCharacter();
                IEnumerable<string> randomWords = this.SubstituteCharacters(
                                                                            (this.CaseTransform
                                                                             == CaseTransformation.None
                                                                                 ? this.GetRandomWords(reader)
                                                                                 : this.TransformCase(this
                                                                                                          .GetRandomWords
                                                                                                          (reader))));

                IEnumerable<char> startPaddingDigits = (this.PaddingDigitsBefore > 0)
                                                           ? GetRandomDigits(this.PaddingDigitsBefore)
                                                           : new List<char>(),
                                  endPaddingDigits = (this.PaddingDigitsAfter > 0)
                                                         ? GetRandomDigits(this.PaddingDigitsAfter)
                                                         : new List<char>();

                passwordBuilder.Append(startPaddingDigits.ToArray());
                if (this.PaddingDigitsBefore > 0)
                {
                    passwordBuilder.Append(separator);
                }
                passwordBuilder.Append(string.Join(separator, randomWords.ToArray()));
                if (this.PaddingDigitsAfter > 0)
                {
                    passwordBuilder.Append(separator);
                }
                passwordBuilder.Append(endPaddingDigits.ToArray());

                switch (this.PaddingType)
                {
                    case Padding.None:
                        break;
                    case Padding.Fixed:
                        if (!hadPaddingChar)
                        {
                            this.PaddingCharacter = separator[0];
                        }

                        if (this.PaddingCharactersBefore > 0)
                        {
                            passwordBuilder.Insert(0,
                                                   this.PaddingCharacter.Value.ToString(CultureInfo.InvariantCulture)
                                                   + this.SeparatorCharacter,
                                                   this.PaddingCharactersBefore);
                        }

                        if (this.PaddingCharactersAfter > 0)
                        {
                            passwordBuilder.Append(this.PaddingCharacter.Value, this.PaddingCharactersAfter);
                        }

                        if (!hadPaddingChar)
                        {
                            this.PaddingCharacter = null;
                        }
                        break;
                    case Padding.Adaptive:
                        if (this.PadToLength <= 0)
                        {
                            break;
                        }

                        if (!hadPaddingChar)
                        {
                            this.PaddingCharacter =
                                this.SymbolAlphabet.ElementAt(RandomNumberGenerator.Next(0, this.SymbolAlphabet.Count));
                        }
                        if (passwordBuilder.Length < this.PadToLength)
                        {
                            passwordBuilder.Append(this.PaddingCharacter.Value,
                                                   this.PadToLength - passwordBuilder.Length);
                        }
                        else if (passwordBuilder.Length > this.PadToLength)
                        {
                            passwordBuilder.Truncate(this.PadToLength);
                        }
                        break;
                }
            }

            return passwordBuilder.ToString();
        }

        /// <summary>
        ///     Generates the specified number of passwords.
        /// </summary>
        /// <param name="numPasswords">The number of passwords to generate.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="string" /> of passwords.</returns>
        public IEnumerable<string> Generate(int numPasswords)
        {
            for (int i = 0; i < numPasswords; i++)
            {
                yield return this.Generate();
            }
        }
    }
}