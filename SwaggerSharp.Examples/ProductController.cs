using System;
using System.Collections.Generic;

namespace SwaggerSharp.Examples
{
    public class ProductController:ControllerBase
    {

        /// <summary>
        /// 新增产品
        /// </summary>
        /// <param name="product">产品信息</param>
        /// <returns></returns>
        [ActionVerb(Verb = "post")]
        public Guid AddProduct(ProductDto product)
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// 新增产品
        /// </summary>
        /// <param name="product">产品信息</param>
        /// <returns></returns>
        [ActionVerb(Verb = "put")]
        public ProductDto ModifyProduct(ProductDto product)
        {
            return new ProductDto();
        }

        /// <summary>
        /// 删除产品数据
        /// </summary>
        /// <param name="id">产品id</param>
        /// <returns></returns>
        [ActionVerb(Verb = "delete")]
        public bool Delete(string id = "aaa")
        {
            return true;
        }


        /// <summary>
        /// 不转换到swagger文档
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ForbidHttp]
        public bool HiddenMe(string id)
        {
            return true;
        }

        /// <summary>
        /// 分页查询产品
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">分页大小</param>
        /// <returns></returns>
        [ActionVerb(Verb = "get")]
        public IList<ProductDto> GetProducts(int page, int pageSize)
        {
            return new List<ProductDto>();
        }


    }

}