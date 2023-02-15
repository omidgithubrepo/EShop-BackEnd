using System.Threading.Tasks;
using AngularEshop.Core.DTOs.Products;
using AngularEshop.Core.Services.Interfaces;
using AngularEshop.Core.Utilities.Common;
using AngularEshop.Core.Utilities.Extensions.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AngularEshop.WebApi.Controllers
{
    public class ProductsController : SiteBaseController
    {
        #region constructor

        private IProductService productService;

        public ProductsController(IProductService productService)
        {
            this.productService = productService;
        }

        #endregion

        #region products

        [HttpGet("filter-products")]
        public async Task<IActionResult> GetProducts([FromQuery] FilterProductsDTO filter)
        {
            var products = await productService.FilterProducts(filter);

            // await Task.Delay(4000);

            return JsonResponseStatus.Success(products);
        }

        #endregion

        #region get products categories

        [HttpGet("product-active-categories")]
        public async Task<IActionResult> GetProductsCategories()
        {
            return JsonResponseStatus.Success(await productService.GetAllActiveProductCategories());
        }

        #endregion

        #region get single product

        [HttpGet("single-product/{id}")]
        public async Task<IActionResult> GetProduct(long id)
        {
            var product = await productService.GetProductById(id);
            var productGalleries = await productService.GetProductActiveGalleries(id);

            if (product != null)
                return JsonResponseStatus.Success(new { product = product, galleries = productGalleries });

            return JsonResponseStatus.NotFound();
        }

        #endregion

        #region related products

        [HttpGet("related-products/{id}")]
        public async Task<IActionResult> GetRelatedProducts(long id)
        {
            var relatedProducts = await productService.GetRelatedProducts(id);

            return JsonResponseStatus.Success(relatedProducts);
        }

        #endregion

        #region product comments

        #region list

        [HttpGet("product-comments/{id}")]
        public async Task<IActionResult> GetProductComments(long id)
        {
            var comments = await productService.GetActiveProductComments(id);

            return JsonResponseStatus.Success(comments);
        }

        #endregion

        #region add

        [HttpPost("add-product-comment")]
        public async Task<IActionResult> AddProductComponent([FromBody] AddProductCommentDTO comment)
        {
            if (!User.Identity.IsAuthenticated)
                return JsonResponseStatus.Error(new { message = "لطفا ابتدا وارد سایت شوید" });

            if (!await productService.IsExistsProductById(comment.ProductId))
                return JsonResponseStatus.NotFound();

            var userId = User.GetUserId();

            var res = await productService.AddProductComment(comment, userId);

            return JsonResponseStatus.Success(res);
        }

        #endregion

        #endregion
    }
}