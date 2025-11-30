namespace Shop.DTOs;

public class ParametersDTO
{
    public int? Id { get; set; }
    public string Name { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string Unit { get; set; } = null!;
}