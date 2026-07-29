# FsHcl

A typed HCL generation DSL for F#.

```fsharp
open FsHcl.Hcl
open FsHcl.TerraformHcl

hcl {
    module_ "example" {
        attr "source" (str "./modules/example")

        list_ "patterns" {
            item (str "infra/**")
        }
    }
}
|> document
```

`jsonencode(...)` is built from anonymous records:

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

See [examples/HelloLambda](examples/HelloLambda) for a larger example.
