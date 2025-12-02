using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Shop.DTOs;
using Shop.Entities;

namespace Shop.Models;

public partial class ProductDetailsModel : ObservableObject
{
    private readonly ProductDetailsDTO _dto;

    public ProductDetailsModel(ProductDetailsDTO dto)
    {
        _dto = dto;
        
        _name = _dto.Name;
        _category = _dto.Category;
        _description = _dto.Description;
        _price = _dto.Price;
        _producer = _dto.Producer;
        _country =  _dto.Country;
        _isDeleted = _dto.IsDeleted;
        _count = _dto.Count;

        Parameters = _dto.Parameters != null
            ? new ObservableCollection<ParametersModel>(
                _dto.Parameters.Select(p => new ParametersModel(p)))
            : [];
    }
    
    public int Id => _dto.Id;
    
    [ObservableProperty]
    private string _name;
    partial void OnNameChanged(string value) => _dto.Name = value;
    
    [ObservableProperty]
    private string _description;
    partial void OnDescriptionChanged(string value) => _dto.Description = value;
    
    [ObservableProperty]
    private decimal _price;
    partial void OnPriceChanged(decimal value) => _dto.Price = value;
    
    [ObservableProperty]
    private string _producer;
    partial void OnProducerChanged(string value) => _dto.Producer = value;
    
    [ObservableProperty]
    private string _country;
    partial void OnCountryChanged(string value) => _dto.Country = value;
    
    [ObservableProperty]
    private string _category;
    partial void OnCategoryChanged(string value) => _dto.Category = value;
    
    [ObservableProperty]
    private bool _isDeleted;
    partial void OnIsDeletedChanged(bool value) => _dto.IsDeleted = value;
    
    [ObservableProperty]
    private int _count;
    partial void OnCountChanged(int value) => _dto.Count = value;
    
    public byte[]? Image => _dto.Image;
    public Bitmap? DisplayImage => _dto.DisplayImage;

    public ObservableCollection<ParametersModel> Parameters { get; }

    public ProductDetailsDTO ToDto()
    {
        _dto.Parameters = Parameters.Select(p => p.ToDto()).ToList();
        return _dto;
    }

    public bool HasChanges(ProductDetailsModel? original)
    {
        if (original == null) return false;
        
        var hasChanges = Name != original.Name ||
                         Description != original.Description ||
                         Price != original.Price ||
                         Producer != original.Producer ||
                         Category != original.Category ||
                         Country != original.Country ||
                         IsDeleted != original.IsDeleted ||
                         Count != original.Count ||
                         ParametersHasChanges(original.Parameters);
    
        return hasChanges;
    }
    
    private bool ParametersHasChanges(ObservableCollection<ParametersModel> original)
    {
        if (original.Count != Parameters.Count) return true;

        for (int i = 0; i < Parameters.Count; i++)
        {
            if (Parameters[i].HasChanges(original[i]))
                return true;
        }
    
        return false;
    }
}