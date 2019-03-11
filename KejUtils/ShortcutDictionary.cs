using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    /// <summary>
    /// A dictionary of strings to values. Also searches for shortcuts for those strings, and returns all matches found if there are collisions on shortcuts.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ShortcutDictionary<T>
    {
        /// <summary>
        /// True if this instance allows any case for values.
        /// </summary>
        /// <param name="ignoreCase"></param>
        public ShortcutDictionary(bool ignoreCase = false)
        {
            this.ignoreCase = ignoreCase;
        }

        bool ignoreCase;
        Dictionary<string, T> mainOptions = new Dictionary<string, T>();
        Dictionary<string, SubstringOptionContainer<T>> substringOptions = new Dictionary<string, SubstringOptionContainer<T>>();
        public void Add(string fullText, T value)
        {
            if (ignoreCase) fullText = fullText.ToUpper();
            mainOptions.Add(fullText, value);
            for (int i = 1; i < fullText.Length; i++)
            {
                string substr = fullText.Substring(0, i);
                SubstringOptionContainer<T> old;
                if (substringOptions.TryGetValue(substr, out old))
                {
                    if (old.list == null)
                    {
                        old.list = new List<T>();
                        old.list.Add(old.single);
                        substringOptions[substr] = old;
                    }
                    old.list.Add(value);
                }
                else
                {
                    substringOptions.Add(substr, new SubstringOptionContainer<T>() { single = value });
                }
            }
        }
        /// <summary>
        /// Try to look up a value from a given input.
        /// </summary>
        /// <param name="key">Input to search for.</param>
        /// <param name="value">Found value, if a single one exists.</param>
        /// <param name="options">Found options, if there are multiple values that 'input' matches</param>
        /// <returns>True if a single value was found. False if 0 or many options were found.</returns>
        public bool TryGet(string key, out T value, out List<T> options)
        {
            if (ignoreCase) key = key.ToUpper();
            options = null;
            if (mainOptions.TryGetValue(key, out value))
                return true;
            SubstringOptionContainer<T> group;
            if (substringOptions.TryGetValue(key, out group))
            {
                options = group.list;
                if (options == null)
                {
                    value = group.single;
                    return true;
                }
                return false; //Ambiguous text.
            }
            return false; //No valid option.
        }
        public bool Remove(string key)
        {
            if (!ignoreCase) key = key.ToUpper();
            T value;
            if (mainOptions.TryGetValue(key, out value))
            {
                mainOptions.Remove(key);
                for (int i = 1; i < key.Length; i++)
                {
                    string substr = key.Substring(0, i);
                    SubstringOptionContainer<T> old = substringOptions[substr];
                    if (old.list != null)
                    {
                        old.list.Remove(value);
                        if (old.list.Count == 1)
                        {
                            old.single = old.list[0];
                            old.list = null;
                            substringOptions[substr] = old;
                        }
                    }
                    else
                    {
                        substringOptions.Remove(substr);
                    }
                }

                return true;
            }
            return false;
        }
        public void Clear()
        {
            mainOptions.Clear();
            substringOptions.Clear();
        }
        struct SubstringOptionContainer<U>
        {
            public U single;
            public List<U> list;
        }
    }
}
