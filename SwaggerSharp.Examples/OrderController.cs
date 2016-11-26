using System;
using System.Collections.Generic;

namespace SwaggerSharp.Examples
{
    public class OrderController : ControllerBase
    {

        /// <summary>
        /// 获取订单详情
        /// </summary>
        /// <param name="orderId">订单id</param>
        /// <returns></returns>
        [ActionVerb(Verb = "get")]
        public OrderDto GetOrderDetail(string orderId)
        {
            return new OrderDto
            {
                Id = Guid.NewGuid(),
                IsPayed = true,
                CreatedOn = DateTime.Now,
                Products = new List<ProductDto>(),
                TotalFee = 100
            };
        }

        /// <summary>
        /// 删除订单
        /// </summary>
        /// <param name="orderId">订单id</param>
        /// <returns></returns>
        public int DeleteOrder(string orderId)
        {
            return 1;
        }

        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="order">订单</param>
        /// <returns></returns>
        public Guid AddOrder(OrderDto order)
        {
            return Guid.NewGuid();
        }

    }
}