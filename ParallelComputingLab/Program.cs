namespace ParallelComputingLab;
internal class Program
{
    const int Threads = 4;
    static void Main()
    {
        int[] numbers = DataGenerator.Generate(1_000_000, seed: 12345);

        Calculator.CalculateSequential(numbers);
        Calculator.CalculateWithThreads(numbers, Threads);

        Console.WriteLine("\n1 000 000 элементов, 4 Thread.Sleep(100)");
        Print(numbers, 100);

        Console.WriteLine("\n10 000 элементов");
        Print(DataGenerator.Generate(10_000, 12345), 0);

        Console.WriteLine("\n10 000 000 элементов");
        Print(DataGenerator.Generate(10_000_000, 12345), 0);

        SharedCounterDemo();
        ExceptionDemo();

        Console.WriteLine("Работа завершена.");
    }

    static void Print(int[] numbers, int delayMs)
    {
        Console.WriteLine("# | Последовательно | Многопоточно | Что быстрее");
        for (int run = 1; run <= 5; run++)
        {
            var seq = BenchmarkRunner.Measure(() =>
            {
                if (delayMs > 0)
                {
                    for (int i = 0; i < Threads; i++)
                    {
                        Thread.Sleep(delayMs);
                    }
                }
                return Calculator.CalculateSequential(numbers);
            });

            var par = BenchmarkRunner.Measure(
                () => Calculator.CalculateWithThreads(numbers, Threads, delayMs));

            string faster = par.ElapsedMilliseconds < seq.ElapsedMilliseconds ? "многопоточно" : "последовательно";

            Console.WriteLine($"{run} | {seq.ElapsedMilliseconds:F1} | {par.ElapsedMilliseconds:F1} | {faster}");

            if (seq.Result != par.Result)
            {
                Console.WriteLine("результаты не совпали");
            }
        }
    }

    static void SharedCounterDemo()
    {
        int[] numbers = DataGenerator.Generate(10_000_000, seed: 12345);
        CalculationResult expected = Calculator.CalculateSequential(numbers);

        Console.WriteLine($"\nСумма {expected.Sum}, количество {expected.Count}, максимум {expected.Max}");

        for (int run = 1; run <= 5; run++)
        {
            var wrong = Calculator.CalculateWithThreads(numbers, Threads, safe: false);
            var correct = Calculator.CalculateWithThreads(numbers, Threads, safe: true);

            Console.WriteLine($"{run} | без синхронизации: {wrong.Sum} / {wrong.Count} " +
                              $"{(wrong == expected ? "совпало" : "ошибка")} | " +
                              $"Interlocked: {correct.Sum} / {correct.Count} " +
                              $"{(correct == expected ? "совпало" : "ошибка")}");
        }
    }

    static void ExceptionDemo()
    {
        Console.WriteLine("\nОбработка исключений в рабочих потоках");
        try
        {
            Calculator.CalculateWithThreads(DataGenerator.Generate(100_000, 12345), Threads, breakOneThread: true);
        }
        catch (AggregateException ex)
        {
            Console.WriteLine($"Поймано ошибок: {ex.InnerExceptions.Count}");
            foreach (Exception inner in ex.InnerExceptions)
            {
                Console.WriteLine($"{inner.GetType().Name}: {inner.Message}");
            }
        }
    }
}