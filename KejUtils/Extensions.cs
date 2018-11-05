﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KejUtils
{
    public static class Extensions
    {
        /// <summary>
        /// Attempts to add an object to a sorted List. Assumes each object should be unique.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">A sorted List object</param>
        /// <param name="newValue">New member to add</param>
        /// <returns>True if the object was added.</returns>
        public static bool BinaryAdd<T>(this List<T> list, T newValue, IComparer<T> comparer = null)
        {
            int index = list.BinarySearch(newValue, comparer);
            if (index < 0)
            {
                list.Insert(-index - 1, newValue);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Attempts to remove an object to a sorted List.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">A sorted List object</param>
        /// <param name="oldValue">Member to remove</param>
        /// <returns>True if the object was removed.</returns>
        public static bool BinaryRemove<T>(this List<T> list, T oldValue, IComparer<T> comparer = null)
        {
            int index = list.BinarySearch(oldValue, comparer);
            if (index >= 0)
            {
                list.RemoveAt(index);
                return true;
            }
            return false;
        }

        //I think these are already implemented with Write and ReadString. If I need a fixed length size for some bizarre reason this might be appropriate, but that seems unlikely.
        ///// <summary>
        ///// Write a length-specified string to data.
        ///// </summary>
        ///// <param name="writer">Data to write to</param>
        ///// <param name="str">String to write</param>
        ///// <param name="numBytesLength">1 through 4, for maximum size of string (255, 65535, 16777215, 2147483647)</param>
        //public static void WriteString(this BinaryWriter writer, string str, int numBytesLength)
        //{
        //    int length = str.Length;
        //    switch (numBytesLength)
        //    {
        //        case 1:
        //            if (length > (1 << 8) - 1) throw new ArgumentOutOfRangeException("str is too long for the given length");
        //            writer.Write((byte)length);
        //            break;
        //        case 2:
        //            if (length > (1 << 16) - 1) throw new ArgumentOutOfRangeException("str is too long for the given length");
        //            writer.Write((ushort)length);
        //            break;
        //        case 3:
        //            if (length > (1 << 24) - 1) throw new ArgumentOutOfRangeException("str is too long for the given length");
        //            byte[] data = BitConverter.GetBytes(length);
        //            if(BitConverter.IsLittleEndian)
        //            {
        //                writer.Write(data, 0, 3);
        //            }
        //            else
        //            {
        //                writer.Write(data, 1, 3);
        //            }
        //            break;
        //        case 4:
        //            if (length > (1 << 32) - 1) throw new ArgumentOutOfRangeException("str is too long for the given length");
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException("numBytesLength must be a value from 1 to 4");
        //    }
        //    writer.Write(str.ToCharArray());
        //}
        ///// <summary>
        ///// Read a length-specified string from data.
        ///// </summary>
        ///// <param name="reader">Data to read</param>
        ///// <param name="numBytesLength">1 through 4, for maximum size of string (255, 65535, 16777215, 2147483647)</param>
        ///// <returns></returns>
        //public static string ReadString(this BinaryReader reader, int numBytesLength)
        //{
        //    int length;
        //    switch (numBytesLength)
        //    {
        //        case 1:
        //            length = reader.ReadByte();
        //            break;
        //        case 2:
        //            length = reader.ReadUInt16();
        //            break;
        //        case 3:
        //            byte[] data = new byte[4];
        //            if (BitConverter.IsLittleEndian)
        //            {
        //                data[0] = reader.ReadByte();
        //                data[1] = reader.ReadByte();
        //                data[2] = reader.ReadByte();
        //            }
        //            else
        //            {
        //                data[1] = reader.ReadByte();
        //                data[2] = reader.ReadByte();
        //                data[3] = reader.ReadByte();
        //            }
        //            length = BitConverter.ToInt32(data, 0);
        //            break;
        //        case 4:
        //            length = reader.ReadInt32();
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException("numBytesLength must be a value from 1 to 4");
        //    }
        //    string result = new string(reader.ReadChars(length));
        //    return result;
        //}
    }
}
