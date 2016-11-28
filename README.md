
从.net程序生成swagger json
------------------------

## How to run

- 指定控制器类


```

swaggerSharp.exe -c "SwaggerSharp.Examples.ProductController, SwaggerSharp.Examples.dll" -o stdout -n mySwagger

```

参数说明:

> `-c`: 指定控制器的类型, 并指定程序集dll文件名
> `-o`: 输出到控制台(stdout), 或者json文件(c:\myswagger.json),或者api(http://myapp.com/api)
> `-n`: swagger名称

> **NOTE:** 可以指定多个dll名称, 多个输出．例如: -o stdout c:\myswagger.json,同时输出到终端和json文件． 

> 如果输出到api,则可以通过-H 参数指定headers, 例如 -o http://myapp.com/api -H Authorization=xxxx


- 指定程序集

```

swaggerSharp.exe -a c:\swagger-csharp\bin\Debug\SwaggerSharp.Examples.dll -i "SwaggerSharp.Examples.ControllerBase, SwaggerSharp.Examples.dll"

```

参数说明:  

> `-i`: 指定controller基类; `-a`: 指定程序集的完整路径


## 相关参数

``` c#

 internal class APIConfig
    {
        // action方法名提取
        public string ActionPathRegex { get; set; } = @"(?<action>[^\s]+)$";

        // 控制器类名提取
        public string ControllerPathRegex { get; set; } = @"(?<controller>[^\s]+)Controller$";

        // 命名空间提取到module
        public string NameSpacePathRegex { get; set; } = @"\.?(?<namespace>[^\s\.]+)$";

        // 解析时是否忽略
        public string SwaggerIgnoreAttribute { get; set; } = "ForbidHttpAttribute";

        // verb属性: post,get,put等
        public string VerbAttribute { get; set; } = "ActionVerbAttribute.Verb";

        // 根据此参数的属性是否存在, 值判断参数是否可选
        public string RequiredParemeterAttribute { get; set; } = "RequiredAttribute";

    }


    internal class AssemblyConfig
    {

        // 后缀名
        public string ControllerSuffix { get; set; } = "Controller";

        // Controller父类, 如: SwaggerSharp.Examples.ControllerBase, SwaggerSharp.Examples.dll
        public string InheritFrom { get; set; }

    }

```
