namespace Atc.Kusto.Sample.Contracts;

public record CustomersByGender(
    IReadOnlyList<Customer> Females,
    IReadOnlyList<Customer> Males,
    IReadOnlyList<CustomerGenderCount> Counts);