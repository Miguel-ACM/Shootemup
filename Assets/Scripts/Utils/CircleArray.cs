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
        //Debug.Log(realIndex.ToString() + "  " + this.head.ToString() + "  " + index.ToString());
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
        T[] newElements = new T[newSize];
        int sizeDifference = newSize - this.maxLength;
        int copyFrom;
        if (sizeDifference > 0)
        {
            copyFrom = (this.head + 1) % this.maxLength;
        }
        else
        {
            copyFrom = (this.head - sizeDifference + 1) % this.maxLength;
        }
        int i = 0;

        while (copyFrom != this.head)
        {
            newElements[i] = this.elements[copyFrom];
            copyFrom = (copyFrom + 1) % this.maxLength;
            i++;
        }
        newElements[i] = this.elements[copyFrom];
        this.head = i;
        this.elements = newElements;
        this.maxLength = newSize;
        while (sizeDifference > 0)
        {
            this.elements[i + sizeDifference] = defaultValue;
            sizeDifference--;
        }
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