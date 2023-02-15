using System.Threading.Tasks;
using AngularEshop.Core.DTOs.Products;
using AngularEshop.Core.Services.Interfaces;
using AngularEshop.Core.Utilities.Common;
using AngularEshop.WebApi.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AngularEshop.WebApi.Controllers
{
    public class AdminProductsController : SiteBaseController
    {
        #region constructor

        private readonly IProductService productService;

        public AdminProductsController(IProductService productService)
        {
            this.productService = productService;
        }

        #endregion

        #region get product for edit

        [PermissionChecker("ProductManager")]
        [HttpGet("get-product-for-edit/{id}")]
        public async Task<IActionResult> GetProductForEdit(long id)
        {
            var product = await productService.GetProductForEdit(id);
            if (product == null) return JsonResponseStatus.NotFound();
            return JsonResponseStatus.Success(product);
        }

        #endregion

        #region edit product

        [HttpPost("edit-product")]
        public async Task<IActionResult> EditProduct([FromBody] EditProductDTO product)
        {
            if (ModelState.IsValid)
            {
                await productService.EditProduct(product);
                return JsonResponseStatus.Success();
            }

            return JsonResponseStatus.Error();
        }

        #endregion
    }
}
