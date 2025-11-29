using System;

namespace Shop.Messages;

public class ChangeViewModelMessage
{
    public Type ViewModelType { get; }
    public object? Parameter { get; }
    public ChangeViewModelMessage(Type viewModelType, object? parameter = null)
    {
        ViewModelType = viewModelType;
        Parameter = parameter;
    }
}