﻿using System;
using System.Xml.Serialization;

public abstract class DLockEvent
{
    public enum GlobalEvent
    {
        None = 0,
        MutexEvent = 1000,
    }

    public enum DEvent
    {
        None = 0,
        InitApplyToken = 1, //apply token first time
        ApplyToken = 2, //apply a token from server
        ReturnToken = 3, //return toekn to server
        RequireToken = 4, //server send this event to client to require the token. Client need return the token immediately.
        Exit = 100, //Exit when all the mutex with same name are closed.
    }

    /// <summary>
    /// DEvent 
    /// </summary>
    [XmlAttribute]
    public DEvent Event { get; set; }

    [XmlAttribute]
    public string Name { get; set; }

    [XmlAttribute]
    public int Handle { get; set; }

    [XmlAttribute]
    public bool Suspected { get; set; }

    public DLockEvent()
	{

	}

    public DLockEvent(DEvent evt, string name, int handle, bool suspected)
    {
        Event = evt;
        Name = name;
        Handle = handle;
        Suspected = suspected;
    }


    public DLockEvent(DEvent evt, string name)
        :this(evt, name, -1, false)
    {
    }

    /// <summary>
    /// Get global event.
    /// Global event specified class type
    /// </summary>
    /// <returns></returns>
    public GlobalEvent GetGlobalEvent()
    {
        if (this is MutexEvent)
        {
            return GlobalEvent.MutexEvent;
        }
        else
        {
            return GlobalEvent.None;
        }
    }

    /// <summary>
    /// Serialize event to bytes
    /// </summary>
    /// <returns></returns>
    public byte[] GetBytes()
    {
        return DLock.Framework.Serialization.XmlSerialization.Serialize(this).ToArray();
    }

    /// <summary>
    /// Deserialize event from bytes
    /// </summary>
    /// <param name="bytes">body of data</param>
    /// <returns></returns>
    public static T FromBytes<T>(byte[] bytes)
    {
        return DLock.Framework.Serialization.XmlSerialization<T>.Deserialize(
            new System.IO.MemoryStream(bytes));
    }
}
