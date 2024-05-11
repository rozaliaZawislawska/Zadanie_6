using System.Data.SqlClient;
using WebApplication2.Dto;
using WebApplication2.Exceptions;
using WebApplication2.Repositories;

namespace WebApplication2.Services;

public interface IWarehouseService
{
    public Task<int> RegisterProductInWarehouseAsync(RegisterProductInWarehouseRequestDTO dto);

    public Task RegisterProductInWarehouseByProcedureAsync(RegisterProductInWarehouseRequestDTO dto);
}

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    public WarehouseService(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }
    
    public async Task<int> RegisterProductInWarehouseAsync(RegisterProductInWarehouseRequestDTO dto)
    {
        if (!await _warehouseRepository.ProductExists((int)dto.IdProduct))
            throw new NotFoundException("Product does not exists");
        if (!await _warehouseRepository.WarehouseExists((int)dto.IdProduct))
            throw new NotFoundException("Warehouse does not exists");
        if (!await _warehouseRepository.OrderExists((int)dto.IdProduct, (int)dto.Amount))
            throw new ConflictException("Order does not exists");
        if (!await _warehouseRepository.CheckDate((DateTime)dto.CreatedAt, (int)dto.IdProduct, (int)dto.Amount))
            throw new NotFoundException("Bad dateTime");

        int idOrder = await _warehouseRepository.GetIdOrder((int)dto.IdProduct, (int)dto.Amount);

        if (!await _warehouseRepository.IdOrderInProduct_WarehouseExists(idOrder))
            throw new NotFoundException("IdOrder does not exists in Product_Warehouse");

        var idProductWarehouse = await _warehouseRepository.RegisterProductInWarehouseAsync(
            idWarehouse: dto.IdWarehouse!.Value,
            idProduct: dto.IdProduct!.Value,
            idOrder: idOrder,
            Amount: dto.Amount!.Value,
            createdAt: DateTime.UtcNow);

        if (!idProductWarehouse.HasValue)
            throw new Exception("Failed to register product in warehouse");

        return idProductWarehouse.Value;
    }


    public async Task RegisterProductInWarehouseByProcedureAsync(RegisterProductInWarehouseRequestDTO dto)
    {
         _warehouseRepository.RegisterProductInWarehouseByProcedureAsync(
            idWarehouse: dto.IdWarehouse!.Value,
            idProduct: dto.IdProduct!.Value,
            createdAt: DateTime.UtcNow);
    }
    
}