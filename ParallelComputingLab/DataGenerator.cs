namespace ParallelComputingLab;
// Генератор массива с фиксированным seed.
public static class DataGenerator
{
    public static int[] Generate(int size, int seed = 12345)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }
        
        var random = new Random(seed);
        var numbers = new int[size];
        for (int i = 0; i < numbers.Length; i++)
        {
            numbers[i] = random.Next(1, 1_000_001);
        }
        return numbers;
    }
}