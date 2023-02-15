using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngularEshop.Core.DTOs.Products;
using AngularEshop.DataLayer.Entities.Product;

namespace AngularEshop.Core.Services.Interfaces
{
    public interface IProductService : IDisposable
    {
        #region product

        Task AddProduct(Product product);
        Task UpdateProduct(Product product);
        Task<FilterProductsDTO> FilterProducts(FilterProductsDTO filter);
        Task<Product> GetProductById(long productId);
        Task<List<Product>> GetRelatedProducts(long productId);
        Task<bool> IsExistsProductById(long productId);
        Task<Product> GetProductForUserOrder(long productId);
        Task<EditProductDTO> GetProductForEdit(long productId);
        Task EditProduct(EditProductDTO product);

        #endregion

        #region product categories

        Task<List<ProductCategory>> GetAllActiveProductCategories();

        #endregion

        #region proudct gallery

        Task<List<ProductGallery>> GetProductActiveGalleries(long productId);

        #endregion

        #region product comments

        Task AddCommentToProduct(ProductComment comment);
        Task<List<ProductCommentDTO>> GetActiveProductComments(long productId);
        Task<ProductCommentDTO> AddProductComment(AddProductCommentDTO comment, long userId);

        #endregion
    }
}