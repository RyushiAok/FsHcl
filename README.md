# FsHcl

[![NuGet](https://img.shields.io/nuget/v/FsHcl.svg)](https://www.nuget.org/packages/FsHcl)

**FsHcl** is a typed HCL (HashiCorp Configuration Language) generation DSL for F#.
Write Terraform configurations in F# with full type safety and IDE support, then render them to `.tf` files.

## Features

- F# computation expression syntax for HCL blocks (`resource`, `data`, `module`, `variable`, `output`, `locals`, `provider`, `terraform`, ...)
- F# anonymous records as HCL values via `ofValue` / `ofRecord` (also used by `jsonencode`)
- Terraform-specific helpers (`TerraformHcl` module)
- Targets `netstandard2.0` — works with .NET 6/8/9 and beyond

## Quick Start

```
dotnet add package FsHcl
```

```fsharp
open FsHcl.Hcl
open FsHcl.TerraformHcl

hcl {
    module_ "example" {
        attr "source" (str "./modules/example")

        list_ "patterns" [ str "infra/**" ]
    }
}
|> document
```

F# anonymous records are converted to HCL values with `ofValue` / `ofRecord`.
This works anywhere a `Value` is accepted — attributes, function arguments, etc.

```fsharp
attr "tags" (ofValue {| Environment = "prod"; Team = "infra" |})
```

`jsonencode(...)` also accepts anonymous records:

```fsharp
attr "template_body" (
    jsonencode {|
        AWSTemplateFormatVersion = "2010-09-09"
        Resources = {|
            Example = {|
                Type = "AWS::Lambda::Permission"
                Properties = {|
                    FunctionName = expr "aws_lambda_function.example.function_name"
                    InvokedViaFunctionUrl = true
                |}
            |}
        |}
    |}
)
```

## Examples

See [examples/HelloLambda](examples/HelloLambda) for a larger example.
https://github.com/RyushiAok/FsHcl/blob/1df628ac30c33116a50c4fa11ad6b1dd696f1ee8/examples/HelloLambda/Program.fs#L53-L227

## License

[MIT](LICENSE)
