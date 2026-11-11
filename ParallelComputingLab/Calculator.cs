using System.Collections.Concurrent;

namespace ParallelComputingLab;

public static class Calculator
{
    public static CalculationResult CalculateSequential(int[] numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        long sum = 0;
        int count = 0;
        int max = int.MinValue;

        foreach (int number in numbers)
        {
            if (number % 13 == 0)
            {
                sum += number;
                count++;
                if (number >  max)
                {
                    max = number;
                }
            }
        }
         
        return new CalculationResult(sum, count, max);
    }

    public static CalculationResult CalculateParallel(
        int[] numbers,
        int maxDegreeOfParallelism = -1)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        if (maxDegreeOfParallelism != -1 &&
            maxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDegreeOfParallelism));
        }

        long sum = 0;
        int count = 0;
        int max = int.MinValue;
        object lockMax = new();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };

        Parallel.For(
            0,
            numbers.Length,
            options,
            () => (LocalSum: 0L, LocalCount: 0, LocalMax: int.MinValue),
            (i, state, local) =>
            {
                int number = numbers[i];

                if (number % 13 == 0)
                {
                    local.LocalSum += number;
                    local.LocalCount++;
                    if (number > local.LocalMax)
                    {
                        local.LocalMax = number;
                    }
                }

                return local;
            },
            local =>
            {
                Interlocked.Add(ref sum, local.LocalSum);
                Interlocked.Add(ref count, local.LocalCount);
                lock (lockMax)
                {
                    if (local.LocalMax > max)
                    {
                        max = local.LocalMax;
                    }
                }
            });

        return new CalculationResult(sum, count, max);
    }

    public static CalculationResult CalculateParallelWrong(int[] numbers)
    {
        long sum = 0;
        int count = 0;
        int max = int.MinValue;

        Parallel.For(0, numbers.Length, i =>
        {
            if (numbers[i] % 13 == 0)
            {
                sum += numbers[i];
                count++;
                if (numbers[i] > max)
                {
                    max = numbers[i];
                }
            }
        });

        return new CalculationResult(sum, count, max);
    }

    public static CalculationResult CalculateWithThreads(
        int[] numbers,
        int threadCount,
        int delayMs = 0,
        bool safe = true,
        bool breakOneThread = false)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        if (threadCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(threadCount));
        }

        long sum = 0;
        int count = 0;
        int max = int.MinValue;
        var errors = new ConcurrentQueue<Exception>();
        var threads = new List<Thread>();
        int chunk = (numbers.Length + threadCount - 1) / threadCount;

        for (int t = 0; t < threadCount; t++)
        {
            int id = t;
            int from = id * chunk;
            int to = Math.Min(from + chunk, numbers.Length);

            var thread = new Thread(() =>
            {
                try
                {
                    if (breakOneThread && id == 1)
                    {
                        throw new InvalidOperationException($"Сбой в потоке {id}");
                    }

                    if (delayMs > 0)
                    {
                        Thread.Sleep(delayMs);
                    }

                    long localSum = 0;
                    int localCount = 0;
                    int localMax = int.MinValue;

                    for (int i = from; i < to; i++)
                    {
                        int number = numbers[i];
                        if (number % 13 != 0)
                        {
                            continue;
                        }

                        if (safe)
                        {
                            localSum += number;
                            localCount++;
                            if (number > localMax)
                            {
                                localMax = number;
                            }
                        }
                        else
                        {
                            sum += number;
                            count++;
                            if (number > max)
                            {
                                max = number;
                            }
                        }
                    }

                    if (safe)
                    {
                        Interlocked.Add(ref sum, localSum);
                        Interlocked.Add(ref count, localCount);

                        int current = Volatile.Read(ref max);
                        while (localMax > current)
                        {
                            int old = Interlocked.CompareExchange(ref max, localMax, current);
                            if (old == current)
                            {
                                break;
                            }
                            current = old;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Enqueue(ex);
                }
            });

            threads.Add(thread);
            thread.Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        if (!errors.IsEmpty)
        {
            throw new AggregateException("Ошибки в рабочих потоках", errors);
        }

        return new CalculationResult(sum, count, max);
    }
}