using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Bson.TestHelpers
{
    public static class TasksUtils
    {
        public static Task CreateTaskOnOwnThread(Action action) =>
            Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler());

        public static Task<T> CreateTaskOnOwnThread<T>(Func<T> function) =>
            Task.Factory.StartNew(
                function,
                CancellationToken.None,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler());

        public static Task<T>[] CreateTasksOnOwnThread<T>(int count, Func<int, T> function) =>
            Enumerable.Range(0, count)
            .Select(i => Task.Factory.StartNew(
                () => function(i),
                CancellationToken.None,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler()))
            .ToArray();

        public static Task<T>[] CreateTasks<T>(int count, Func<int, Task<T>> taskCreator) =>
            Enumerable.Range(0, count)
            .Select(i => taskCreator(i))
            .ToArray();
    }
}
