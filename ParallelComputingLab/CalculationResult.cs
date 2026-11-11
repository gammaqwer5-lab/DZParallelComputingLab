namespace ParallelComputingLab;
// Класс хранит результат вычислений.
public sealed record CalculationResult(
    long Sum,
    int Count,
    int Max);