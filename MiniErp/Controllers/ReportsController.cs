using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniErp.Application;

namespace MiniErp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("total-sales-today")]
    public async Task<ActionResult<TotalSalesTodayResult>> GetTotalSalesToday(CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetTotalSalesTodayAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueByRangeResult>> GetRevenue([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetRevenueByRangeAsync(from, to, cancellationToken);
        return Ok(result);
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<IReadOnlyList<TopProductResult>>> GetTopProducts([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int top = 10, CancellationToken cancellationToken = default)
    {
        var result = await _reportingService.GetTopProductsAsync(from, to, top, cancellationToken);
        return Ok(result);
    }

    [HttpGet("top-customers")]
    public async Task<ActionResult<IReadOnlyList<TopCustomerResult>>> GetTopCustomers([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int top = 10, CancellationToken cancellationToken = default)
    {
        var result = await _reportingService.GetTopCustomersAsync(from, to, top, cancellationToken);
        return Ok(result);
    }

    [HttpGet("low-stock-products")]
    public async Task<ActionResult<IReadOnlyList<StockSummaryResult>>> GetLowStockProducts(CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetLowStockProductsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("stock-summary")]
    public async Task<ActionResult<IReadOnlyList<StockSummaryResult>>> GetStockSummary(CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetStockSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("monthly-sales-trend")]
    public async Task<ActionResult<IReadOnlyList<MonthlySalesTrendPoint>>> GetMonthlySalesTrend(CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetMonthlySalesTrendAsync(cancellationToken);
        return Ok(result);
    }
}

