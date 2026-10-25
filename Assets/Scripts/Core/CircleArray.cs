using System;
using UnityEngine;

public class CircularArray<T>
{
    private T[] elements;
    private int maxLength;
    private int head = -1;
    //private int tail = 0;
    //private bool filled = false;



    public CircularArray(int length, T defaultValue)
    {
        this.maxLength = length;
        this.elements = new T[this.maxLength];
        for (int i = 0; i < length; i++)
        {
            this.elements[i] = defaultValue;
        }
    }

    int mod(int x, int m)
    {
        return (x % m + m) % m;
    }


    // This get will ignore if the index is greater than the current number of elements
    public T Get(int index)
    {
        if (index > this.maxLength)
        {
            throw new IndexOutOfRangeException("Index out of bounds");
        }
        int realIndex = mod((this.head - index), this.maxLength);
        return elements[realIndex];
    }



    public void Push(T element)
    {
        if (this.head == this.maxLength - 1)
        {
            this.head = 0;
            //this.filled = true;
        }
        else
        {
            this.head++;
        }
        this.elements[this.head] = element;

    }



    public void ChangeSize(int newSize, T defaultValue)
    {
        if (newSize < 1)
            throw new ArgumentOutOfRangeException(nameof(newSize));
        var resized = new T[newSize];
        for (int i = 0; i < newSize; i++)
            resized[i] = defaultValue;
        // Copy newest-to-oldest, including the initial head=-1 state. The previous
        // sentinel loop could never reach -1 when two followers spawned before movement.
        int keep = Math.Min(maxLength, newSize);
        for (int age = 0; age < keep; age++)
            resized[newSize - 1 - age] = elements[mod(head - age, maxLength)];
        elements = resized;
        maxLength = newSize;
        head = newSize - 1;
    }

    public override string ToString()
    {
        string ret = "[";
        for (int i = 0; i < this.maxLength; i++)
        {
            if (i == this.head)
            {
                ret += "{" + elements[i].ToString() + "}";
            }
            else
            {
                ret += elements[i].ToString();
            }
            if (i != this.maxLength - 1)
            {
                ret += ", ";
            }
        }
        ret += "]";
        return ret;
    }
}
