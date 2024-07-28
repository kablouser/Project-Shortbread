using System;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static void Reserve<T>(this List<T> list, int capacity)
    {
        if (list.Count < capacity)
        {
            list.Capacity = capacity;
        }
    }

    public static int Dot(this Vector2Int lhs, in Vector2Int rhs)
    {
        return lhs.x * rhs.x + lhs.y * rhs.y;
    }

    public static bool Approximately(float a, float b, float slack)
    {
        return Mathf.Abs(b - a) < slack;
    }

    public static void ReserveArrayClear<T>(ref T[] array, int capacity)
    {
        if (array.Length <= capacity)
        {
            array = new T[capacity << 1];
        }
        else
        {
            Array.Clear(array, 0, capacity);
        }
    }

    public delegate void ActionRef<T>(ref T element, int i);
    public static void ForEachRef<T>(this T[] array, ActionRef<T> actionRef)
    {
        Span<T> span = array.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            actionRef(ref span[i], i);
        }
    }

    // retains elements if they are in the length
    public static void Resize<T>(ref T[] array, int length)
    {
        T[] newArray = new T[length];

        for(int i = 0; i < newArray.Length && i < array.Length; i++)
        {
            newArray[i] = array[i];
        }

        array = newArray;
    }

    public delegate void SetName<T>(ref T element, string name);
    public static void FillWithEnumNames<EnumType, ArrayType>(ref ArrayType[] array, SetName<ArrayType> setName)
    {
        string[] enumNames = Enum.GetNames(typeof(EnumType));
        Resize(ref array, enumNames.Length);
        array.ForEachRef((ref ArrayType element, int i) => setName(ref element, enumNames[i]));
    }

    public static Vector3 AddRandom(this Vector3 vector, float randomRange)
    {
        return new Vector3
        (
            vector.x + UnityEngine.Random.Range(-randomRange, randomRange),
            vector.y + UnityEngine.Random.Range(-randomRange, randomRange),
            vector.z + UnityEngine.Random.Range(-randomRange, randomRange)
        );
    }

    public static Vector2 AddRandom(this Vector2 vector, float randomRange)
    {
        return new Vector2
        (
            vector.x + UnityEngine.Random.Range(-randomRange, randomRange),
            vector.y + UnityEngine.Random.Range(-randomRange, randomRange)
        );
    }

    public static float Accumulate(ref this float x, float y)
    {
        x += y;
        return x;
    }

    public static Transform GetParentUntil(this Transform transform, Transform untilThisIsParent)
    {
        Transform current = transform;
        Transform parent = transform.parent;
        while (parent != null && parent != untilThisIsParent)
        {
            current = parent;
            parent = current.parent;
        }

        if (parent == null)
            return null;
        return current;
    }
}
