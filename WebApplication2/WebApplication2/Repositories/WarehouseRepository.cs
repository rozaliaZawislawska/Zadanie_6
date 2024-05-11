using System.Data;
using System.Data.SqlClient;
using WebApplication2.Exceptions;

namespace WebApplication2.Repositories;

public interface IWarehouseRepository
{
    public Task<int?> RegisterProductInWarehouseAsync(int idWarehouse, int idProduct, int idOrder, int Amount, DateTime createdAt);
    public Task RegisterProductInWarehouseByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt);
    public Task<bool> ProductExists(int idProduct);

    public Task<bool> WarehouseExists(int idWarehouse);
    public Task<bool> OrderExists(int idProduct, int Amount);
    public Task<bool> CheckDate(DateTime date, int IdProduct, int Amount);
    public Task<int> GetIdOrder(int IdProduct, int Amount);
    public Task<bool> IdOrderInProduct_WarehouseExists(int IdOrder);
}

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _configuration;
    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<int?> RegisterProductInWarehouseAsync(int idWarehouse, int idProduct, int idOrder, int Amount, DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var query = "UPDATE \"Order\" SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            await using var command = new SqlCommand(query, connection);
            command.Transaction = (SqlTransaction)transaction;
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync();
            command.CommandText = @"SELECT Price FROM Product WHERE idProduct = @IdProduct";
            var ProductPrice = (int)await command.ExecuteScalarAsync();

            command.CommandText = @"
                      INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, CreatedAt, Amount, Price)
                      OUTPUT Inserted.IdProductWarehouse
                      VALUES (@IdWarehouse, @IdProduct, @IdOrder, @CreatedAt, @Amount, @Price);";
            ProductPrice = ProductPrice * Amount;
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
            command.Parameters.AddWithValue("@IdProduct", idProduct);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@Amount", Amount);
            command.Parameters.AddWithValue("@Price", ProductPrice);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
            var idProductWarehouse = (int)await command.ExecuteScalarAsync();

            await transaction.CommitAsync();
            return idProductWarehouse;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }
    
    
    
    public async Task RegisterProductInWarehouseByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
        await using var command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("IdProduct", idProduct);
        command.Parameters.AddWithValue("IdWarehouse",idWarehouse);
        command.Parameters.AddWithValue("Amount", 0);
        command.Parameters.AddWithValue("CreatedAt", createdAt);
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException e)
        {
            throw new NotFoundException("SQL ERROR");
        }
    }
    
    public async Task<bool> ProductExists(int idProduct)
    {
        var query = "SELECT COUNT(idProduct) FROM Product WHERE idProduct =@idProduct";
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await using var command = new SqlCommand(query, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@idProduct", idProduct);
        int exists = (int)await command.ExecuteScalarAsync();
        if (exists == 0) return false; else return true;
    }
    
    public async Task<bool> WarehouseExists(int idWarehouse)
    {
        var query = "SELECT COUNT(idWarehouse) FROM Warehouse WHERE idWarehouse =@idWarehouse";
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await using var command = new SqlCommand(query, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@idWarehouse", idWarehouse);
        int exists = (int)await command.ExecuteScalarAsync();
        if (exists == 0) return false; else return true;
    }
    
    public async Task<bool> OrderExists(int IdProduct, int Amount)
    {
        var query = "SELECT COUNT(idProduct) FROM \"Order\" WHERE idProduct =@idProduct AND Amount = @Amount";
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await using var command = new SqlCommand(query, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@idProduct", IdProduct);
        command.Parameters.AddWithValue("@Amount", Amount);
        int exists = (int)await command.ExecuteScalarAsync();
        if (exists == 0)
            return false;
        else 
            return true;
    }

    public async Task<bool> CheckDate(DateTime date, int IdProduct, int Amount)
    {
        var query = "SELECT COUNT(idProduct) FROM \"Order\" WHERE idProduct =@idProduct AND Amount = @Amount";
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await using var command = new SqlCommand(query, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@idProduct", IdProduct);
        command.Parameters.AddWithValue("@Amount", Amount);
        if((DateTime)await command.ExecuteScalarAsync() > date)
            return false;
        else 
            return true;
    }
    
    public async Task<int> GetIdOrder(int IdProduct, int Amount)
    {
        var query = "SELECT IdOrder FROM \"Order\" WHERE idProduct =@idProduct AND Amount = @Amount";
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await using var command = new SqlCommand(query, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@idProduct", IdProduct);
        command.Parameters.AddWithValue("@Amount", Amount);
        return (int)await command.ExecuteScalarAsync();
    }
    
    public async Task<bool> IdOrderInProduct_WarehouseExists(int IdOrder)
    {
        var query = "SELECT COUNT(IdOrder) FROM Product_Warehouse WHERE IdOrder =@IdOrder";
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await using var command = new SqlCommand(query, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@IdOrder", IdOrder);
        if ((int)await command.ExecuteScalarAsync() > 0)
            return false;
        else
            return true;
    }
}