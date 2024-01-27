namespace Cirtuo.RetailProcurementSystem.Domain;

public class SupplierRetailer
{
    public int Id { get; }
    public int SupplierId { get; private set; }
    public int RetailerId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    
    public Supplier Supplier { get; private set; }
    public Retailer Retailer { get; private set; }
    
    public SupplierRetailer(int id, int supplierId, int retailerId, DateTime startDate, DateTime? endDate)
    {
        Id = id;
        SupplierId = supplierId;
        RetailerId = retailerId;
        StartDate = startDate;
        EndDate = endDate;
    }
}