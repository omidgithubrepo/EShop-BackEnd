using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngularEshop.Core.DTOs.Paging;
using AngularEshop.Core.DTOs.Products;
using AngularEshop.Core.Services.Interfaces;
using AngularEshop.Core.Utilities.Common;
using AngularEshop.Core.Utilities.Extensions.FileExtensions;
using AngularEshop.Core.Utilities.Extensions.Paging;
using AngularEshop.DataLayer.Entities.Product;
using AngularEshop.DataLayer.Repository;
using Microsoft.EntityFrameworkCore;

namespace AngularEshop.Core.Services.Implementations
{
    public class ProductService : IProductService
    {
        #region constructor

        private IGenericRepository<Product> productRepository;
        private IGenericRepository<ProductCategory> productCategoryRepository;
        private IGenericRepository<ProductGallery> productGalleryRepository;
        private IGenericRepository<ProductSelectedCategory> productSelectedCategoryRepository;
        private IGenericRepository<ProductVisit> productVisitRepository;
        private IGenericRepository<ProductComment> productCommentRepository;

        public ProductService(IGenericRepository<Product> productRepository,
            IGenericRepository<ProductCategory> productCategoryRepository,
            IGenericRepository<ProductGallery> productGalleryRepository,
            IGenericRepository<ProductSelectedCategory> productSelectedCategoryRepository,
            IGenericRepository<ProductVisit> productVisitRepository,
            IGenericRepository<ProductComment> productCommentRepository)
        {
            this.productRepository = productRepository;
            this.productCategoryRepository = productCategoryRepository;
            this.productGalleryRepository = productGalleryRepository;
            this.productSelectedCategoryRepository = productSelectedCategoryRepository;
            this.productVisitRepository = productVisitRepository;
            this.productCommentRepository = productCommentRepository;
        }

        #endregion

        #region product

        public async Task AddProduct(Product product)
        {
            await productRepository.AddEntity(product);
            await productRepository.SaveChanges();
        }

        public async Task UpdateProduct(Product product)
        {
            productRepository.UpdateEntity(product);
            await productRepository.SaveChanges();
        }

        public async Task<FilterProductsDTO> FilterProducts(FilterProductsDTO filter)
        {
            var productsQuery = productRepository.GetEntitiesQuery().AsQueryable();

            switch (filter.OrderBy)
            {
                case ProductOrderBy.PriceAsc:
                    productsQuery = productsQuery.OrderBy(s => s.Price);
                    break;
                case ProductOrderBy.PriceDec:
                    productsQuery = productsQuery.OrderByDescending(s => s.Price);
                    break;
            }

            if (!string.IsNullOrEmpty(filter.Title))
                productsQuery = productsQuery.Where(s => s.ProductName.Contains(filter.Title));

            if (filter.StartPrice != 0)
                productsQuery = productsQuery.Where(s => s.Price >= filter.StartPrice);

            if (filter.EndPrice != 0)
                productsQuery = productsQuery.Where(s => s.Price <= filter.EndPrice);

            productsQuery = productsQuery.Where(s => s.Price >= filter.StartPrice);

            if (filter.Categories != null && filter.Categories.Any())
                productsQuery = productsQuery.SelectMany(s =>
                    s.ProductSelectedCategories.Where(f => filter.Categories.Contains(f.ProductCategoryId))
                        .Select(t => t.Product));

            if (filter.EndPrice != 0)
                productsQuery = productsQuery.Where(s => s.Price <= filter.EndPrice);

            var count = (int)Math.Ceiling(productsQuery.Count() / (double)filter.TakeEntity);

            var pager = Pager.Build(count, filter.PageId, filter.TakeEntity);

            var products = await productsQuery.Paging(pager).ToListAsync();

            return filter.SetProducts(products).SetPaging(pager);
        }

        public async Task<Product> GetProductById(long productId)
        {
            return await productRepository
                .GetEntitiesQuery()
                .AsQueryable()
                .SingleOrDefaultAsync(s => !s.IsDelete && s.Id == productId);
        }

        public async Task<List<Product>> GetRelatedProducts(long productId)
        {
            var product = await productRepository.GetEntityById(productId);

            if (product == null) return null;

            var productCategoriesList = await productSelectedCategoryRepository.GetEntitiesQuery()
                .Where(s => s.ProductId == productId).Select(f => f.ProductCategoryId).ToListAsync();

            var relatedProducts = await productRepository
                .GetEntitiesQuery()
                .SelectMany(s =>
                    s.ProductSelectedCategories.Where(f => productCategoriesList.Contains(f.ProductCategoryId))
                        .Select(t => t.Product))
                .Where(s => s.Id != productId)
                .OrderByDescending(s => s.CreateDate)
                .Take(4).ToListAsync();

            return relatedProducts;
        }

        public async Task<bool> IsExistsProductById(long productId)
        {
            return await productRepository.GetEntitiesQuery().AnyAsync(s => s.Id == productId);
        }

        public async Task<Product> GetProductForUserOrder(long productId)
        {
            return await productRepository.GetEntitiesQuery()
                .SingleOrDefaultAsync(s => s.Id == productId && !s.IsDelete);
        }

        public async Task<EditProductDTO> GetProductForEdit(long productId)
        {
            var product = await productRepository.GetEntitiesQuery().AsQueryable()
                .SingleOrDefaultAsync(s => s.Id == productId);
            if (product == null) return null;

            return new EditProductDTO
            {
                Id = product.Id,
                CurrentImage = product.ImageName,
                Description = product.Description,
                IsExists = product.IsExists,
                IsSpecial = product.IsSpecial,
                Price = product.Price,
                ProductName = product.ProductName,
                ShortDescription = product.ShortDescription
            };
        }

        public async Task EditProduct(EditProductDTO product)
        {
            var mainProduct = await productRepository.GetEntityById(product.Id);
            if (mainProduct != null)
            {
                mainProduct.ProductName = product.ProductName;
                mainProduct.Description = product.Description;
                mainProduct.IsExists = product.IsExists;
                mainProduct.IsSpecial = product.IsSpecial;
                mainProduct.Price = product.Price;
                mainProduct.Description = product.Description;
                mainProduct.ShortDescription = product.ShortDescription;

                if (!string.IsNullOrEmpty(product.Base64Image))
                {
                    var imageFile = ImageUploaderExtension.Base64ToImage(product.Base64Image);
                    var imageName = Guid.NewGuid().ToString("N") + ".jpeg";
                    imageFile.AddImageToServer(imageName, PathTools.ProductImageServerPath, mainProduct.ImageName);
                    mainProduct.ImageName = imageName;
                }

                productRepository.UpdateEntity(mainProduct);
                await productRepository.SaveChanges();
            }
        }

        #endregion

        #region product categories

        public async Task<List<ProductCategory>> GetAllActiveProductCategories()
        {
            return await productCategoryRepository.GetEntitiesQuery().Where(s => !s.IsDelete).ToListAsync();
        }

        #endregion

        #region products gallery

        public async Task<List<ProductGallery>> GetProductActiveGalleries(long productId)
        {
            return await productGalleryRepository
                .GetEntitiesQuery()
                .Where(s => s.ProductId == productId && !s.IsDelete)
                .Select(s => new ProductGallery
                {
                    ProductId = s.ProductId,
                    Id = s.Id,
                    ImageName = s.ImageName,
                    CreateDate = s.CreateDate
                }).ToListAsync();
        }

        #endregion

        #region product comments

        public async Task AddCommentToProduct(ProductComment comment)
        {
            await productCommentRepository.AddEntity(comment);
            await productCommentRepository.SaveChanges();
        }

        public async Task<List<ProductCommentDTO>> GetActiveProductComments(long productId)
        {
            return await productCommentRepository
                .GetEntitiesQuery()
                .Include(s => s.User)
                .Where(c => c.ProductId == productId && !c.IsDelete)
                .OrderByDescending(s => s.CreateDate)
                .Select(s => new ProductCommentDTO
                {
                    Id = s.Id,
                    Text = s.Text,
                    UserId = s.UserId,
                    UserFullName = s.User.FirstName + " " + s.User.LastName,
                    CreateDate = s.CreateDate.ToString("yyyy/MM/dd HH:mm")
                }).ToListAsync();
        }

        public async Task<ProductCommentDTO> AddProductComment(AddProductCommentDTO comment, long userId)
        {
            var commentData = new ProductComment
            {
                ProductId = comment.ProductId,
                Text = comment.Text,
                UserId = userId
            };

            await productCommentRepository.AddEntity(commentData);

            await productCommentRepository.SaveChanges();

            return new ProductCommentDTO
            {
                Id = commentData.Id,
                CreateDate = commentData.CreateDate.ToString("yyyy/MM/dd HH:mm"),
                Text = commentData.Text,
                UserId = userId,
                UserFullName = ""
            };
        }

        #endregion

        #region dispose

        public void Dispose()
        {
            productRepository?.Dispose();
            productCategoryRepository?.Dispose();
            productGalleryRepository?.Dispose();
            productSelectedCategoryRepository?.Dispose();
            productVisitRepository?.Dispose();
            productCommentRepository?.Dispose();
        }

        #endregion
    }
}