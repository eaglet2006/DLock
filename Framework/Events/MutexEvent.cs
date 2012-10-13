using System;

public class MutexEvent : DLockEvent
{
    public MutexEvent()
	{
	}

    public MutexEvent(string name, DLockEvent.DEvent dEvent, int handle)
        : base(dEvent, name, handle)
    {

    }
}
