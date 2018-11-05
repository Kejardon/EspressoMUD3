using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public interface IEncrypter
    {
        /// <summary>
        /// Encrypts a string so it can safely be stored and handled.
        /// </summary>
        /// <param name="sourceString">String to encrypt</param>
        /// <returns>Encrypted string</returns>
        string Encrypt(string sourceString);
        /// <summary>
        /// Checks if a plaintext string matches an encrypted string.
        /// </summary>
        /// <param name="inputString">Plaintext string</param>
        /// <param name="encryptedString">A string previously encrypted with this class' Encrypt algorithm</param>
        /// <returns>True iff the strings match.</returns>
        bool Compare(string inputString, string encryptedString);
    }

    /// <summary>
    /// Placeholder class that doesn't actually encrypt anything.
    /// </summary>
    public class NonEncryption : IEncrypter
    {
        public bool Compare(string inputString, string encryptedString)
        {
            return inputString == encryptedString;
        }
        public string Encrypt(string sourceString)
        {
            return sourceString;
        }
    }

    /// <summary>
    /// Uses BCrypt.Net implementation. Recommended default.
    /// </summary>
    public class BCryptEncryption : IEncrypter
    {
        public bool Compare(string inputString, string encryptedString)
        {
            return BCrypt.Net.BCrypt.Verify(inputString, encryptedString);
        }
        public string Encrypt(string sourceString)
        {
            return BCrypt.Net.BCrypt.HashPassword(sourceString);
        }
    }
}
