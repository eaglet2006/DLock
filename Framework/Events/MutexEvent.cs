using System;

public class MutexEvent : DLockEvent
{
    public MutexEvent()
	{
	}

    public MutexEvent(string name, DLockEvent.DEvent dEvent, int handle, bool suspected)
        : base(dEvent, name, handle, suspected)
    {
    }

    public MutexEvent(string name, DLockEvent.DEvent dEvent, int handle)
        : this(name, dEvent, handle, false)
    {

    }
}
