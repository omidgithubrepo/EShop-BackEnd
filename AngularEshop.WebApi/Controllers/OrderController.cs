using System.Linq;
using System.Threading.Tasks;
using AngularEshop.Core.Services.Interfaces;
using AngularEshop.Core.Utilities.Common;
using AngularEshop.Core.Utilities.Extensions.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AngularEshop.WebApi.Controllers
{
    public class OrderController : SiteBaseController
    {
        #region constructor

        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        #endregion

        #region add product to order

        [HttpGet("add-order")]
        public async Task<IActionResult> AddProductToOrder(long productId, int count)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.GetUserId();
                await _orderService.AddProductToOrder(userId, productId, count);
                return JsonResponseStatus.Success(new
                {
                    message = "محصول با موفقیت به سبد خرید شما اضافه شد",
                    details = await _orderService.GetUserBasketDetails(userId)
                });
            }

            return JsonResponseStatus.Error(new { message = "برای افزودن محصول به سبد خرید ، ابتدا لاگین کنید" });
        }

        #endregion

        #region user basket details

        [HttpGet("get-order-details")]
        public async Task<IActionResult> GetUserBasketDetails()
        {
            if (User.Identity.IsAuthenticated)
            {
                var details = await _orderService.GetUserBasketDetails(User.GetUserId());
                return JsonResponseStatus.Success(details);
            }

            return JsonResponseStatus.Error();
        }

        #endregion

        #region remove order detail from basket

        [HttpGet("remove-order-detail/{detailId}")]
        public async Task<IActionResult> RemoveOrderDetail(int detailId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userOpenOrder = await _orderService.GetUserOpenOrder(User.GetUserId());
                var detail = userOpenOrder.OrderDetails.SingleOrDefault(s => s.Id == detailId);
                if (detail != null)
                {
                    await _orderService.DeleteOrderDetail(detail);
                    return JsonResponseStatus.Success(await _orderService.GetUserBasketDetails(User.GetUserId()));
                }
            }

            return JsonResponseStatus.Error();
        }

        #endregion
    }
}