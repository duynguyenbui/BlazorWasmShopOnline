using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ShopOnline.Models.Dtos;
using ShopOnline.Web.Services.Contracts;

namespace ShopOnline.Web.Pages;

public class ShoppingCartBase : ComponentBase
{
    [Inject]
    public IJSRuntime Js { get; set; }
    
    [Inject]
    public IShoppingCartService ShoppingCartService { get; set; }
    public List<CartItemDto>? ShoppingCartItems { get; set; }
    public string ErrorMessage { get; set; }
    
    protected string TotalPrice { get; set; }
    protected int TotalQuantity { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            ShoppingCartItems = await ShoppingCartService.GetItems(HardCoded.UserId);
            CartChanged();
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
        }
    }

    protected async Task DeleteCartItem_Click(int id)
    {
        var carItemDto = await ShoppingCartService.DeleteItem(id);
        RemoveCartItem(carItemDto.Id);
        CalculateCartSummaryTotals();
        
    }
    
    public async Task UpdateQty_Input(int id)
    {
        await MakeUpdateQtyButtonVisible(id, true);
    }
    
    private async Task MakeUpdateQtyButtonVisible(int id, bool visible)
    {
        await Js.InvokeVoidAsync("MakeUpdateQtyButtonVisible", id, visible);
    }

    protected async Task UpdateQtyCartItem_Click(int id, int qty)
    {
        try
        {
            if (qty > 0)
            {
                var updateItemDto = new CartItemQtyUpdateDto()
                {
                    CartItemId = id,
                    Qty = qty,
                };

                var returnedUpdateItemDto = await ShoppingCartService.UpdateQty(updateItemDto);

                UpdateItemTotalPrice(returnedUpdateItemDto);

                CartChanged();
                await MakeUpdateQtyButtonVisible(id, false);

            }
            else
            {
                var item = ShoppingCartItems.FirstOrDefault(i => i.Id == id);

                if (item != null)
                {
                    item.Qty = 1;
                    item.TotalPrice = item.Price;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    private async Task UpdateItemTotalPrice(CartItemDto cartItemDto)
    {
        var item = GetCartItem(cartItemDto.Id);

        if (item != null)
        {
            item.TotalPrice = cartItemDto.Price * cartItemDto.Qty;
        }
    }
    
    private void CalculateCartSummaryTotals()
    {
        SetTotalPrice();
        SetTotalQuantity();
    }

    private void SetTotalPrice()
    {
        TotalPrice = ShoppingCartItems.Sum(p => p.TotalPrice).ToString("C");
    }

    private void SetTotalQuantity()
    {
        TotalQuantity = ShoppingCartItems.Sum(p => p.Qty);
    }

    private CartItemDto GetCartItem(int id)
    {
        return ShoppingCartItems.FirstOrDefault(x => x.Id == id);
    }

    private void RemoveCartItem(int id)
    {
        var cartItemDto = GetCartItem(id);
        ShoppingCartItems.Remove(cartItemDto);
    }
    
    
    private void CartChanged()
    {
        CalculateCartSummaryTotals();
        ShoppingCartService.RaiseEventOnShoppingCartChanged(TotalQuantity);
    }
}