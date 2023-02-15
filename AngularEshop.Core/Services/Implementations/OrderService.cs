using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngularEshop.Core.DTOs.Orders;
using AngularEshop.Core.Services.Interfaces;
using AngularEshop.Core.Utilities.Common;
using AngularEshop.DataLayer.Entities.Orders;
using AngularEshop.DataLayer.Repository;
using Microsoft.EntityFrameworkCore;

namespace AngularEshop.Core.Services.Implementations
{
    public class OrderService : IOrderService
    {
        #region constructor

        private readonly IGenericRepository<Order> _orderRepository;
        private readonly IGenericRepository<OrderDetail> _orderDetailRepository;
        private readonly IUserService _userService;
        private readonly IProductService _productService;

        public OrderService(IGenericRepository<Order> orderRepository,
            IGenericRepository<OrderDetail> orderDetailRepository, IUserService userService,
            IProductService productService)
        {
            _orderRepository = orderRepository;
            _orderDetailRepository = orderDetailRepository;
            _userService = userService;
            _productService = productService;
        }

        #endregion

        #region order

        public async Task<Order> CreateUserOrder(long userId)
        {
            var order = new Order
            {
                UserId = userId
            };

            await _orderRepository.AddEntity(order);
            await _orderRepository.SaveChanges();

            return order;
        }

        public async Task<Order> GetUserOpenOrder(long userId)
        {
            var order = await _orderRepository.GetEntitiesQuery()
                .Include(s => s.OrderDetails)
                .ThenInclude(s => s.Product)
                .SingleOrDefaultAsync(s => s.UserId == userId && !s.IsPay && !s.IsDelete);

            if (order == null)
                order = await CreateUserOrder(userId);

            return order;
        }

        #endregion

        #region order detail

        public async Task AddProductToOrder(long userId, long productId, int count)
        {
            var user = await _userService.GetUserByUserId(userId);
            var product = await _productService.GetProductForUserOrder(productId);

            if (user != null && product != null)
            {
                var order = await GetUserOpenOrder(userId);

                if (count < 1) count = 1;


                var details = await GetOrderDetails(order.Id);

                var existsDetail = details.SingleOrDefault(s => s.ProductId == productId && !s.IsDelete);

                if (existsDetail != null)
                {
                    existsDetail.Count += count;
                    _orderDetailRepository.UpdateEntity(existsDetail);
                }
                else
                {
                    var detail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = productId,
                        Count = count,
                        Price = product.Price
                    };
                    await _orderDetailRepository.AddEntity(detail);
                }

                await _orderDetailRepository.SaveChanges();
            }
        }

        public async Task<List<OrderDetail>> GetOrderDetails(long orderId)
        {
            return await _orderDetailRepository.GetEntitiesQuery().Where(s => s.OrderId == orderId).ToListAsync();
        }

        public async Task<List<OrderBasketDetail>> GetUserBasketDetails(long userId)
        {
            var openOrder = await GetUserOpenOrder(userId);

            if (openOrder == null) return null;

            return openOrder.OrderDetails.Where(s => !s.IsDelete).Select(f => new OrderBasketDetail
            {
                Id = f.Id,
                Count = f.Count,
                Price = f.Price,
                Title = f.Product.ProductName,
                ImageName = PathTools.Domain + PathTools.ProductImagePath + f.Product.ImageName
            }).ToList();
        }

        public async Task DeleteOrderDetail(OrderDetail detail)
        {
            _orderDetailRepository.RemoveEntity(detail);
            await _orderDetailRepository.SaveChanges();
        }

        #endregion

        #region disposable

        public void Dispose()
        {
            _orderRepository.Dispose();
            _orderDetailRepository.Dispose();
        }

        #endregion
    }
}