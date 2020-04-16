using System;
using System.Collections.Generic;

namespace WrapContent{
    public static class ArrayTool 
    {
        //array util
        public static T[] ResizeArray<T>(T[] target, int addcount)
        {
            int oldlength = target.Length;
            Array.Resize(ref target, oldlength + addcount);
            return target;
        }
        public static T[] InsertArray<T>(T[] target, int index, int inscount)
        {
            int oldlength = target.Length;
            Array.Resize(ref target, oldlength + inscount);
            Array.Copy(target, index, target, index + inscount, target.Length - index - inscount);
            return target;
        }
        public static T[] AddToArray<T>(T[] target, T item)
        {
            if (target == null)
            {
                return target;
            }
            int oldlength = target.Length;
            Array.Resize(ref target, oldlength + 1);
            target[oldlength] = item;
            return target;
        }
        public static T[] InsertToArray<T>(T[] target, T item, int index)
        {
            if (target == null)
            {
                return target;
            }
            int oldlength = target.Length;
            Array.Resize(ref target, oldlength + 1);
            Array.Copy(target, index, target, index + 1, target.Length - index - 1);
            target[index] = item;
            return target;
        }
        public static T[] RemoveFromArray<T>(T[] target, int delindex)
        {
            if (target == null)
            {
                return target;
            }
            int oldlength = target.Length;
            if(delindex == -1)
                return target;
            for(int i=0; i<oldlength; i++)
            {
                if(i >= delindex && i + 1 < oldlength)
                {
                    target[i] = target[i+1];
                }
            }
            Array.Resize(ref target, oldlength - 1);
            return target;
        }
        //array util
    }
}