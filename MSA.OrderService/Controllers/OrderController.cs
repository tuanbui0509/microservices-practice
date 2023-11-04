using Microsoft.AspNetCore.Mvc;
using MSA.OrderService.Domain;
using MSA.OrderService.Infrastructure.Data;
using MSA.OrderService.Dtos;
using MSA.Common.Contracts.Domain;
using MSA.Common.PostgresMassTransit.PostgresDB;
using MSA.OrderService.Services;
using MassTransit;
using MSA.Common.Contracts.Domain.Commands.Product;
using MSA.Common.Contracts.Domain.Events.Order;

namespace MSA.OrderService.Controllers;

[ApiController]
[Route("v1/order")]
public class OrderController : ControllerBase
{
    private readonly IRepository<Order> repository;
    private readonly IProductService productService;
    private readonly PostgresUnitOfWork<MainDbContext> uow;
    private readonly ISendEndpointProvider sendEndpointProvider;
    private readonly IPublishEndpoint publishEndpoint;
    public OrderController(
        IRepository<Order> repository,
        PostgresUnitOfWork<MainDbContext> uow,
        IProductService productService,
        ISendEndpointProvider sendEndpointProvider,
        IPublishEndpoint publishEndpoint
        )
    {
        this.repository = repository;
        this.uow = uow;
        this.productService = productService;
        this.sendEndpointProvider = sendEndpointProvider;
        this.publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<IEnumerable<Order>> GetAsync()
    {
        var orders = (await repository.GetAllAsync()).ToList();
        return orders;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> PostAsync(CreateOrderDto createOrderDto)
    {
        //validate and ensure product exist before creating
        // var isProductExisted = await productService.IsProductExisted(createOrderDto.ProductId);
        // if (!isProductExisted) return BadRequest();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = createOrderDto.UserId,
            OrderStatus = "Order Submitted"
        };
        await repository.CreateAsync(order);

        await uow.SaveChangeAsync();
        //async validate
        // var endpoint = await sendEndpointProvider.GetSendEndpoint(
        //     new Uri("queue:product-validate-product")
        // );
        // await endpoint.Send(new ValidateProduct
        // {
        //     OrderId = order.Id,
        //     ProductId = createOrderDto.ProductId
        // });

        //async Orchestrator
        await publishEndpoint.Publish<OrderSubmitted>(
            new OrderSubmitted
            {
                OrderId = order.Id,
                ProductId = createOrderDto.ProductId
            });

        await uow.SaveChangeAsync();

        return CreatedAtAction(nameof(PostAsync), order);
    }
}