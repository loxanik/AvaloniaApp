using System;
using System.Collections.Generic;

namespace Shop.DTOs;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool CanGoNext => PageNumber < TotalPages;
    public bool CanGoBack => PageNumber > 1;
    
    private int CalculateTotalPages()
    {
        if (TotalCount == 0 || PageSize == 0)
            return 1;
            
        return (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}