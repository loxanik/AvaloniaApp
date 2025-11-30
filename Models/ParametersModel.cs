using CommunityToolkit.Mvvm.ComponentModel;
using Shop.DTOs;

namespace Shop.Models;

public partial class ParametersModel : ObservableObject
{
    private readonly ParametersDTO  _dto;

    public ParametersModel(ParametersDTO dto)
    {
        _dto = dto;
        
        _name = _dto.Name;
        _value = _dto.Value;
        _unit = _dto.Unit;
    }
    
    public int? Id => _dto.Id;
    
    [ObservableProperty]
    private string _name;
    partial void OnNameChanged(string value) => _dto.Name = value;
        
    [ObservableProperty]
    private string _value;
    partial void OnValueChanged(string value) => _dto.Value = value;
        
    [ObservableProperty]
    private string _unit;
    partial void OnUnitChanged(string value) => _dto.Unit = value;
    
    public ParametersDTO ToDto() => _dto;

    public bool HasChanges(ParametersModel? original)
    {
        if (original == null) return true;
        
        return Name != original.Name ||
               Value != original.Value ||
               Unit != original.Unit;
    }
    
    public bool IsNew => Id is null or 0;
}