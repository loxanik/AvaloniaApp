using System.Threading.Tasks;

namespace Shop.Interfaces;

public interface IParameterized
{
    Task InitializeParam(object param);
}