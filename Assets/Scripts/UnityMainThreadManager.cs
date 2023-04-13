using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadManager
{
    // Inherit this Class to Run Methods on the Main Thread
    public abstract class Task
    {
        public bool Completed = false;

        public void Run()
        {
            Execute();

            Completed = true;
        }

        // Override this Function to Run Something on the Main Thread
        public abstract void Execute();
    }

    public int NumOfTasksPerUpdate = 4;

    private Queue<Task> TaskQueue;

    // No Instances of this class allowed!
    // Directly use the Singleton
    private UnityMainThreadManager()
    {
        TaskQueue = new Queue<Task>();
    }

    private static UnityMainThreadManager Instance = null;
    public static UnityMainThreadManager GetInstance()
    {
        if (Instance == null)
        {
            Instance = new UnityMainThreadManager();
        }
        return Instance;
    }

    public void Enqueue(Task NewTask)
    {
        lock (TaskQueue)
        {
            TaskQueue.Enqueue(NewTask);
        }
    }

    public void Update()
    {
        lock (TaskQueue)
        {
            int i = 0;
            while (TaskQueue.Count > 0 && i < NumOfTasksPerUpdate) 
            {
                Task NewTask = TaskQueue.Dequeue();
                NewTask.Run();

                ++i;
            }
        }
    }
}
