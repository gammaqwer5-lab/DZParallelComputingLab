using System.Diagnostics;

namespace ParallelComputingLab;
// Класс проводит измерения и выводит медианное время.
public static class BenchmarkRunner
{
    private const int MeasurementCount = 10;

    public static void Run(int[] numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        Console.WriteLine();
        Console.WriteLine($"Размер массива: {numbers.Length:N0}");

        // Прогрев обеих реализаций.
        Calculator.CalculateSequential(numbers);
        Calculator.CalculateParallel(numbers);
        Calculator.CalculateParallelWrong(numbers);

        var sequentialTimes = new List<double>();
        var parallelTimes = new List<double>();
        var parallelWrongTimes = new List<double>();

        CalculationResult? expectedResult = null;
        CalculationResult? parallelWrongResult = null;

        for (int i = 0; i < MeasurementCount; i++)
        {
            var sequential = Measure(
                () => Calculator.CalculateSequential(numbers));

            var parallel = Measure(
                () => Calculator.CalculateParallel(numbers));

            var parallelWrong = Measure(
                () => Calculator.CalculateParallelWrong(numbers));

            expectedResult ??= sequential.Result;
            parallelWrongResult ??= parallelWrong.Result;

            if (sequential.Result != parallel.Result)
            {
                throw new InvalidOperationException(
                    "Последовательный и параллельный результаты не совпадают.");
            }

            sequentialTimes.Add(sequential.ElapsedMilliseconds);
            parallelTimes.Add(parallel.ElapsedMilliseconds);
            parallelWrongTimes.Add(parallelWrong.ElapsedMilliseconds);
        }

        Console.WriteLine(
            $"Сумма:           {expectedResult!.Sum:N0}");
        Console.WriteLine(
            $"Количество:      {expectedResult.Count:N0}");
        Console.WriteLine(
           $"Максимальный:      {expectedResult.Max:N0}");
        Console.WriteLine(
            $"Последовательно: {Median(sequentialTimes):F3} мс");
        Console.WriteLine(
            $"Параллельно:     {Median(parallelTimes):F3} мс");
        Console.WriteLine("\nCalculateParallelWrong");
        Console.WriteLine(
            $"Сумма:           {parallelWrongResult!.Sum:N0}");
        Console.WriteLine(
            $"Количество:      {parallelWrongResult.Count:N0}");
        Console.WriteLine(
           $"Максимальный:      {parallelWrongResult.Max:N0}");
        Console.WriteLine(
           $"Время:     {Median(parallelWrongTimes):F3} мс");
    }

    public static (CalculationResult Result, double ElapsedMilliseconds)
        Measure(Func<CalculationResult> action)
    {
        var stopwatch = Stopwatch.StartNew();
        CalculationResult result = action();
        stopwatch.Stop();
        return (result, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static double Median(List<double> values)
    {
        double[] sorted = values.OrderBy(x => x).ToArray();
        int middle = sorted.Length / 2;
        if (sorted.Length % 2 == 1)
        {
            return sorted[middle];
        }
        return (sorted[middle - 1] + sorted[middle]) / 2;
    }

    public static void CompareParallelism(int[] numbers)
    {
        int[] degrees = { 1, 2, 3, 4, -1 };

        Console.WriteLine();
        Console.WriteLine($"Размер массива: {numbers.Length:N0}");
        Console.WriteLine("Степень параллелизма | Время, мс | Ускорение | Быстрее | Совпадение");
        Console.WriteLine("-------------------------------------------------------------------");

        Calculator.CalculateSequential(numbers);

        foreach (int degree in degrees)
        {
            Calculator.CalculateParallel(numbers, degree);
        }

        var sequentialTimes = new List<double>();
        CalculationResult? expectedResult = null;

        for (int i = 0; i < MeasurementCount; i++)
        {
            var measurement = Measure(
                () => Calculator.CalculateSequential(numbers));
            expectedResult ??= measurement.Result;
            sequentialTimes.Add(measurement.ElapsedMilliseconds);
        }

        double sequentialMedian = Median(sequentialTimes);

        foreach (int degree in degrees)
        {
            var times = new List<double>();
            bool resultsMatch = true;

            for (int i = 0; i < 5; i++)
            {
                var measurement = Measure(
                    () => Calculator.CalculateParallel(numbers, degree));

                if (measurement.Result != expectedResult)
                {
                    resultsMatch = false;
                }

                times.Add(measurement.ElapsedMilliseconds);
            }
            double parallelMedian = Median(times);
            double acc = sequentialMedian / parallelMedian;

            Console.WriteLine(
                $"{degree,20} | {parallelMedian,10:F3} | {acc,8:F2} | {acc >= 1,7} | {resultsMatch}");
        }
    }
}