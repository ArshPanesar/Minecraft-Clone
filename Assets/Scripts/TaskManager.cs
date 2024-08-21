using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class TaskManager
{
    // Inherit this Class to Run Tasks on the Main Thread
    // This will be used to distribute workload among frames
    public abstract class Task
    {
        // Is the Task Complete?
        public bool Completed = false;
        // Is the Task Waiting for Another Task to Complete?
        // Use this Flag in the Execute Function to Enqueue this Task Back to the Task Queue
        public bool WaitingFlag = false;

        public void Run()
        {
            Execute();

            if (!WaitingFlag)
            {
                Completed = true;
            }
        }

        // Override this Function to Perform Some Task
        public abstract void Execute();
    }

    public int NumOfTasksPerUpdate = 16;

    private Queue<Task> TaskQueue;

    // No Instances of this class allowed!
    // Directly use the Singleton
    private TaskManager()
    {
        TaskQueue = new Queue<Task>();
    }

    private static TaskManager Instance = null;
    public static TaskManager GetInstance()
    {
        if (Instance == null)
        {
            Instance = new TaskManager();
        }
        return Instance;
    }

    public void Enqueue(Task NewTask)
    {
        TaskQueue.Enqueue(NewTask);
    }

    public void Update()
    {
        int i = 0;
        while (TaskQueue.Count > 0 && i < NumOfTasksPerUpdate)
        {
            Task NewTask = TaskQueue.Dequeue();
            
            NewTask.Run();
            
            if (!NewTask.Completed)
            {
                Enqueue(NewTask);
            }

            ++i;
        }
    }
}
