# FsHcl

A small typed HCL generation DSL for F#.

The generic HCL API is split by responsibility:

- `FsHcl.Hcl.Values`: typed values and CLR/F# value conversion
- `FsHcl.Hcl.Syntax`: block, attribute, list, and document builders
- `FsHcl.Hcl.Render`: configurable rendering

`TerraformHcl` adds helpers for Terraform syntax elements such as `terraform`, `provider`, `resource`, `data`, `variable`, `output`, `module_`, `import_`, and `moved_`.
Provider-specific or module-specific arguments such as `project_name` should be defined by callers with `Syntax.attr`.

```fsharp
open FsHcl
open FsHcl.Hcl.Render
open FsHcl.Hcl.Syntax
open FsHcl.Hcl.Values
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

See [examples/HelloLambda](examples/HelloLambda) for a larger example that generates an AWS Lambda-related Terraform file.

`jsonencode(...)` can be built from anonymous records:

```fsharp
attr "template_body" (
    jsonencode
        {|
            AWSTemplateFormatVersion = "2010-09-09"
            Resources =
                {|
                    Example =
                        {|
                            Type = "AWS::Lambda::Permission"
                            Properties =
                                {|
                                    FunctionName = expr "aws_lambda_function.example.function_name"
                                    InvokedViaFunctionUrl = true
                                |}
                        |}
                |}
        |}
)
```
