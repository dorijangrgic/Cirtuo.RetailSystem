using Cirtuo.RetailProcurementSystem.Application.Common;
using Cirtuo.RetailProcurementSystem.Application.Common.Models;
using Cirtuo.RetailProcurementSystem.Application.Common.Services;
using Cirtuo.RetailProcurementSystem.Application.SupplierRetailers.Models;
using Cirtuo.RetailProcurementSystem.Application.SupplierRetailers.Specifications;
using Cirtuo.RetailProcurementSystem.Application.Suppliers.Models;
using Cirtuo.RetailProcurementSystem.Domain;

namespace Cirtuo.RetailProcurementSystem.Application.SupplierRetailers.Services;

public class SupplierRetailerService : ISupplierRetailerService
{
    private readonly IGenericRepository<Supplier> _supplierRepository;
    private readonly IGenericRepository<Retailer> _retailerRepository;
    private readonly IGenericRepository<SupplierRetailer> _supplierRetailerRepository;
    private readonly IDateTimeService _dateTimeService;

    public SupplierRetailerService(
        IGenericRepository<Retailer> retailerRepository,
        IGenericRepository<Supplier> supplierRepository,
        IGenericRepository<SupplierRetailer> supplierRetailerRepository,
        IDateTimeService dateTimeService
    )
    {
        _retailerRepository = retailerRepository;
        _supplierRepository = supplierRepository;
        _supplierRetailerRepository = supplierRetailerRepository;
        _dateTimeService = dateTimeService;
    }

    public async Task AddSuppliersForUpcomingQuarterAsync(ConnectSupplierRetailerRequest connectSupplierRetailerRequest)
    {
        var retailer = await _retailerRepository.GetByIdAsync(connectSupplierRetailerRequest.RetailerId);
        if (retailer is null) throw new NotFoundException($"Retailer with id {connectSupplierRetailerRequest.RetailerId} does not exist");

        var upcomingQuarter = _dateTimeService.CurrentQuarterYear.Next();

        var supplierRetailers = new List<SupplierRetailer>();
        foreach (var supplierId in connectSupplierRetailerRequest.SupplierIds)
        {
            var supplier = await _supplierRepository.GetByIdAsync(supplierId);
            if (supplier is null) throw new NotFoundException($"Supplier with id {supplierId} does not exist");
            
            var supplierRetailer = await _supplierRetailerRepository.FirstOrDefaultAsync(new GetSupplierRetailerSpec(supplierId, retailer.Id));
            if (supplierRetailer is not null)
            {
                var supplierRetailerEndDateQuarter = _dateTimeService.QuarterYearFrom(supplierRetailer.EndDate);
                if (upcomingQuarter.Equals(supplierRetailerEndDateQuarter))
                    throw new ApplicationException($"Supplier with id {supplierId} is already connected to retailer with id {retailer.Id}");
                supplierRetailer.UpdateEndDate(upcomingQuarter.EndDate);
            }
            else
            {
                supplierRetailers.Add(new SupplierRetailer(supplier.Id, retailer.Id, upcomingQuarter.StartDate, upcomingQuarter.EndDate));
            }
        }
        
        await _supplierRetailerRepository.AddRangeAsync(supplierRetailers);
        await _supplierRetailerRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<SupplierDto>> GetSuppliersForCurrentQuarterAsync()
    {
        var currentQuarter = _dateTimeService.CurrentQuarterYear;
        var getSupplierRetailersForCurrentQuarterSpec = new GetSuppliersForCurrentQuarterSpec(currentQuarter.EndDate);
        var supplierRetailers = await _supplierRetailerRepository.ListAsync(getSupplierRetailersForCurrentQuarterSpec);
        
        return supplierRetailers.Select(sr =>
        {
            var supplier = sr.Supplier;
            var location = new LocationDto(supplier.Location.Id, supplier.Location.Address, supplier.Location.City, supplier.Location.State, supplier.Location.ZipCode);
            var contact = new ContactDto(supplier.Contact.Id, supplier.Contact.Email, supplier.Contact.Phone);
            return new SupplierDto(supplier.Id, supplier.Name, location, contact);
        });
    }
}