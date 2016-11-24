using System;
using System.Collections.Generic;

namespace SwaggerSharp.Examples
{
    /// <summary>
    /// 订单
    /// </summary>
    public class OrderDto
    {
        /// <summary>
        /// 订单id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 产品
        /// </summary>
        public IList<ProductDto> Products { get; set; }

        /// <summary>
        /// 订单总费用
        /// </summary>
        public float TotalFee { get; set; }

        /// <summary>
        /// 订单创建时间
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// 是否已支付
        /// </summary>
        public bool IsPayed { get; set; }

    }
}